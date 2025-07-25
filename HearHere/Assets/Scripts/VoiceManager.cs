using UnityEngine;
using UnityEngine.UI;

public class VoiceManager : MonoBehaviour
{
    [Header("Components")]
    public MicRecorder micRecorder;
    public VoiceAPI voiceAPI;
    public AudioPlayer audioPlayer;
    
    [Header("UI Elements")]
    private Button recordButton;
    private Button stopButton;
    private Button testButton;
    private Text statusText;
    private Text userText;
    private Text aiText;
    
    private bool isProcessing = false;
    
    void Start()
    {
        Debug.Log("음성 채팅 매니저 시작!");
        
        // 컴포넌트 자동 찾기 또는 생성
        SetupComponents();
        
        // 이벤트 연결
        SetupEventListeners();
        
        // UI 생성
        CreateUI();
        
        // 서버 연결 테스트
        if (voiceAPI != null)
        {
            voiceAPI.TestConnection();
        }
        
        Debug.Log("음성 채팅 시스템 준비 완료!");
    }
    
    void SetupComponents()
    {
        // 컴포넌트들이 없으면 자동으로 생성
        if (micRecorder == null)
        {
            micRecorder = gameObject.AddComponent<MicRecorder>();
        }
        
        if (voiceAPI == null)
        {
            voiceAPI = gameObject.AddComponent<VoiceAPI>();
        }
        
        if (audioPlayer == null)
        {
            audioPlayer = gameObject.AddComponent<AudioPlayer>();
        }
        
        Debug.Log("컴포넌트 설정 완료");
    }
    
    void SetupEventListeners()
    {
        // 마이크 이벤트
        if (micRecorder != null)
        {
            micRecorder.OnRecordingStart += OnRecordingStart;
            micRecorder.OnRecordingStop += OnRecordingStop;
            micRecorder.OnRecordingComplete += OnRecordingComplete;
        }
        
        // API 이벤트
        if (voiceAPI != null)
        {
            voiceAPI.OnVoiceChatComplete += OnVoiceChatComplete;
            voiceAPI.OnVoiceChatError += OnVoiceChatError;
        }
        
        // 오디오 재생 이벤트
        if (audioPlayer != null)
        {
            audioPlayer.OnAudioPlayStart += OnAudioPlayStart;
            audioPlayer.OnAudioPlayComplete += OnAudioPlayComplete;
        }
        
        Debug.Log("이벤트 리스너 설정 완료");
    }
    
