using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Verse;

namespace VibePlaying
{
    /// <summary>
    /// Async HTTP client for OpenAI-compatible chat completion APIs.
    /// Supports both streaming (SSE) and non-streaming modes.
    /// Uses WebRequest + ThreadPool to avoid blocking the Unity main thread.
    /// Includes retry with exponential backoff.
    /// </summary>
    public static class LLMClient
    {
        private const int MaxRetries = 3;
        private static readonly int[] RetryDelaysMs = { 1000, 3000, 8000 };

        // Volatile field for streaming partial text — UI reads from main thread
        private static volatile string _streamingText = "";
        public static string StreamingText => _streamingText;
        public static volatile bool IsStreaming;

        private const string SystemPrompt = @"You are an expert RimWorld colony management AI advisor.

Your role:
- Analyze the colony state data provided as JSON
- Identify critical issues (food shortage, power deficit, mood crisis, security gaps)
- Suggest concrete, actionable improvements with executable actions
- Prioritize by urgency: survival threats > efficiency > expansion

You can propose actions the player can approve. Available action types:

1. set_work_priority — Change a colonist's work priority
   params: {""pawn_name"": ""Name"", ""work_type"": ""Mining"", ""priority"": ""1""}
   priority: 0=disabled, 1=highest, 4=lowest

2. place_blueprint — Place a building blueprint
   params: {""building_def"": ""Wall"", ""x"": ""50"", ""z"": ""50"", ""rotation"": ""0"", ""stuff"": ""BlocksGranite""}
   rotation: 0=North, 1=East, 2=South, 3=West

3. designate — Designate hunt/mine/cut/harvest
   params: {""action"": ""hunt"", ""target"": ""Deer"", ""count"": ""3""} (for hunt)
   params: {""action"": ""mine"", ""x"": ""50"", ""z"": ""50""} (for mine/cut/harvest)

4. queue_bill — Queue a crafting/cooking bill
   params: {""recipe_def"": ""Make_MealSimple"", ""workbench_def"": ""ElectricStove"", ""count"": ""10""}

5. send_report — Send in-game notification
   params: {""title"": ""..."", ""content"": ""..."", ""severity"": ""info|warning|critical""}

6. create_zone — Create a stockpile or growing zone
   params: {""zone_type"": ""stockpile|growing"", ""x"": ""50"", ""z"": ""50"", ""width"": ""5"", ""height"": ""5""}
   For growing zones, add: ""plant_def"": ""PlantRice""

7. set_draft — Draft or undraft a colonist for combat
   params: {""pawn_name"": ""Name"", ""drafted"": ""true""}

8. place_template — Place a predefined building template
   params: {""template"": ""bedroom"", ""x"": ""50"", ""z"": ""50"", ""stuff"": ""BlocksGranite""}
   Available templates: bedroom (3x4), barracks (5x7), kitchen (4x4), hospital (4x5), killbox (11x5), storage (5x5), research (4x4)
   PREFER templates over individual place_blueprint for room construction.

Output format:
First write your analysis text (status summary, critical issues, recommendations).
Then if you have specific executable actions, add them in a fenced block:

```actions
[{""type"":""set_work_priority"",""params"":{""pawn_name"":""Alice"",""work_type"":""Mining"",""priority"":""1""},""description"":""Assign Alice to mining (skill 12)""},{""type"":""queue_bill"",""params"":{""recipe_def"":""Make_MealSimple"",""count"":""20""},""description"":""Cook 20 simple meals""}]
```

Rules:
- Only propose actions you are confident about
- Use exact defName values from the colony state data
- Keep action list reasonable (max 10 per response)
- Always include a 'description' field explaining WHY
- Respond in the same language as the user's strategy prompt (default: English)";

        public static void AnalyzeAsync(
            string colonyStateJson,
            string userPrompt,
            VibePlayingSettings settings,
            System.Collections.Generic.IReadOnlyList<AnalysisRecord> history,
            Action<string, string> callback)
        {
            var effectiveUserMessage = string.IsNullOrEmpty(userPrompt)
                ? $"Analyze this colony and provide recommendations.\n\nColony State:\n{colonyStateJson}"
                : $"{userPrompt}\n\nColony State:\n{colonyStateJson}";

            var requestBody = BuildRequestJson(settings, effectiveUserMessage, history, stream: true);

            _streamingText = "";
            IsStreaming = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                string result = null;
                string error = null;

                for (int attempt = 0; attempt <= MaxRetries; attempt++)
                {
                    try
                    {
                        result = DoStreamingRequest(settings, requestBody);
                        error = null;
                        break; // success
                    }
                    catch (WebException ex)
                    {
                        error = ex.Message;
                        if (ex.Response is HttpWebResponse httpResp)
                        {
                            int code = (int)httpResp.StatusCode;
                            try
                            {
                                using (var reader = new StreamReader(httpResp.GetResponseStream(), Encoding.UTF8))
                                    error = $"HTTP {code}: {reader.ReadToEnd()}";
                            }
                            catch { }
                            // Don't retry on client errors (4xx) except 429 (rate limit)
                            if (code >= 400 && code < 500 && code != 429)
                                break;
                        }
                        if (attempt < MaxRetries)
                        {
                            Thread.Sleep(RetryDelaysMs[attempt]);
                            _streamingText = "";
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex.Message;
                        if (attempt < MaxRetries)
                        {
                            Thread.Sleep(RetryDelaysMs[attempt]);
                            _streamingText = "";
                        }
                    }
                }

                IsStreaming = false;
                var finalResult = result;
                var finalError = error;
                LongEventHandler.QueueLongEvent(
                    () => callback(finalResult, finalError),
                    "VibePlaying_ProcessResponse", false, null);
            });
        }

