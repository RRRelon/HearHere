using System.Collections;
using HH;
using UnityEngine;

public abstract class Client : MonoBehaviour
{
    // 이 값보다 큰 소리가 감지되면 '말하기 시작'으로 판단
    [SerializeField] private float sensitivityThreshold = 0.02f;
    // 말하기가 끝났다고 판단하기 전까지 기다리는 시간 (초)
    [SerializeField] private float silenceDelay = 1.5f;
    [SerializeField] private int maxRecordingDuration = 10;

    [Header("AI")]
    [SerializeField] protected AIConversationManagerSO manager;
    [SerializeField] protected PromptSO prompt;
    [SerializeField] protected int promptNum = 0;
    
    [Header("Broadcasting to")]
    [SerializeField] private BoolEventChannelSO blinkScreenDark;
    
    // Flag
    private bool isListening; // 마이크 입력을 받으면 True, 아니면 False
    private bool isSpeaking;  // 마이크 녹음중이면 True, 아니면 False
    
    // 마이크 입력 관련
    private AudioClip monitoringClip;
    private AudioClip recordingClip;
    
    private float timeSinceLastSound;
    private string microphoneDevice;
    
    private void Start()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("마이크를 찾을 수 없습니다!");
            return;
        }
        microphoneDevice = Microphone.devices[0];

        StartMonitoring();
    }

    /// <summary>
    /// isListening은 마이크 입력 받을지 여부
    /// 소리 감지 되면 녹음 시작(isSpeaking = true)
    /// 침묵 진행되면 녹음 종료(isSpeaking = false)
    /// </summary>
    private void Update()
    {
        if (!isListening) return;

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
        Debug.Log("음성 모니터링을 시작합니다...");
        
        isListening = true; // 마이크 입력 받기
        // 상시 입력을 받기 위해 1초짜리 반복 녹음을 시작합니다.
        monitoringClip = Microphone.Start(microphoneDevice, true, 1, 44100);
    }
    
    private void StartActualRecording()
    {
        // 모니터링용 마이크를 중지하고,
        Microphone.End(microphoneDevice);
        
        // 실제 음성을 담을 새롭고 긴 클립으로 녹음을 다시 시작합니다. (반복X)
        recordingClip = Microphone.Start(microphoneDevice, false, maxRecordingDuration, 44100);
        Debug.Log("말하기 시작 감지! 녹음을 시작합니다.");
        
        // 색을 어둡게 해야 해
        if (blinkScreenDark != null)
            blinkScreenDark.OnEventRaised(true);
        
        isSpeaking = true; // 마이크 녹음 시작
    }

    private async void StopAndProcessUserInput()
    {
        Debug.Log("말하기 끝 감지! 분석을 시작합니다. (VAD 비활성화)");
        
        isSpeaking = false; // 마이크 녹음 중지
        isListening = false; // 발화 중에는 마이크 입력 중지

        // 실제 녹음을 종료하여 speechClip에 데이터를 확정합니다.
        Microphone.End(microphoneDevice);
        
        // STT 분석 함수 호출
        string userText = await manager.GetTextFromAudio(recordingClip);
        Debug.Log(userText);
        
        // 색을 다시 밝게 해야 해
        blinkScreenDark.OnEventRaised(false);
        
        ProcessUserInput(userText);
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

    /// <summary>
    /// 마이크 입력 비활성화
    /// </summary>
    protected void DisableInput()
    {
        isListening = false;
    }

    /// <summary>
    /// 마이크 입력 활성화
    /// </summary>
    protected void EnableInput()
    {
        isListening = true;
    }

    /// <summary>
    /// 마이크 입력에 대한 행동 수행
    /// </summary>
    protected virtual void ProcessUserInput(string text)
    {
        float totalDuration;
        if (string.IsNullOrWhiteSpace(text))
            totalDuration = 0f;
        else
            totalDuration = text.Length * 0.08f;
        
        StartCoroutine(DelayedStartMonitoring(totalDuration));
    }

    private IEnumerator DelayedStartMonitoring(float duration)
    {
        yield return new WaitForSeconds(duration);
        
        StartMonitoring();
    }
}
