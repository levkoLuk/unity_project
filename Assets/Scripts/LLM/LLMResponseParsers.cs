// Assets/Scripts/LLM/LLMResponseParsers.cs
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Project.LLM
{
    public static class LLMResponseParsers
    {
        // Try several common JSON shapes and fall back to regex-extracted text.
        // This keeps client tolerant to text-generation-webui, HF TGI, etc.

        public static string ExtractText(string rawJson)
        {
            if (string.IsNullOrEmpty(rawJson)) return "";

            try
            {
                // 1) Look for "generated_text":"..."
                var gt = ExtractBetween(rawJson, "\"generated_text\"", ':');
                if (!string.IsNullOrEmpty(gt)) return CleanJsonString(gt);

                // 2) Look for "text":"..." (some endpoints)
                var t = ExtractBetween(rawJson, "\"text\"", ':');
                if (!string.IsNullOrEmpty(t)) return CleanJsonString(t);

                // 3) Look for results[0].text or results[].text
                var m = Regex.Match(rawJson, "\"results\"\\s*:\\s*\\[\\s*\\{[^\\}]*\"text\"\\s*:\\s*\"(?<txt>[\\s\\S]*?)\"",
                    RegexOptions.IgnoreCase);
                if (m.Success) return CleanJsonString(m.Groups["txt"].Value);

                // 4) Look for choices[0].text (OpenAI style)
                var m2 = Regex.Match(rawJson, "\"choices\"\\s*:\\s*\\[\\s*\\{[^\\}]*\"text\"\\s*:\\s*\"(?<txt>[\\s\\S]*?)\"",
                    RegexOptions.IgnoreCase);
                if (m2.Success) return CleanJsonString(m2.Groups["txt"].Value);

                // 5) Fallback: try to find top-level plain text (rare)
                if (!rawJson.TrimStart().StartsWith("{")) return rawJson.Trim();

                // 6) Generic regex for largest quoted block (best-effort)
                var m3 = Regex.Match(rawJson, "\"(?<txt>[^\"]{20,})\"", RegexOptions.Singleline);
                if (m3.Success) return CleanJsonString(m3.Groups["txt"].Value);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("LLMResponseParsers.ExtractText failed: " + ex.Message);
            }

            // As last resort, return rawJson
            return rawJson;
        }

        static string ExtractBetween(string raw, string key, char sep)
        {
            int idx = raw.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;
            int colon = raw.IndexOf(sep, idx + key.Length);
            if (colon < 0) return null;
            int start = raw.IndexOf('"', colon);
            if (start < 0) return null;
            start++;
            int end = raw.IndexOf('"', start);
            if (end < 0) return null;
            return raw.Substring(start, end - start);
        }

        static string CleanJsonString(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            // unescape common sequences:
            s = s.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"");
            return s;
        }
    }
}