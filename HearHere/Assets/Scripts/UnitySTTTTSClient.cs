using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using System.Text;

/// <summary>
/// Unity에서 FastAPI 백엔드 서버와 통신하는 클라이언트 스크립트입니다.
/// TTS, STT, Chat, Voice Chat 기능을 담당합니다.
/// </summary>
public class UnityTTSSTTClient : MonoBehaviour
{
    [Header("서버 설정")]
    [Tooltip("백엔드 서버의 URL을 입력하세요.")]
    public string serverUrl = "http://localhost:8000";

    [Header("컴포넌트 연결")]
    [Tooltip("TTS 음성을 재생할 AudioSource 컴포넌트입니다.")]
    public AudioSource audioSource;

    // 녹음 관련 변수
    private const int RECORDING_FREQUENCY = 16000; // 16kHz 샘플링
    private const int RECORDING_DURATION_SECONDS = 5; // 최대 녹음 시간
    private string _microphoneDevice;
    private AudioClip _recordedClip;

    #region JSON 데이터 구조체
    // 서버와 JSON 데이터를 주고받기 위한 직렬화 가능 클래스들

    [System.Serializable]
    private class TTSRequest
    {
        public string text;
        public float exaggeration;
        public float cfg_weight;
    }

    [System.Serializable]
    private class ChatRequest
    {
        public string message;
        public string conversation_id;
    }

    [System.Serializable]
    private class APIResponse
    {
        public bool success;
        public string user_text;
        public string ai_response;
        public string conversation_id;
        public string audio_file_id;
        public string download_url;
        public string text; // STT 응답용
    }
    #endregion

    void Awake()
    {
        // 사용할 AudioSource가 없으면 새로 추가
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 사용 가능한 마이크 장치 확인
        if (Microphone.devices.Length > 0)
        {
            _microphoneDevice = Microphone.devices[0];
            Debug.Log($"사용할 마이크: {_microphoneDevice}");
        }
        else
        {
            Debug.LogError("오류: 사용 가능한 마이크 장치를 찾을 수 없습니다!");
        }
    }

    #region Public API Methods
    // GameManager에서 호출할 공개 메소드들

    /// <summary>
    /// 텍스트를 음성으로 변환하고 재생합니다.
    /// </summary>
    public void ConvertTextToSpeech(string text, float exaggeration = 0.5f, float cfg_weight = 0.5f)
    {
        StartCoroutine(RequestTTS(text, exaggeration, cfg_weight));
    }

    /// <summary>
    /// AI에게 텍스트 메시지를 보냅니다.
    /// </summary>
    public void SendChatMessage(string message, string conversationId = null)
    {
        StartCoroutine(RequestChat(message, conversationId));
    }
    
    /// <summary>
    /// 녹음된 오디오 클립을 텍스트로 변환합니다.
    /// </summary>
    public void ConvertRecordedAudioToText(AudioClip clip)
    {
        StartCoroutine(RequestSTT(clip));
    }

    /// <summary>
    /// 음성 녹음을 시작하고, 녹음이 끝나면 AI와 대화를 시작합니다.
    /// </summary>
    public void StartVoiceRecording(float exaggeration = 0.5f, float cfg_weight = 0.5f, string conversationId = null)
    {
        if (Microphone.devices.Length == 0) return;
        StartCoroutine(RecordAndProcessVoiceChat(exaggeration, cfg_weight, conversationId));
    }

    /// <summary>
    /// 테스트용 음성 채팅을 시작합니다. (5초 녹음)
    /// </summary>
    public void TestVoiceChat()
    {
        Debug.Log("테스트 음성 채팅 시작: 5초간 녹음합니다...");
        StartVoiceRecording(0.7f, 0.5f);
    }

    #endregion

    #region Coroutines (API 요청 처리)

