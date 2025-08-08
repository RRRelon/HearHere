using System;
using System.Collections;
using HH;
using UnityEngine;

public abstract class Client : MonoBehaviour
{
    [Header("Playback Sound")]
    [TextArea(5, 30)]
    [SerializeField] protected string playbackStr = "Please say that again with the correct answer.";
    [SerializeField] protected float playbackInterval = 30.0f;
    [SerializeField] protected float playbackTimer;
    
    // 마이크 사용 여부 설정
    [SerializeField] private string userInputByText;
    [SerializeField] private bool useMic = true;
    // Input
    [SerializeField] private InputReader inputReader;
    
    // 이 값보다 큰 소리가 감지되면 '말하기 시작'으로 판단
    [SerializeField] private float sensitivityThreshold = 0.02f;
    // 말하기가 끝났다고 판단하기 전까지 기다리는 시간 (초)
    [SerializeField] private float silenceDelay = 3f;
    [SerializeField] private int maxRecordingDuration = 15;

    [Header("AI")]
    [SerializeField] protected AIConversationManagerSO manager;
    [SerializeField] protected PromptSO prompt;
    
    [Header("Broadcasting to")]
    [SerializeField] protected StringEventChannelSO onTextReadyForTTS;
    [SerializeField] private BoolEventChannelSO blinkScreenDark;
    
    // Flag
    [SerializeField] protected bool isListening; // 마이크 입력을 받으면 True, 아니면 False
    [SerializeField] protected bool isSpeaking;  // 마이크 녹음중이면 True, 아니면 False
    
    protected float totalPlayTime = 0;
    
    // 메뉴 정보 명령어
    protected readonly string[] menuInfoTargets = { "menu" };
    protected readonly string[] menuInfoActions = { "tell me", "what is", "explain", "describe" };
    // 메인 메뉴로 이동 명령어
    protected readonly string[] menuTargets = { "main", "first" };
    protected readonly string[] menuActions = { "menu", "screen", "move", "return", "go" };
    // 게임 종료 명령어
    protected readonly string[] exitTargets = { "game", "application", "program" };
    protected readonly string[] exitActions = { "exit", "quit", "turn off", "close" };
    // 튜토리얼 시작 관련 키워드
    protected string[] tutorialTargets = { "tutorial" };
    protected string[] tutorialActions = { "start", "begin" };
    // 게임 시작 관련 키워드
    protected string[] gameTargets = { "game" };
    protected string[] gameActions = { "start", "begin", "play" };
    
    // 마이크 입력 관련
    private AudioClip monitoringClip;
    private AudioClip recordingClip;
    private float timeSinceLastSound;
    private string microphoneDevice;

    protected virtual void OnEnable()
    {
        if (!useMic)
        {
            inputReader.SpeechEvent += ProcessUserInputByText;
        }
    }

    protected virtual void OnDisable()
    {
        if (!useMic)
        {
            inputReader.SpeechEvent -= ProcessUserInputByText;
        }
    }

    private void ProcessUserInputByText()
    {
        ProcessUserInput(userInputByText);
    }
    

    protected virtual void Start()
    {
        // 1. Playback 재생
        onTextReadyForTTS.OnEventRaised(playbackStr);
        
        // 2. 텍스트로 입력 넣으면 여기서 리턴
        if (!useMic)
            return;
        
        // 3. 마이크 설정
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("There are no Microphone devices available!");
            microphoneDevice = null;
            return;
        }
        microphoneDevice = Microphone.devices[0];
        
        // 4. Playback 재생
        Debug.Log("플레이 백 재생");
        onTextReadyForTTS.OnEventRaised(playbackStr);
        
