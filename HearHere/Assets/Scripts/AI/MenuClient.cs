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
    // Game Management
    [Header("Scene Management")]
    [SerializeField] private GameSceneSO currentlyLoadedScene;
    [SerializeField] private GameSceneSO gameToLoad;
    [SerializeField] private GameSceneSO tutorialToLoad;
    
    [Header("Player Data")]
    [SerializeField] private PlayerDataSO playerData;
    
    [Header("Broadcasting on")]
    [SerializeField] private LoadEventChannelSO loadLocation;

    [SerializeField] private float playbackInterval = 30.0f;
    [SerializeField] private float playbackTimer;
    
    /// <summary>
    /// 메인 메뉴 시작 시마다 현재 데이터를 읽어준다.
    /// 처음 기록 대비 최근 기록을 TTS로 안내한다.
    /// </summary>
    protected override void Start()
    {
        base.Start();
        
        playbackStr = "Welcome to Hear, Here!, the escape room game where you find the target sound. " +
                      "To start the tutorial, please say, 'Start tutorial'. " +
                      "To begin the game, please say, 'Start game'";

        if (playerData.Datas.Count > 0)
        {
            foreach (var data in playerData.Datas)
            {
                Debug.Log($"current player datas : {data.Time}, {data.TryCount}");
            }    
        }
        else
        {
            Debug.Log("There is no player data");
        }
        
        
        // 1. 첫 기록이 없고, 최근 기록도 없다면 그냥 넘어가기
        // 첫 기록(playerData.Datas[0])만 있고 다른 기록이 없는 경우 그냥 넘어가기
        if (playerData.Datas.Count <= 1)
        {
            // 먼저 말할 기록이 없으면 기본 안내 문장 TTS
            onTextReadyForTTS.OnEventRaised(playbackStr);
            return;   
        }
        
        // 2. 첫 기록(playerData.Datas[0])과 그 외 기록이 있는 경우 TTS로 안내

        float firstRecord = playerData.Datas[0].GetAverage();
        float lastRecord  = playerData.Datas[^1].GetAverage();

        string message;
        
        // 능력이 향상이 된 경우
        if (lastRecord < firstRecord)
        {
            // 성능 향상됨 -> 퍼센트 계산 및 칭찬 메시지 생성
            // firstRecord == 0인 경우 예외 처리
            if (firstRecord > 0)
            {
                // 향상률(%) = (이전 값 - 현재 값) / 이전 값 * 100
                float improvement = ((firstRecord - lastRecord) / firstRecord) * 100f;
                int improvementPercentage = Mathf.RoundToInt(improvement);

                message = $"You're {improvementPercentage}% faster than your first record. That's amazing!";
            }
            else
            {
                // 퍼센트를 계산할 수 없는 경우, 간단한 칭찬 메시지
                message = "Your recent record has improved. Keep up the great work!";
            }
        }
        // 능력이 향상이 된 경우
        else
        {
            // 성능이 그대로거나 나빠짐 -> 격려 메시지 생성
            message = "It's not your best record, but consistency is key. You can do better next time!";
        }

        onTextReadyForTTS.OnEventRaised(message);
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (!isListening)
        {
            playbackTimer = 0;
            return;
        }
        
        // 게임 안내 playback cooltime
        playbackTimer += Time.deltaTime;
        
        if (playbackTimer >= playbackInterval)
        {
            onTextReadyForTTS.OnEventRaised(playbackStr);
            playbackTimer = 0;
        }
    }

    /// <summary>
    /// 사용자 입력에 대한 처리를 우선적으로 한 뒤 필요 시 GPT 응답에 대한 처리 진행
    /// </summary>
    protected override void ProcessUserInput(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText))
        {
            Debug.Log("입력 값이 Null 입니다.");
            base.ProcessUserInput("");
            return;
        }
        
        userText = userText.ToLower().Replace(".", "").Replace("?", "");

        // 메뉴 설명
        if (CheckSystemOperationInput(userText, menuInfoTargets, menuInfoActions))
        {
            base.ProcessUserInput("Available commands are Start Game and Exit Game.");
            return;
        }
        
        // 튜토리얼 시작
        if (CheckSystemOperationInput(userText, tutorialTargets, tutorialActions))
        {
            base.ProcessUserInput( "Starting the tutorial.");
            // TTS 응답 속도에 대응하기 위해 조금 기다렸다 씬 로딩 
            StartCoroutine(DelaySceneLoad(5.0f, tutorialToLoad));
            return;
        }
        
        // 게임 시작
        if (CheckSystemOperationInput(userText, gameTargets, gameActions))
        {
            base.ProcessUserInput( "Starting the game.");
            // TTS 응답 속도에 대응하기 위해 조금 기다렸다 씬 로딩 
            StartCoroutine(DelaySceneLoad(5.0f, gameToLoad));
            return;
        }
            
        // 게임 종료
        if (CheckSystemOperationInput(userText, exitTargets, exitActions))
        {
            base.ProcessUserInput("Exit game.");
            StartCoroutine(ExitGame());
            return;
        }
        
        // 아무 처리도 못했을 경우
        Debug.Log("아무 처리도 못함");
        base.ProcessUserInput(playbackStr);
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