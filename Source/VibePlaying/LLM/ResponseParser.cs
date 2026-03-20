using System.Collections.Generic;
using System.Text;

namespace VibePlaying
{
    /// <summary>
    /// Parses the LLM response to extract both analysis text and JSON action blocks.
    /// Expected format from LLM:
    ///   (analysis text)
    ///   ```actions
    ///   [{"type":"set_work_priority","params":{"pawn_name":"Alice","work_type":"Mining","priority":"1"},"description":"..."},...]
    ///   ```
    /// </summary>
    public static class ResponseParser
    {
        public static LLMAnalysisResponse Parse(string rawResponse)
        {
            var result = new LLMAnalysisResponse();

            if (string.IsNullOrEmpty(rawResponse))
            {
                result.AnalysisText = "";
                return result;
            }

            // Look for ```actions ... ``` block
            const string startMarker = "```actions";
            const string endMarker = "```";

            int startIdx = rawResponse.IndexOf(startMarker);
            if (startIdx < 0)
            {
                // No action block — entire response is analysis text
                result.AnalysisText = rawResponse.Trim();
                return result;
            }

            // Extract analysis text (everything before the actions block)
            result.AnalysisText = rawResponse.Substring(0, startIdx).Trim();

            // Extract JSON array
            int jsonStart = startIdx + startMarker.Length;
            int jsonEnd = rawResponse.IndexOf(endMarker, jsonStart);
            if (jsonEnd < 0) jsonEnd = rawResponse.Length;

            var jsonStr = rawResponse.Substring(jsonStart, jsonEnd - jsonStart).Trim();

            // Also check for text after the actions block
            if (jsonEnd + endMarker.Length < rawResponse.Length)
            {
                var afterText = rawResponse.Substring(jsonEnd + endMarker.Length).Trim();
                if (!string.IsNullOrEmpty(afterText))
                    result.AnalysisText += "\n\n" + afterText;
            }

            // Parse the JSON array manually (lightweight, no Newtonsoft needed at runtime)
            result.Actions = ParseActions(jsonStr);
            return result;
        }

        private static List<ProposedAction> ParseActions(string json)
        {
            var actions = new List<ProposedAction>();
            if (string.IsNullOrEmpty(json) || json[0] != '[')
                return actions;

            // Simple state machine JSON array parser
            int i = 1; // skip opening [
            while (i < json.Length)
            {
                SkipWhitespace(json, ref i);
                if (i >= json.Length || json[i] == ']') break;
                if (json[i] == ',') { i++; continue; }
                if (json[i] == '{')
                {
                    var action = ParseSingleAction(json, ref i);
                    if (action != null) actions.Add(action);
                }
                else
                {
                    i++;
                }
            }
            return actions;
        }

        private static ProposedAction ParseSingleAction(string json, ref int i)
        {
            // Find matching closing brace (handling nested objects)
            int start = i;
            int depth = 0;
            int end = i;
            for (int j = i; j < json.Length; j++)
            {
                if (json[j] == '{') depth++;
                else if (json[j] == '}') { depth--; if (depth == 0) { end = j; break; } }
            }

            var objStr = json.Substring(start, end - start + 1);
            i = end + 1;

            var action = new ProposedAction();
            action.Type = ExtractStringValue(objStr, "type");
            action.Description = ExtractStringValue(objStr, "description");

            // Parse params sub-object
            int paramsIdx = objStr.IndexOf("\"params\"");
            if (paramsIdx >= 0)
            {
                int braceStart = objStr.IndexOf('{', paramsIdx);
                if (braceStart >= 0)
                {
                    int braceEnd = FindMatchingBrace(objStr, braceStart);
                    var paramsStr = objStr.Substring(braceStart, braceEnd - braceStart + 1);
                    action.Params = ParseFlatObject(paramsStr);
                }
            }

            return string.IsNullOrEmpty(action.Type) ? null : action;
        }

        private static Dictionary<string, string> ParseFlatObject(string json)
        {
            var dict = new Dictionary<string, string>();
            int i = 1; // skip {
            while (i < json.Length - 1)
            {
                SkipWhitespace(json, ref i);
                if (json[i] == '}' || json[i] == ',') { i++; continue; }
                if (json[i] == '"')
                {
                    var key = ReadString(json, ref i);
                    SkipWhitespace(json, ref i);
                    if (i < json.Length && json[i] == ':') i++;
                    SkipWhitespace(json, ref i);

                    if (i < json.Length && json[i] == '"')
                    {
                        dict[key] = ReadString(json, ref i);
                    }
                    else
                    {
                        // Number or other literal
                        var sb = new StringBuilder();
                        while (i < json.Length && json[i] != ',' && json[i] != '}')
                            sb.Append(json[i++]);
                        dict[key] = sb.ToString().Trim();
                    }
                }
                else
                {
                    i++;
                }
            }
            return dict;
        }

        private static string ExtractStringValue(string json, string key)
        {
            var marker = $"\"{key}\"";
            int idx = json.IndexOf(marker);
            if (idx < 0) return null;
            idx += marker.Length;
            SkipWhitespace(json, ref idx);
            if (idx >= json.Length || json[idx] != ':') return null;
            idx++;
            SkipWhitespace(json, ref idx);
            if (idx >= json.Length || json[idx] != '"') return null;
            return ReadString(json, ref idx);
        }

        private static string ReadString(string json, ref int i)
        {
            if (json[i] != '"') return null;
            i++; // skip opening quote
            var sb = new StringBuilder();
            bool escaped = false;
            while (i < json.Length)
            {
                char c = json[i++];
                if (escaped) { sb.Append(c); escaped = false; continue; }
                if (c == '\\') { escaped = true; continue; }
                if (c == '"') break;
                sb.Append(c);
            }
            return sb.ToString();
        }

        private static int FindMatchingBrace(string json, int start)
        {
            int depth = 0;
            for (int j = start; j < json.Length; j++)
            {
                if (json[j] == '{') depth++;
                else if (json[j] == '}') { depth--; if (depth == 0) return j; }
            }
            return json.Length - 1;
        }

        private static void SkipWhitespace(string json, ref int i)
        {
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
        }
    }
}
