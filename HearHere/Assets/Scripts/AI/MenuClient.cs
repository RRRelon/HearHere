using System;
using System.Collections;
using System.Data;
using UnityEngine;
using HH;
using UnityEngine.Serialization;

/// <summary>
/// Unity에서 FastAPI 백엔드 서버와 통신하는 클라이언트 스크립트입니다.
/// TTS, STT, Chat, Voice Chat 기능을 담당합니다.
/// </summary>
public class MenuClient : Client
{
    [Header("Audio")]
    [SerializeField] private AudioScaleManagerSO audioScaleManager;
    [SerializeField] private AudioClip sourceNoteClip;
    [SerializeField] private float noteDuration = 0.3f;
    
    // Game Management
    [Header("Scene Management")]
    [SerializeField] private GameSceneSO currentlyLoadedScene;
    [SerializeField] private GameSceneSO gameToLoad;
    [SerializeField] private GameSceneSO tutorialToLoad;
    
    [Header("Player Data")]
    [SerializeField] private PlayerDataSO playerData;
    
    [Header("Broadcasting on")]
    [SerializeField] private LoadEventChannelSO loadLocation;
    
    /// <summary>
    /// 메인 메뉴 시작 시마다 현재 데이터를 읽어준다.
    /// 처음 기록 대비 최근 기록을 TTS로 안내한다.
    /// </summary>
    protected override void Start()
    {
        // 첫 시작 때는 환영 TTS를 뱉는다.
        if (GameSessionManager.IsFirstLaunchOfSession)
        {
            EnqueueRequestTTS(playbackStr, true);
            
            GameSessionManager.CompleteFirstLaunch();
        }
        
        // 1. 기록이 2개 이상이면 실력 향상 지표 알려주기
        if (playerData.Datas.Count >= 1)
        {
            // 1. 첫 기록(playerData.Datas[0])과 그 외 기록이 있는 경우 TTS로 안내
            string message1 = ""; // 처음 vs 현재
            string message2 = ""; // 이전 vs 현재
            string message3 = ""; // 종합 평가
            string message4 = "I will represent the overall performance using a musical scale.";
            
            // 2. 처음 vs 현재. 능력이 향상이 된 경우
            if (playerData.IsImproveThanFirst())
            {
                int improvementPercentage = playerData.GetImprovementPercentageThanFirst();
                message1 = improvementPercentage > 0 // firstRecord == 0인 경우 예외 처리
                    ? $"s"                           // 향상도
                    : "Keep up the great work!";     // 간단한 격려
            }
            
            // 3. 이전 vs 현재. 능력이 향상이 된 경우
            if (playerData.IsImproveThanPrevious())
            {
                // 성능 향상됨 -> 퍼센트 계산 및 칭찬 메시지 생성
                // previousRecord == 0인 경우 예외 처리
                int improvementPercentage = playerData.GetImprovementPercentageThanPrevious();
                if (improvementPercentage > 0)
                {
                    playerData.SequentialDecrease = 0; // 연속 감소 정도 초기화
                    message2 = $"You're {improvementPercentage}% faster than your before record. That's amazing!";
                }
                else
                {
                    // 퍼센트를 계산할 수 없는 경우, 간단한 칭찬 메시지
                    message2 = "Your recent record has improved. Keep up the great work!";
                }
            }
            // 3. 하락 시
            else
            {
                playerData.SequentialDecrease += 1;
                if (playerData.SequentialDecrease < 5)
                {
                    // 격려 메시지
                    message3 = "It's not your best record, but consistency is key. You can do better next time!";
                }
                // 5번 연속 하강 시, 능력이 악화 된 경우
                else
                {
                    // 의료진 만나보세요
                    message3 = "It might be a good idea to speak with a healthcare professional.";
                }
            }
            
            EnqueueRequestTTS(message1 + message2 + message3 + message4, true);
            EnqueueRequestTTS(audioScaleManager.CreateMelodyClip(playerData.SequentialRecords, sourceNoteClip, 0.3f), false);
        }
        else
        {
            EnqueueRequestTTS(playbackStr, true);
        }
        
        base.Start();
    }
    
    /// <summary>
    /// 사용자 입력에 대한 처리를 우선적으로 한 뒤 필요 시 GPT 응답에 대한 처리 진행
    /// </summary>
    protected override void ProcessUserInput(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText))
        {
            Debug.Log("입력 값이 Null 입니다.");
            return;
        }
        
        userText = userText.ToLower().Replace(".", "").Replace("?", "");
        
        Debug.Log($"user input : {userText}");

        // 메뉴 설명
        if (CheckSystemOperationInput(userText, menuInfoTargets, menuInfoActions))
        {
            EnqueueRequestTTS("Available commands are Start Game and Exit Game.", false);
            return;
        }
        
        // 튜토리얼 시작
        if (CheckSystemOperationInput(userText, tutorialTargets, tutorialActions))
        {
            EnqueueRequestTTS("Starting the tutorial.", true);
            onTextReadyForTTS.OnEventRaised("Starting the tutorial.", true);
            // TTS 응답 속도에 대응하기 위해 조금 기다렸다 씬 로딩 
            StartCoroutine(DelaySceneLoad(5.0f, tutorialToLoad));
            return;
        }
        
        // 게임 시작
        if (CheckSystemOperationInput(userText, startGameTargets, startGameActions))
        {
            EnqueueRequestTTS("Starting the game.", true);
            // TTS 응답 속도에 대응하기 위해 조금 기다렸다 씬 로딩 
            StartCoroutine(DelaySceneLoad(5.0f, gameToLoad));
            return;
        }
            
        // 게임 종료
        if (CheckSystemOperationInput(userText, exitTargets, exitActions))
        {
            EnqueueRequestTTS("Exit game.", true);
            StartCoroutine(ExitGame());
            return;
        }
        
        // 아무 처리도 못했을 경우
        Debug.Log("아무 처리도 못함");
        onTextReadyForTTS.OnEventRaised(playbackStr, false);
    }
    
    /// <summary>
    /// 게임 종료 메서드
    /// </summary>
    private IEnumerator ExitGame()
    {
        yield return new WaitForSeconds(5.0f);
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
    
    /// <summary>
    /// 일정 시간 뒤 씬 로딩. TTS 끝나는걸 기다리기 위해 필요
    /// </summary>
    private IEnumerator DelaySceneLoad(float waitTime, GameSceneSO sceneToLoad)
    {
        yield return new WaitForSeconds(waitTime);
        loadLocation.OnLoadingRequested(sceneToLoad, true, true);
    }
}