        // 5. 마이크 입력 대기
        StartMonitoring();
    }

    /// <summary>
    /// isListening은 마이크 입력 받을지 여부
    /// 소리 감지 되면 녹음 시작(isSpeaking = true)
    /// 침묵 진행되면 녹음 종료(isSpeaking = false)
    /// </summary>
    protected virtual void Update()
    {
        // 전체 플레이 시간
        totalPlayTime += Time.deltaTime;
        
        // 게임 안내 playback cooltime
        playbackTimer += Time.deltaTime;
        
        if (playbackTimer >= playbackInterval)
        {
            onTextReadyForTTS.OnEventRaised(playbackStr);
            playbackTimer = 0;
        }
        
        if (!useMic) return;
        
        // 만약 마이크를 못 찾았을 시, 재할당 시도
        if (microphoneDevice == null)
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("There are no Microphone devices available!");
                microphoneDevice = null;
                return;
            }
            microphoneDevice = Microphone.devices[0];
        }
        
        // 만약, isListening이 True면 마이크 입력 받지 않음 
        if (!isListening)
            return;

        // 현재 마이크 볼륨 측정
        float currentVolume = GetAverageVolume();

        if (currentVolume > sensitivityThreshold)
        {
            // 소리가 감지됨
            if (!isSpeaking)
            {
                StartActualRecording();             
            }
            timeSinceLastSound = 0f; // 마지막 소리 감지 시간 초기화
        }
        else
        {
            // 조용한 상태
            if (isSpeaking)
            {
                timeSinceLastSound += Time.deltaTime;
                if (timeSinceLastSound > silenceDelay)
                {
                    StopAndProcessUserInput();
                }
            }
        }
    }
    
    private void StartMonitoring()
    {
        Debug.Log("Starting mic monitoring...");
        
        isListening = true; // 마이크 입력 받기
        // 상시 입력을 받기 위해 1초짜리 반복 녹음 시작
        monitoringClip = Microphone.Start(microphoneDevice, true, 1, 44100);
    }
    
    private void StartActualRecording()
    {
        // 모니터링용 마이크를 중지하고,
        Microphone.End(microphoneDevice);
        
        // 실제 음성을 담을 새롭고 긴 클립으로 녹음을 다시 시작합니다. (반복X)
        recordingClip = Microphone.Start(microphoneDevice, false, maxRecordingDuration, 44100);
        Debug.Log("Sensing mic! Start Recording.");
        
        // 색을 어둡게 해야 해
        if (blinkScreenDark != null)
            blinkScreenDark.OnEventRaised(true);
        
        isSpeaking = true; // 마이크 녹음 시작
    }

    private async void StopAndProcessUserInput()
    {
        Debug.Log("Done recording! Try to convert. (Deactivate VAD)");
        
        isSpeaking = false; // 마이크 녹음 중지
        isListening = false; // 발화 중에는 마이크 입력 중지

        // 실제 녹음을 종료하여 speechClip에 데이터를 확정합니다.
        Microphone.End(microphoneDevice);
        
        // 색을 다시 밝게 해야 해
        blinkScreenDark.OnEventRaised(false);
        
        // STT 분석 함수 호출
        string userText = await manager.GetTextFromAudio(recordingClip);
        Debug.Log(userText);
        
        ProcessUserInput(userText);
    }

    /// <summary>
    /// TTS, GPT를 거친 응답 텍스트를 TTS로 재생 
    /// </summary>
    protected virtual async void ProcessUserInput(string text)
    {
        // 1. Playback 쿨타임 다시 돌리기
        playbackTimer = 0f;
        
        // 2. TTS 길이 계산
        float totalDuration;
        if (string.IsNullOrWhiteSpace(text))
            totalDuration = 0f;
        else
            totalDuration = text.Length * 0.08f;
        
        if (useMic)
            StartCoroutine(DelayedStartMonitoring(totalDuration + 2.0f));
        onTextReadyForTTS.OnEventRaised(text);
    }

    protected void EnableInput()
    {
        isListening = true;
    }
    
    protected void DisableInput()
    {
        isListening = false;
    }

    protected bool CheckSystemOperationInput(string text, string[] targets, string[] actions)
    {
        bool isMenuInfoTargetMatch = false;
        foreach (var target in targets)
        {
            if (text.Contains(target))
            {
                isMenuInfoTargetMatch = true;
                break;
            }
        }
        if (isMenuInfoTargetMatch)
        {
            foreach (var action in actions)
            {
                if (text.Contains(action))
                {
                    return true;
                }
            }
        }

        return false;
    }
    
    /// <summary>
    /// 초(float)를 "X시간 Y분 Z초 걸렸습니다." 형식의 문자열로 변환합니다.
    /// </summary>
    /// <param name="totalSeconds">총 경과 시간 (초)</param>
    /// <returns>형식에 맞게 변환된 시간 문자열</returns>
    protected string FormatPlayTime(float totalSeconds)
    {
        // 1. 초(float)를 TimeSpan 객체로 변환합니다.
        TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);

        string formattedTime;

        // 2. 전체 시간이 1시간 이상인지, 1분 이상인지에 따라 다른 형식으로 만듭니다.
        if (timeSpan.TotalHours >= 1)
        {
            // 1시간 이상: "1시간 5분 10초" 형식
            formattedTime = string.Format("{0}hour {1}minute {2}second", 
                timeSpan.Hours, 
                timeSpan.Minutes, 
                timeSpan.Seconds);
        }
        else if (timeSpan.TotalMinutes >= 1)
        {
            // 1분 이상 1시간 미만: "5분 10초" 형식
            formattedTime = string.Format("{0}minute {1}second", 
                timeSpan.Minutes, 
                timeSpan.Seconds);
        }
        else
        {
            // 1분 미만: "10초" 형식
            formattedTime = string.Format("{0}second", 
                timeSpan.Seconds);
        }

        // 3. 최종 문자열을 조합하여 반환합니다.
        Debug.Log($"it takes {formattedTime}, {timeSpan.Seconds} seconds");
        return $"it takes {formattedTime}";
    }

    private IEnumerator DelayedStartMonitoring(float duration)
    {
        yield return new WaitForSeconds(duration);
        
        StartMonitoring();
    }
    
    /// <summary>
    /// 모니터링 클립의 평균 볼륨을 계산하는 함수 
    /// </summary>
    private float GetAverageVolume()
    {
        if (!Microphone.IsRecording(microphoneDevice)) return 0;
        
        float[] data = new float[256];
        int micPosition = Microphone.GetPosition(microphoneDevice) - (256 + 1);
        if (micPosition < 0) return 0;
        
        // 발화 중이면 녹음용 마이크 가져오기
        if (isSpeaking)
            recordingClip.GetData(data, micPosition);
        // 발화 전에는 모니터링 마이크 가져오기
        else
            monitoringClip.GetData(data, micPosition);
        
        float a = 0;
        foreach (float s in data)
        {
            a += Mathf.Abs(s);
        }
        
        return a / 256;
    }
}
