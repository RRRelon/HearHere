using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace HH
{
    public class GPTManager : MonoBehaviour
    {
        [SerializeField] private PromptSO prompt;
        
        [Header("Broadcasting on")]
        [SerializeField] private StringEventChannelSO onGPTResponseSuccess;
        
        private const string OPEN_AI_API_URL = "https://api.openai.com/v1/chat/completions";
        private const string SECRET_JSON = "secret";
        private string apiKey;
        
        private void Start()
        {
            // Resources/secret.json에서 API Key 읽기
            TextAsset jsonFile = Resources.Load<TextAsset>(SECRET_JSON); // 경로 쓸 때 확장자는 쓰지 않고, resources 폴더 기준 상대 경로로 작성해야 함
            SecretData secret = JsonUtility.FromJson<SecretData>(jsonFile.text); // JSON을 C# 객체로 바꾸는 코드
            apiKey = secret.openaiApiKey;
        }

        public void OnSubmit(string userInput)
        {
            if (!string.IsNullOrWhiteSpace(userInput))
            {
                StartCoroutine(SendRequestToChatGPT(userInput));
            }
        }

        private IEnumerator SendRequestToChatGPT(string userInput)
        {
            var request = new UnityWebRequest(OPEN_AI_API_URL, "POST");
            
            string requestData = OpenAIRequestHelper.CreateChatRequestBody(userInput, prompt.Stage1);
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(requestData);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if(request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(request.error);
            }
            else
            {
                string response = request.downloadHandler.text;
                ChatGPTResponse chatGPTResponse = JsonUtility.FromJson<ChatGPTResponse>(response);
                string gptResponse = chatGPTResponse.choices[0].message.content;
                Debug.Log($"GPT Response: {gptResponse}");
                onGPTResponseSuccess.OnEventRaised(gptResponse);
            }
        }
    }

    [System.Serializable]
    public class ChatGPTResponse
    {
        public Choice[] choices;
    }

    [System.Serializable]
    public class Choice
    {
        public Message message;
    }

    [System.Serializable]
    public class Message
    {
        public string content;
    }
    
    /// <summary>
    /// OpenAI API 요청을 위한 데이터 구조를 정의하고,
    /// 동적으로 JSON 요청 본문을 생성하는 기능을 담당하는 클래스입니다.
    /// </summary>
    public class OpenAIRequestHelper : MonoBehaviour
    {
    #region Data Classes for JSON Serialization
        // JSON 구조와 정확히 일치하는 C# 클래스

        [System.Serializable]
        private class ChatMessage
        {
            public string role;
            public string content;

            public ChatMessage(string role, string content)
            {
                this.role = role;
                this.content = content;
            }
        }

        [System.Serializable]
        private class ChatRequest
        {
            public string model;
            public List<ChatMessage> messages;
            // 필요하다면 temperature, max_tokens 같은 다른 파라미터도 추가할 수 있습니다.
        }

    #endregion

        /// <summary>
        /// 사용자 입력과 프롬프트를 받아 JSON 요청 본문을 생성
        /// </summary>
        /// <returns>API 요청에 사용할 JSON 문자열</returns>
        public static string CreateChatRequestBody(string userInput, string systemPrompt)
        {
            // 1. 요청 데이터를 담을 ChatRequest 객체를 생성합니다.
            ChatRequest requestData = new ChatRequest
            {
                model = "gpt-3.5-turbo",
                messages = new List<ChatMessage>
                {
                    // 2. 시스템 메시지와 사용자 메시지를 리스트에 추가합니다.
                    new ChatMessage("system", systemPrompt),
                    new ChatMessage("user", userInput)
                }
            };

            // 3. C# 클래스 객체를 JSON 형식의 문자열로 변환합니다.
            string jsonBody = JsonUtility.ToJson(requestData);

            return jsonBody;
        }
    }
}
