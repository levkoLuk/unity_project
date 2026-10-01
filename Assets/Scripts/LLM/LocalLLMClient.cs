// Assets/Scripts/LLM/LocalLLMClient.cs
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Project.LLM
{
    [CreateAssetMenu(fileName = "LLMConfig", menuName = "LLM/Config", order = 1)]
    public class LLMConfig : ScriptableObject
    {
        [Tooltip("Example: http://127.0.0.1:5000/api/generate")]
        public string apiUrl = "http://127.0.0.1:5000/api/generate";
        [Tooltip("Optional API key if your server requires it")]
        public string apiKey = "";
        [Tooltip("Request timeout seconds")]
        public int timeout = 60;
        [Tooltip("Max retry attempts on failure")]
        public int maxRetries = 2;
    }

    public class LocalLLMClient : MonoBehaviour
    {
        [Header("LLM Config")]
        public LLMConfig config;

        // Example defaults — override via inspector or ScriptableObject
        [Header("Request defaults")]
        public int max_new_tokens = 256;
        public float temperature = 0.7f;

        public delegate void OnLLMResult(string generatedText, string rawJson);
        public delegate void OnLLMError(string error);

        /// <summary>
        /// Public API: call LLM with a prompt. Returns via callbacks.
        /// </summary>
        public void Ask(string prompt, OnLLMResult onSuccess, OnLLMError onError = null)
        {
            if (config == null)
            {
                onError?.Invoke("LLM config is null. Assign LLMConfig asset to the LocalLLMClient.");
                return;
            }
            StartCoroutine(PostCoroutine(prompt, onSuccess, onError));
        }

        IEnumerator PostCoroutine(string prompt, OnLLMResult onSuccess, OnLLMError onError)
        {
            if (string.IsNullOrEmpty(config.apiUrl))
            {
                onError?.Invoke("apiUrl is empty in LLMConfig.");
                yield break;
            }

            int attempt = 0;
            while (true)
            {
                attempt++;
                var bodyObj = new
                {
                    prompt = prompt,
                    max_new_tokens = max_new_tokens,
                    temperature = temperature
                };
                string json = JsonUtility.ToJson(bodyObj);
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

                using (var uwr = new UnityWebRequest(config.apiUrl, "POST"))
                {
                    uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    uwr.downloadHandler = new DownloadHandlerBuffer();
                    uwr.SetRequestHeader("Content-Type", "application/json");
                    if (!string.IsNullOrEmpty(config.apiKey))
                        uwr.SetRequestHeader("Authorization", "Bearer " + config.apiKey);

#if UNITY_2020_1_OR_NEWER
                    uwr.timeout = config.timeout;
#endif
                    yield return uwr.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
                    bool isError = (uwr.result == UnityWebRequest.Result.ConnectionError || uwr.result == UnityWebRequest.Result.ProtocolError);
#else
                    bool isError = (uwr.isNetworkError || uwr.isHttpError);
#endif
                    if (isError)
                    {
                        string err = $"LLM request error [{uwr.responseCode}]: {uwr.error} ; body: {uwr.downloadHandler?.text}";
                        Debug.LogError(err);
                        if (attempt <= config.maxRetries)
                        {
                            Debug.LogWarning($"Retrying LLM request ({attempt}/{config.maxRetries})...");
                            yield return new WaitForSeconds(0.5f * attempt);
                            continue;
                        }
                        onError?.Invoke(err);
                        yield break;
                    }
                    else
                    {
                        string resp = uwr.downloadHandler.text;
                        // Try to parse commonly used response shapes:
                        string generated = LLMResponseParsers.ExtractText(resp);
                        onSuccess?.Invoke(generated, resp);
                        yield break;
                    }
                }
            }
        }
    }
}