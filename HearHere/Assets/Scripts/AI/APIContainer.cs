namespace HH
{
   using System.Collections.Generic;
   using UnityEngine;

   #region TTS
   // Google TTS 클래스들 (기존 유지)
   [System.Serializable]
   public class SynthesisInput
   {
       public string text;
   }

   [System.Serializable]
   public class VoiceSelectionParams
   {
       public string languageCode;
       public string name;
       public string ssmlGender;
   }

   [System.Serializable]
   public class AudioConfig
   {
       public string audioEncoding;
       public float speakingRate;
   }

   [System.Serializable]
   public class GoogleCloudTextToSpeechRequest
   {
       public SynthesisInput input;
       public VoiceSelectionParams voice;
       public AudioConfig audioConfig;
   }

   [System.Serializable]
   public class GoogleCloudTextToSpeechResponse
   {
       public string audioContent;
   }

   // OpenAI TTS 클래스들 (새로 추가)
   [System.Serializable]
   public class OpenAITTSRequest
   {
       public string model = "tts-1";
       public string input;
       public string voice = "ash";
       public string response_format = "mp3";
   }
   #endregion

   #region Key
   [System.Serializable]
   public class SecretData
   {
       public string googleTTSApiKey;
       public string openaiApiKey;
       public override string ToString()
       {
           return $"google TTS API Key: {googleTTSApiKey}, OpenAI API Key: {openaiApiKey}";
       }
   }
   #endregion
   
   #region GPT
   /// <summary>
   /// Json으로 받은 GPT 응답을 저장하는 클래스
   /// </summary>
   [System.Serializable]
   public class GPTResponse
   {
       public string response_type;
       public string tts_text;
       public string argument;
               
       public override string ToString()
       {
           return $"Type: {response_type}, TTS: '{tts_text}', Arg: {argument}";
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
   /// 동적으로 JSON 요청 본문을 생성하는 기능을 담당하는 클래스
   /// Json convention(All lower case) 지킬 것
   /// </summary>
   public class OpenAIRequestHelper
   {
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
       }

       /// <summary>
       /// 사용자 입력과 프롬프트를 받아 JSON 요청 본문을 생성
       /// </summary>
       public static string CreateChatRequestBody(string userInput, string systemPrompt)
       {
           // 1. 요청 데이터를 담을 ChatRequest 객체를 생성합니다.
           ChatRequest requestData = new ChatRequest
           {
               model = "gpt-4o-mini",
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

       /// <summary>
       /// OpenAI TTS 요청을 위한 JSON 요청 본문을 생성
       /// </summary>
       public static string CreateTTSRequestBody(string text, string voice = "ash")
       {
           OpenAITTSRequest requestData = new OpenAITTSRequest
           {
               input = text,
               voice = voice
           };

           string jsonBody = JsonUtility.ToJson(requestData);
           return jsonBody;
       }
   }
   #endregion    
}