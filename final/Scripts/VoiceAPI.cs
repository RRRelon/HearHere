using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

[System.Serializable]
public class VoiceChatResponse
{
    public bool success;
    public string user_text;
    public string ai_response;
    public string conversation_id;
    public string audio_data; // Base64 encoded TTS audio
    public int sample_rate;
    public float processing_time_seconds;
}

public class VoiceAPI : MonoBehaviour
{
    public string serverUrl = "http://localhost:8000";
    
    public event Action<VoiceChatResponse> OnVoiceChatComplete;
    public event Action<string> OnVoiceChatError;
    
    private string currentConversationId;
    
    void Start()
    {
        currentConversationId = System.Guid.NewGuid().ToString();
        Debug.Log($"대화 ID 생성: {currentConversationId}");
    }
    
    public void SendVoiceChat(byte[] audioData)
    {
        StartCoroutine(SendVoiceChatCoroutine(audioData));
    }
    
    private IEnumerator SendVoiceChatCoroutine(byte[] audioData)
    {
        Debug.Log($"음성 데이터 전송 시작: {audioData.Length} bytes");
        
        // Multipart form data 생성
        WWWForm form = new WWWForm();
        form.AddBinaryData("file", audioData, "audio.wav", "audio/wav");
        form.AddField("conversation_id", currentConversationId);
        
        string url = $"{serverUrl}/voice-chat";
        
        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            request.timeout = 30;
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    Debug.Log($"서버 응답: {jsonResponse.Substring(0, Math.Min(200, jsonResponse.Length))}...");
                    
                    VoiceChatResponse response = JsonUtility.FromJson<VoiceChatResponse>(jsonResponse);
                    OnVoiceChatComplete?.Invoke(response);
                }
                catch (Exception e)
                {
                    string error = $"JSON 파싱 오류: {e.Message}";
                    Debug.LogError($"{error}");
                    OnVoiceChatError?.Invoke(error);
                }
            }
            else
            {
                string error = $"서버 요청 실패: {request.error}";
                Debug.LogError($"{error}");
                OnVoiceChatError?.Invoke(error);
            }
        }
    }
    
    public void TestConnection()
    {
        StartCoroutine(TestConnectionCoroutine());
    }
    
    private IEnumerator TestConnectionCoroutine()
    {
        Debug.Log("서버 연결 테스트 시작...");
        
        using (UnityWebRequest request = UnityWebRequest.Get(serverUrl))
        {
            request.timeout = 5;
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Python 서버 연결 성공!");
            }
            else
            {
                Debug.LogError($"Python 서버 연결 실패: {request.error}");
            }
        }
    }
    
    public void StartNewConversation()
    {
        currentConversationId = System.Guid.NewGuid().ToString();
        Debug.Log($"새 대화 시작: {currentConversationId}");
    }
}