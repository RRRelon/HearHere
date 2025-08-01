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
        // STT
        private const string fileName = "output.wav";
        private OpenAIApi openai = new OpenAIApi();
        
        // GPT
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
        }

        /// <summary>
        /// 오디오 클립으로부터 텍스트 추출해 반환
        /// </summary>
        public async Task<string> GetTextFromAudio(AudioClip audioClip)
        {
            // 1. STT: 오디오를 텍스트로 변환
            string userText = await TranscribeAudioAsync(audioClip);
            if (string.IsNullOrWhiteSpace(userText))
            {
                Debug.LogError("STT 변환에 실패했거나 음성 입력이 없습니다.");
                return null;
            }
            return userText;
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
        /// TTS text를 Audio Clip으로 추출해 반환
        /// </summary>
        public async Task<AudioClip> RequestTextToSpeech(string text)
        {
            // google TTS api가 요구하는 요청 형식에 맞게 요청 객체 구성
            var request = new GoogleCloudTextToSpeechRequest
            {
                input = new SynthesisInput { text = text },
                voice = new VoiceSelectionParams { languageCode = "en-US", name = "en-US-Standard-C", ssmlGender = "MALE" },
                // voice = new VoiceSelectionParams { languageCode = "ko-KR", name = "ko-KR-Standard-B", ssmlGender = "MALE" },
                audioConfig = new AudioConfig { audioEncoding = "MP3" }
            };
            
            string json = JsonUtility.ToJson(request);      // 요청 객체를 JSON 문자열로 바꿈, unity에서는 서버로 데이터를 보낼 때 JSON형태로 변환해서 보내야함
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);  // 요청 객체를 UTF-8 바이트로 변환, 컴퓨터가 이해할 수 있는 byte로 변환
            string url = "https://texttospeech.googleapis.com/v1/text:synthesize?key=" + googleTTSApiKey;
            
            using (UnityWebRequest wwwSender = new UnityWebRequest(url, "POST")) // UnityWebRequest: unity에서 HTTP요청을 보낼 수 있게 해주는 클래스임
            {
                // HTTP POST 요청 준비
                wwwSender.uploadHandler = new UploadHandlerRaw(bodyRaw); //UploadHandlerRaw: google 서버에 보낼 준비 
                wwwSender.downloadHandler = new DownloadHandlerBuffer(); // 서버에서 response가 오면 그 데이터를 받기 위한 버퍼임
                wwwSender.SetRequestHeader("Content-Type", "application/json"); // 서버에게 JSON파일을 보낸다고 알려주는 코드, 이걸 보고 어떻게 해석할지 결정 가능함
                
                // UnityWebRequest가 완료될 때까지 비동기적으로 기다림
                await wwwSender.SendWebRequest();
                
                // 응답이 오지 않았을 때
                if (wwwSender.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Google TTS API 요청 실패 (HTTP {wwwSender.responseCode}): {wwwSender.error}\n서버 응답: {wwwSender.downloadHandler.text}");
                    return null;
                }
                
                // 응답이 왔을 때 
                string responseJson = wwwSender.downloadHandler.text;
                var response = JsonUtility.FromJson<GoogleCloudTextToSpeechResponse>(responseJson);
                
                // 받은 base64 오디어 데이터를 .mp3파일로 저장
                byte[] audioData = System.Convert.FromBase64String(response.audioContent);
                string filePath = Path.Combine(Application.persistentDataPath, "tts.mp3");
                await File.WriteAllBytesAsync(filePath, audioData);
                
                // Audio Clip으로 생성해 플레이 한다.
                using (UnityWebRequest wwwReceiver = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG)) // 지정한 경로에 있는 mp3파일을 오디오 클립으로 불러오는 요청
                {
                    await wwwReceiver.SendWebRequest();  // 실제로 mp3 파일을 비동기적으로 다운로드 시작함
                
                    // 요청 실패 시 에러 출력 후 함수 종료
                    if (wwwReceiver.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Google TTS API 요청 실패 (HTTP {wwwReceiver.responseCode}): {wwwReceiver.error}\n서버 응답: {wwwReceiver.downloadHandler.text}");
                        return null;
                    }
                
                    // 오디오 파일 재생
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(wwwReceiver);  // www에서 받아온 mp3 오디오 데이터를 AudioClip형식으로 바꿔서 clip 변수에 저장
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

                    // 3. 추출한 메시지 문자열을 우리가 원하는 GPTResponse 객체로 최종 파싱
                    GPTResponse finalResponse = JsonUtility.FromJson<GPTResponse>(gptContent);
                    return finalResponse;
                }
            }
        }
    }
}
    
