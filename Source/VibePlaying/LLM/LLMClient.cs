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
    /// Uses WebRequest + ThreadPool to avoid blocking the Unity main thread.
    /// </summary>
    public static class LLMClient
    {
        private const string SystemPrompt = @"You are an expert RimWorld colony management AI advisor.

Your role:
- Analyze the colony state data provided as JSON
- Identify critical issues (food shortage, power deficit, mood crisis, security gaps)
- Suggest concrete, actionable improvements
- Prioritize by urgency: survival threats > efficiency > expansion

Output format:
1. **Status Summary** — 2-3 sentence overview of colony health
2. **Critical Issues** — Anything requiring immediate attention (if any)
3. **Recommendations** — Numbered list of specific actions, ordered by priority
4. **Resource Outlook** — Brief forecast for the coming season

Keep responses concise. Reference specific colonist names and resource numbers from the data.
Respond in the same language as the user's strategy prompt (default: English).";

        public static void AnalyzeAsync(
            string colonyStateJson,
            string userPrompt,
            VibePlayingSettings settings,
            Action<string, string> callback)
        {
            // Build request body
            var effectiveUserMessage = string.IsNullOrEmpty(userPrompt)
                ? $"Analyze this colony and provide recommendations.\n\nColony State:\n{colonyStateJson}"
                : $"{userPrompt}\n\nColony State:\n{colonyStateJson}";

            var requestBody = BuildRequestJson(settings, effectiveUserMessage);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
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
                        var responseText = reader.ReadToEnd();
                        var content = ExtractContent(responseText);
                        // Marshal back to main thread via LongEventHandler
                        LongEventHandler.QueueLongEvent(
                            () => callback(content, null),
                            "VibePlaying_ProcessResponse", false, null);
                    }
                }
                catch (WebException ex)
                {
                    var errorMsg = ex.Message;
                    if (ex.Response is HttpWebResponse httpResp)
                    {
                        try
                        {
                            using (var reader = new StreamReader(httpResp.GetResponseStream(), Encoding.UTF8))
                                errorMsg = $"HTTP {(int)httpResp.StatusCode}: {reader.ReadToEnd()}";
                        }
                        catch { /* use original message */ }
                    }
                    LongEventHandler.QueueLongEvent(
                        () => callback(null, errorMsg),
                        "VibePlaying_ProcessError", false, null);
                }
                catch (Exception ex)
                {
                    LongEventHandler.QueueLongEvent(
                        () => callback(null, ex.Message),
                        "VibePlaying_ProcessError", false, null);
                }
            });
        }

        private static string BuildRequestJson(VibePlayingSettings settings, string userMessage)
        {
            // Manual JSON construction to avoid Newtonsoft dependency at runtime.
            // RimWorld ships with its own JSON handling, but manual is most portable.
            var sb = new StringBuilder(512);
            sb.Append('{');
            sb.Append($"\"model\":\"{EscapeJson(settings.model)}\",");
            sb.Append($"\"max_tokens\":{settings.maxTokens},");
            sb.Append($"\"temperature\":{settings.temperature:F2},");
            sb.Append("\"messages\":[");
            sb.Append($"{{\"role\":\"system\",\"content\":\"{EscapeJson(SystemPrompt)}\"}},");
            sb.Append($"{{\"role\":\"user\",\"content\":\"{EscapeJson(userMessage)}\"}}");
            sb.Append("]}");
            return sb.ToString();
        }

        /// <summary>
        /// Extract the assistant message content from an OpenAI-format response.
        /// Lightweight parsing without a full JSON library.
        /// </summary>
        private static string ExtractContent(string responseJson)
        {
            // Look for "content":"..." in the response
            const string marker = "\"content\":\"";
            int start = responseJson.LastIndexOf(marker);
            if (start < 0)
                return responseJson; // fallback: return raw

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
                else if (c == '\\')
                {
                    escape = true;
                }
                else if (c == '"')
                {
                    break;
                }
                else
                {
                    sb.Append(c);
                }
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