        private static string DoStreamingRequest(VibePlayingSettings settings, string requestBody)
        {
            var request = WebRequest.CreateHttp(settings.apiEndpoint);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Timeout = settings.timeoutSeconds * 1000;
            request.ReadWriteTimeout = settings.timeoutSeconds * 1000;

            if (!string.IsNullOrEmpty(settings.apiKey))
                request.Headers["Authorization"] = $"Bearer {settings.apiKey}";

            var bodyBytes = Encoding.UTF8.GetBytes(requestBody);
            request.ContentLength = bodyBytes.Length;

            using (var stream = request.GetRequestStream())
                stream.Write(bodyBytes, 0, bodyBytes.Length);

            using (var response = (HttpWebResponse)request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {
                var contentType = response.ContentType ?? "";
                // Check if server actually returned SSE stream
                if (contentType.Contains("text/event-stream") || contentType.Contains("text/plain"))
                {
                    return ReadSSEStream(reader);
                }
                else
                {
                    // Non-streaming fallback
                    var responseText = reader.ReadToEnd();
                    var content = ExtractContent(responseText);
                    _streamingText = content;
                    return content;
                }
            }
        }

        private static string ReadSSEStream(StreamReader reader)
        {
            var sb = new StringBuilder();
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (!line.StartsWith("data: ")) continue;
                var data = line.Substring(6);
                if (data == "[DONE]") break;

                // Extract delta content from the SSE chunk
                var delta = ExtractDelta(data);
                if (delta != null)
                {
                    sb.Append(delta);
                    _streamingText = sb.ToString();
                }
            }
            return sb.ToString();
        }

        private static string BuildRequestJson(VibePlayingSettings settings, string userMessage,
            System.Collections.Generic.IReadOnlyList<AnalysisRecord> history, bool stream)
        {
            var sb = new StringBuilder(512);
            sb.Append('{');
            sb.Append($"\"model\":\"{EscapeJson(settings.model)}\",");
            sb.Append($"\"max_tokens\":{settings.maxTokens},");
            sb.Append($"\"temperature\":{settings.temperature:F2},");
            if (stream) sb.Append("\"stream\":true,");
            sb.Append("\"messages\":[");
            sb.Append($"{{\"role\":\"system\",\"content\":\"{EscapeJson(SystemPrompt)}\"}}");

            // Include last 2 analysis records as conversation context
            if (history != null)
            {
                int count = 0;
                for (int i = 0; i < history.Count && count < 2; i++)
                {
                    var record = history[i];
                    if (record.IsError) continue;

                    // Reconstruct the user prompt
                    var prompt = string.IsNullOrEmpty(record.Prompt) ? "Analyze colony" : record.Prompt;
                    sb.Append($",{{\"role\":\"user\",\"content\":\"{EscapeJson(prompt)}\"}}");

                    // Truncate long responses to save tokens
                    var response = record.Response ?? "";
                    if (response.Length > 800) response = response.Substring(0, 800) + "...(truncated)";
                    sb.Append($",{{\"role\":\"assistant\",\"content\":\"{EscapeJson(response)}\"}}");
                    count++;
                }
            }

            sb.Append($",{{\"role\":\"user\",\"content\":\"{EscapeJson(userMessage)}\"}}");
            sb.Append("]}");
            return sb.ToString();
        }

        /// <summary>
        /// Extract delta.content from SSE chunk: {"choices":[{"delta":{"content":"text"}}]}
        /// </summary>
        private static string ExtractDelta(string json)
        {
            const string marker = "\"content\":\"";
            int idx = json.IndexOf("\"delta\"");
            if (idx < 0) return null;
            int start = json.IndexOf(marker, idx);
            if (start < 0) return null;
            start += marker.Length;

            var sb = new StringBuilder();
            bool escape = false;
            for (int i = start; i < json.Length; i++)
            {
                char c = json[i];
                if (escape)
                {
                    switch (c)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case '\\': sb.Append('\\'); break;
                        case '"': sb.Append('"'); break;
                        default: sb.Append('\\'); sb.Append(c); break;
                    }
                    escape = false;
                }
                else if (c == '\\') escape = true;
                else if (c == '"') break;
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static string ExtractContent(string responseJson)
        {
            const string marker = "\"content\":\"";
            int start = responseJson.LastIndexOf(marker);
            if (start < 0)
                return responseJson;

            start += marker.Length;
            var sb = new StringBuilder();
            bool escape = false;
            for (int i = start; i < responseJson.Length; i++)
            {
                char c = responseJson[i];
                if (escape)
                {
                    switch (c)
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case '\\': sb.Append('\\'); break;
                        case '"': sb.Append('"'); break;
                        default: sb.Append('\\'); sb.Append(c); break;
                    }
                    escape = false;
                }
                else if (c == '\\') escape = true;
                else if (c == '"') break;
                else sb.Append(c);
            }
            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }
    }
}
