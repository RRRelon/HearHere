using System;
using Newtonsoft.Json;

namespace HH
{
    using OpenAI;
    using Samples.Whisper;
    using UnityEngine;
    using UnityEngine.Networking;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    
    [CreateAssetMenu(fileName = "AIConversationManager", menuName = "AI/AI Conversation Manager")]
    public class AIConversationManagerSO : ScriptableObject
    {
        // Open AI
        private const string fileName = "output.wav";
        private OpenAIApi openai;
        private const string OPEN_AI_API_URL = "https://api.openai.com/v1/chat/completions";
        private const string SECRET_JSON = "secret";
        private string googleTTSApiKey;
        private string openaiApiKey;
        
        private void OnEnable()
        {
            // Resources/secret.json에서 API Key 읽기
            TextAsset jsonFile = Resources.Load<TextAsset>(SECRET_JSON); // 경로 쓸 때 확장자는 쓰지 않고, resources 폴더 기준 상대 경로로 작성해야 함
            SecretData secret = JsonUtility.FromJson<SecretData>(jsonFile.text); // JSON을 C# 객체로 바꾸는 코드
            googleTTSApiKey = secret.googleTTSApiKey;
            openaiApiKey = secret.openaiApiKey;

            openai = new OpenAIApi(openaiApiKey);
        }

        /// <summary>
        /// 오디오 클립으로부터 텍스트 추출해 반환
        /// </summary>
        public async Task<string> GetTextFromAudio(AudioClip audioClip)
        {
            // 1. STT: 오디오를 텍스트로 변환
            try
            {
                string userText = await TranscribeAudioAsync(audioClip);
                return userText;
            }
            catch (JsonSerializationException jsonEx)
            {
                Debug.LogError($"STT Error: {jsonEx.Message}");
                return string.Empty;
            }
            catch (Exception e)
            {
                Debug.LogError($"STT Error: {e.Message}");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// Text에 대한 GPT 응답을 Json 형식으로 반환
        /// </summary>
        public async Task<GPTResponse> GetGPTResponseFromText(string userText, string prompt)
        {
            GPTResponse gptResponse = await GetGPTResponseAsync(userText, prompt);
            if (gptResponse == null)
            {
                Debug.LogError("GPT 응답을 받아오는 데 실패했습니다.");
                return null;
            }

            return gptResponse;
        }
        
        /// <summary>
        /// TTS text를 Audio Clip으로 추출해 반환 (OpenAI TTS 사용)
        /// </summary>
        public async Task<AudioClip> RequestTextToSpeech(string text)
        {
            string url = "https://api.openai.com/v1/audio/speech";
            
            // OpenAI TTS 요청 데이터 생성
            string requestJson = OpenAIRequestHelper.CreateTTSRequestBody(text, "ash");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);
            
            using (UnityWebRequest wwwSender = new UnityWebRequest(url, "POST"))
            {
                // HTTP POST 요청 준비
                wwwSender.uploadHandler = new UploadHandlerRaw(bodyRaw);
                wwwSender.downloadHandler = new DownloadHandlerBuffer();
                wwwSender.SetRequestHeader("Content-Type", "application/json");
                wwwSender.SetRequestHeader("Authorization", "Bearer " + openaiApiKey);
                
                // UnityWebRequest가 완료될 때까지 비동기적으로 기다림
                var asyncOp = wwwSender.SendWebRequest();
                while (!asyncOp.isDone)
                {
                    await Task.Yield();
                }
                
                // 응답이 오지 않았을 때
                if (wwwSender.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"OpenAI TTS API 요청 실패 (HTTP {wwwSender.responseCode}): {wwwSender.error}\n서버 응답: {wwwSender.downloadHandler.text}");
                    return null;
                }
                
                // 응답이 왔을 때 - OpenAI는 직접 MP3 바이너리 데이터를 반환
                byte[] audioData = wwwSender.downloadHandler.data;
                string filePath = Path.Combine(Application.persistentDataPath, "tts.mp3");
                await File.WriteAllBytesAsync(filePath, audioData);
                
                // Audio Clip으로 생성해 플레이 한다.
                using (UnityWebRequest wwwReceiver = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG))
                {
                    var asyncOp2 = wwwReceiver.SendWebRequest();
                    while (!asyncOp2.isDone)
                    {
                        await Task.Yield();
                    }
                
                    // 요청 실패 시 에러 출력 후 함수 종료
                    if (wwwReceiver.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"오디오 파일 로드 실패 (HTTP {wwwReceiver.responseCode}): {wwwReceiver.error}");
                        return null;
                    }
                
                    // 오디오 파일 재생
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(wwwReceiver);
                    return clip;
                }
            }
        }
        
        /// <summary>
        /// 오디오 클립을 텍스트로 변환하는 비동기 함수
        /// </summary>
        private async Task<string> TranscribeAudioAsync(AudioClip clip)
        {
            byte[] data = SaveWav.Save(fileName, clip);
            var req = new CreateAudioTranscriptionsRequest
            {
                FileData = new FileData() { Data = data, Name = "audio.wav" },
                Model = "whisper-1",
                Language = "en"
            };

            var res = await openai.CreateAudioTranscription(req);
            return res.Text;
        }
        
        /// <summary>
        /// 사용자 입력을 받아 GPT에게 응답을 요청하는 비동기 함수
        /// </summary>
        private async Task<GPTResponse> GetGPTResponseAsync(string userInput, string prompt)
        {
            string requestJson = OpenAIRequestHelper.CreateChatRequestBody(userInput, prompt);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(requestJson);

            using (var request = new UnityWebRequest(OPEN_AI_API_URL, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + openaiApiKey);

                // UnityWebRequest를 비동기적으로 기다립니다.
                var asyncOp = request.SendWebRequest();
                while (!asyncOp.isDone)
                {
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"GPT 요청 실패: {request.error}\n{request.downloadHandler.text}");
                    return null;
                }
                else
                {
                    // 1. 전체 OpenAI 응답을 파싱
                    string rawResponse = request.downloadHandler.text;
                    var openAIResponse = JsonUtility.FromJson<ChatGPTResponse>(rawResponse);
                    
                    // 2. 실제 AI가 생성한 메시지(JSON 형식의 문자열)를 추출
                    string gptContent = openAIResponse.choices[0].message.content;
                    gptContent = gptContent.Replace("```json", "").Replace("```", "").Trim();

                    // 3. 추출한 메시지 문자열을 우리가 원하는 GPTResponse 객체로 최종 파싱
                    try
                    {
                        GPTResponse finalResponse = JsonUtility.FromJson<GPTResponse>(gptContent);
                        return finalResponse;
                    }

                    catch (Exception ex)
                    {
                        Debug.LogError($"[JSON Parsing Error] Failed to parse GPT response. Error: {ex.Message}");
                        Debug.LogError($"[JSON Parsing Error] Original Content: {gptContent}");
                        return null; // 실패 시 null 반환
                    }
                }
            }
        }
    }
}
    