    void CreateUI()
    {
        // Canvas 생성
        GameObject canvasObj = new GameObject("VoiceChatCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // EventSystem
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        
        CreateStatusText(canvas.transform);
        CreateControlButtons(canvas.transform);
        CreateChatDisplay(canvas.transform);
        
        Debug.Log("UI 생성 완료");
    }
    
    void CreateStatusText(Transform parent)
    {
        GameObject statusObj = new GameObject("StatusText");
        statusObj.transform.SetParent(parent, false);
        
        statusText = statusObj.AddComponent<Text>();
        statusText.text = "Unity 음성 채팅 준비됨";
        statusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 20;
        statusText.color = Color.white;
        statusText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform rect = statusObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0.8f);
        rect.anchorMax = new Vector2(1, 0.95f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
    
    void CreateControlButtons(Transform parent)
    {
        // 녹음 시작 버튼
        recordButton = CreateButton(parent, "녹음 시작", new Vector2(-150, 0), Color.green, StartRecording);
        
        // 녹음 중지 버튼
        stopButton = CreateButton(parent, "녹음 중지", new Vector2(0, 0), Color.red, StopRecording);
        stopButton.interactable = false;
        
        // 연결 테스트 버튼
        testButton = CreateButton(parent, "서버 테스트", new Vector2(150, 0), Color.blue, TestConnection);
    }
    
    Button CreateButton(Transform parent, string text, Vector2 position, Color color, System.Action onClick)
    {
        GameObject buttonObj = new GameObject($"Button_{text}");
        buttonObj.transform.SetParent(parent, false);
        
        Image bg = buttonObj.AddComponent<Image>();
        bg.color = color;
        
        Button button = buttonObj.AddComponent<Button>();
        button.onClick.AddListener(() => onClick());
        
        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 60);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        
        // 버튼 텍스트
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 14;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        return button;
    }
    
    void CreateChatDisplay(Transform parent)
    {
        // 사용자 발화 텍스트
        GameObject userObj = new GameObject("UserText");
        userObj.transform.SetParent(parent, false);
        
        userText = userObj.AddComponent<Text>();
        userText.text = "사용자: (아직 없음)";
        userText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        userText.fontSize = 16;
        userText.color = Color.cyan;
        
        RectTransform userRect = userObj.GetComponent<RectTransform>();
        userRect.anchorMin = new Vector2(0.05f, 0.2f);
        userRect.anchorMax = new Vector2(0.95f, 0.3f);
        userRect.offsetMin = Vector2.zero;
        userRect.offsetMax = Vector2.zero;
        
        // AI 응답 텍스트
        GameObject aiObj = new GameObject("AIText");
        aiObj.transform.SetParent(parent, false);
        
        aiText = aiObj.AddComponent<Text>();
        aiText.text = "AI: (아직 없음)";
        aiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        aiText.fontSize = 16;
        aiText.color = Color.yellow;
        
        RectTransform aiRect = aiObj.GetComponent<RectTransform>();
        aiRect.anchorMin = new Vector2(0.05f, 0.05f);
        aiRect.anchorMax = new Vector2(0.95f, 0.15f);
        aiRect.offsetMin = Vector2.zero;
        aiRect.offsetMax = Vector2.zero;
    }
    
    // 버튼 이벤트 핸들러
    void StartRecording()
    {
        if (isProcessing) return;
        
        micRecorder?.StartRecording();
    }
    
    void StopRecording()
    {
        micRecorder?.StopRecording();
    }
    
    void TestConnection()
    {
        voiceAPI?.TestConnection();
    }
    
    // 마이크 이벤트 핸들러
    void OnRecordingStart()
    {
        statusText.text = "녹음 중... 말씀하세요!";
        recordButton.interactable = false;
        stopButton.interactable = true;
    }
    
    void OnRecordingStop()
    {
        statusText.text = "음성을 처리하고 있습니다...";
        isProcessing = true;
        stopButton.interactable = false;
    }
    
    void OnRecordingComplete(byte[] audioData)
    {
        Debug.Log($"녹음 완료: {audioData.Length} bytes");
        voiceAPI?.SendVoiceChat(audioData);
    }
    
    // API 이벤트 핸들러
    void OnVoiceChatComplete(VoiceChatResponse response)
    {
        if (response.success)
        {
            // 텍스트 표시
            userText.text = $"사용자: {response.user_text}";
            aiText.text = $"AI: {response.ai_response}";
            
            statusText.text = "AI 음성 재생 중...";
            
            // TTS 오디오 재생
            if (!string.IsNullOrEmpty(response.audio_data))
            {
                audioPlayer?.PlayTTSAudio(response.audio_data, response.sample_rate);
            }
            else
            {
                OnAudioPlayComplete(); // 오디오가 없으면 바로 완료 처리
            }
        }
        else
        {
            statusText.text = "음성 처리 실패";
            isProcessing = false;
            recordButton.interactable = true;
        }
    }
    
    void OnVoiceChatError(string error)
    {
        statusText.text = $"오류: {error}";
        isProcessing = false;
        recordButton.interactable = true;
    }
    
    // 오디오 재생 이벤트 핸들러
    void OnAudioPlayStart()
    {
        statusText.text = "AI 음성 재생 중...";
    }
    
    void OnAudioPlayComplete()
    {
        statusText.text = "음성 채팅 완료! 다시 녹음하세요.";
        isProcessing = false;
        recordButton.interactable = true;
    }
    
    // 키보드 단축키
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !isProcessing)
        {
            if (!micRecorder.IsRecording())
            {
                StartRecording();
            }
            else
            {
                StopRecording();
            }
        }
    }
}