    private IEnumerator RequestTTS(string text, float exaggeration, float cfg_weight)
    {
        string url = $"{serverUrl}/tts";
        Debug.Log($"[TTS 요청] URL: {url}, Text: {text}");

        // 1. TTS 요청 객체 생성 및 JSON으로 변환
        TTSRequest ttsRequest = new TTSRequest
        {
            text = text,
            exaggeration = exaggeration,
            cfg_weight = cfg_weight
        };
        string jsonBody = JsonUtility.ToJson(ttsRequest);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        // 2. UnityWebRequest 생성 및 설정
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // 3. 요청 보내고 응답 기다리기
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // 4. 응답 성공 시, 다운로드 URL로 음성 파일 요청
                Debug.Log("[TTS 응답] 성공: " + request.downloadHandler.text);
                APIResponse response = JsonUtility.FromJson<APIResponse>(request.downloadHandler.text);
                if (response.success)
                {
                    yield return StartCoroutine(DownloadAndPlayAudio(response.download_url));
                }
            }
            else
            {
                Debug.LogError($"[TTS 응답] 실패: {request.error}\n{request.downloadHandler.text}");
            }
        }
    }

    private IEnumerator RequestChat(string message, string conversationId)
    {
        string url = $"{serverUrl}/chat";
        Debug.Log($"[Chat 요청] URL: {url}, Message: {message}");

        ChatRequest chatRequest = new ChatRequest { message = message, conversation_id = conversationId };
        string jsonBody = JsonUtility.ToJson(chatRequest);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[Chat 응답] 성공: " + request.downloadHandler.text);
                // TODO: 채팅 응답 텍스트를 UI에 표시하는 로직 추가
            }
            else
            {
                Debug.LogError($"[Chat 응답] 실패: {request.error}\n{request.downloadHandler.text}");
            }
        }
    }

    private IEnumerator RequestSTT(AudioClip clip)
    {
        string url = $"{serverUrl}/stt";
        Debug.Log("[STT 요청] 오디오 클립을 텍스트로 변환합니다.");

        byte[] wavData = ConvertAudioClipToWav(clip);
        if (wavData == null)
        {
            Debug.LogError("[STT 요청] 실패: WAV 데이터 변환에 실패했습니다.");
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "audio.wav", "audio/wav");

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[STT 응답] 성공: " + request.downloadHandler.text);
                APIResponse response = JsonUtility.FromJson<APIResponse>(request.downloadHandler.text);
                Debug.Log($"변환된 텍스트: {response.text}");
                // TODO: 변환된 텍스트를 UI에 표시하는 로직 추가
            }
            else
            {
                Debug.LogError($"[STT 응답] 실패: {request.error}\n{request.downloadHandler.text}");
            }
        }
    }

    private IEnumerator RecordAndProcessVoiceChat(float exaggeration, float cfg_weight, string conversationId)
    {
        // 1. 마이크 녹음 시작
        Debug.Log("마이크 녹음을 시작합니다...");
        _recordedClip = Microphone.Start(_microphoneDevice, false, RECORDING_DURATION_SECONDS, RECORDING_FREQUENCY);
        yield return new WaitForSeconds(RECORDING_DURATION_SECONDS); // 지정된 시간만큼 녹음
        Microphone.End(_microphoneDevice);
        Debug.Log("마이크 녹음이 종료되었습니다.");

        // 2. 녹음된 오디오를 서버로 전송
        yield return StartCoroutine(RequestVoiceChat(_recordedClip, exaggeration, cfg_weight, conversationId));
    }

    private IEnumerator RequestVoiceChat(AudioClip clip, float exaggeration, float cfg_weight, string conversationId)
    {
        string url = $"{serverUrl}/voice-chat";
        Debug.Log("[VoiceChat 요청] 녹음된 음성을 서버로 전송합니다.");

        byte[] wavData = ConvertAudioClipToWav(clip);
        if (wavData == null)
        {
            Debug.LogError("[VoiceChat 요청] 실패: WAV 데이터 변환에 실패했습니다.");
            yield break;
        }

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavData, "audio.wav", "audio/wav");
        form.AddField("exaggeration", exaggeration.ToString());
        form.AddField("cfg_weight", cfg_weight.ToString());
        if (!string.IsNullOrEmpty(conversationId))
        {
            form.AddField("conversation_id", conversationId);
        }

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[VoiceChat 응답] 성공: " + request.downloadHandler.text);
                APIResponse response = JsonUtility.FromJson<APIResponse>(request.downloadHandler.text);
                if (response.success)
                {
                    Debug.Log($"사용자 음성: {response.user_text}");
                    Debug.Log($"AI 응답: {response.ai_response}");
                    yield return StartCoroutine(DownloadAndPlayAudio(response.download_url));
                }
            }
            else
            {
                Debug.LogError($"[VoiceChat 응답] 실패: {request.error}\n{request.downloadHandler.text}");
            }
        }
    }

    private IEnumerator DownloadAndPlayAudio(string downloadUrl)
    {
        string fullUrl = $"{serverUrl}{downloadUrl}";
        Debug.Log($"[Audio 다운로드] URL: {fullUrl}");

        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(fullUrl, AudioType.WAV))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                AudioClip downloadedClip = DownloadHandlerAudioClip.GetContent(request);
                audioSource.clip = downloadedClip;
                audioSource.Play();
                Debug.Log("다운로드한 오디오를 재생합니다.");
            }
            else
            {
                Debug.LogError($"[Audio 다운로드] 실패: {request.error}");
            }
        }
    }

    #endregion

    #region 오디오 데이터 변환 헬퍼 (AudioClip to WAV)
    // AudioClip 데이터를 WAV 파일 형식의 byte 배열로 변환하는 유틸리티 코드

    private byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        if (clip == null) return null;

        using (var memoryStream = new MemoryStream())
        {
            // WAV 헤더 작성
            memoryStream.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);
            memoryStream.Write(new byte[4], 0, 4); // File size (placeholder)
            memoryStream.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);
            memoryStream.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);
            memoryStream.Write(BitConverter.GetBytes(16), 0, 4); // Sub-chunk size (16 for PCM)
            memoryStream.Write(BitConverter.GetBytes((ushort)1), 0, 2); // Audio format (1 for PCM)
            memoryStream.Write(BitConverter.GetBytes(clip.channels), 0, 2); // Number of channels
            memoryStream.Write(BitConverter.GetBytes(clip.frequency), 0, 4); // Sample rate
            memoryStream.Write(BitConverter.GetBytes(clip.frequency * clip.channels * 2), 0, 4); // Byte rate
            memoryStream.Write(BitConverter.GetBytes((ushort)(clip.channels * 2)), 0, 2); // Block align
            memoryStream.Write(BitConverter.GetBytes((ushort)16), 0, 2); // Bits per sample

            memoryStream.Write(Encoding.ASCII.GetBytes("data"), 0, 4);
            memoryStream.Write(new byte[4], 0, 4); // Data size (placeholder)

            // 오디오 데이터 작성
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            for (int i = 0; i < samples.Length; i++)
            {
                short value = (short)(samples[i] * short.MaxValue);
                memoryStream.Write(BitConverter.GetBytes(value), 0, 2);
            }

            // 플레이스홀더에 실제 크기 업데이트
            long fileSize = memoryStream.Length;
            memoryStream.Seek(4, SeekOrigin.Begin);
            memoryStream.Write(BitConverter.GetBytes((int)(fileSize - 8)), 0, 4);
            memoryStream.Seek(40, SeekOrigin.Begin);
            memoryStream.Write(BitConverter.GetBytes((int)(fileSize - 44)), 0, 4);

            return memoryStream.ToArray();
        }
    }
    #endregion
}

