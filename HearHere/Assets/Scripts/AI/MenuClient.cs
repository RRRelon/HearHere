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
    private string playbackStr = "Welcome to Hear, Here!, the escape room game where you find the target sound.\"\n\n\"To start the tutorial, please say, 'Start tutorial'. To begin the game, please say, 'Start game'";
    
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
    /// 메인 메뉴 시작 시마다 현재 데이터를 읽어준다.
    /// 처음 기록 대비 최근 기록을 TTS로 안내한다.
    /// </summary>
    protected override void OnEnable()
    {
        foreach (var data in playerData.Datas)
        {
            Debug.Log($"current player datas : {data.Time}, {data.TryCount}");
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
        
        #region 메뉴 설명
        string[] menuInfoTargets = { "menu" };
        string[] menuInfoActions = { "tell me", "what is", "explain", "describe" };

        string explainMenuStr = "Available commands are Start Game and Exit Game.";
        
        // --- 메뉴 설명 명령어 확인 ---
        bool isMenuInfoTargetMatch = false;
        foreach (var target in menuInfoTargets)
        {
            if (userText.Contains(target))
            {
                isMenuInfoTargetMatch = true;
                break;
            }
        }

        if (isMenuInfoTargetMatch)
        {
            foreach (var action in menuInfoActions)
            {
                if (userText.Contains(action))
                {
                    
                    // 메뉴 설명 TTS 실행
                    onTextReadyForTTS.OnEventRaised(explainMenuStr);
                    base.ProcessUserInput(explainMenuStr);
                    return; // 처리 완료, GPT에 보내지 않음
                }
            }
        }
        #endregion
        
        #region 튜토리얼 시작
        // 튜토리얼 시작 관련 키워드
        string[] tutorialTargets = { "tutorial" };
        string[] tutorialActions = { "start", "begin" };

        string startTutorialStr = "Starting the tutorial.";
        string alreadyTutorialStr = "The tutorial is already in progress.";

        // --- 튜토리얼 시작 명령어 확인 ---
        bool isTutorialTargetMatch = false;
        foreach (var target in tutorialTargets)
        {
            if (userText.ToLower().Contains(target))
            {
                isTutorialTargetMatch = true;
                break;
            }
        }
        if (isTutorialTargetMatch)
        {
            foreach (var action in tutorialActions)
            {
                if (userText.ToLower().Contains(action))
                {
                    // 튜토리얼 시작 로직 실행
                    if (currentlyLoadedScene.SceneType != GameSceneType.Location)
                    {
                        onTextReadyForTTS.OnEventRaised(startTutorialStr);
                        base.ProcessUserInput(startTutorialStr);
                        StartCoroutine(DelaySceneLoad(3.0f, tutorialToLoad));
                    }
                    else
                    {
                        onTextReadyForTTS.OnEventRaised(alreadyTutorialStr);
                        base.ProcessUserInput(alreadyTutorialStr);
                    }
                    return; // 처리 완료, GPT에 보내지 않음
                }
            }
        }
        #endregion
        
        #region 게임 시작
        // 게임 시작 관련 키워드
        string[] gameTargets = { "game" };
        string[] gameActions = { "start", "begin", "play" };

        string startGameStr = "Starting the game.";
        string alreadyGameStr = "The game is already in progress.";

        // --- 게임 시작 명령어 확인 ---
        bool isGameTargetMatch = false;
        foreach (var target in gameTargets)
        {
            if (userText.ToLower().Contains(target))
            {
                isGameTargetMatch = true;
                break;
            }
        }
        if (isGameTargetMatch)
        {
            foreach (var action in gameActions)
            {
                if (userText.ToLower().Contains(action))
                {
                    // 게임 시작 로직 실행
                    if (currentlyLoadedScene.SceneType != GameSceneType.Location)
                    {
                        onTextReadyForTTS.OnEventRaised(startGameStr);
                        base.ProcessUserInput(startGameStr);
                        StartCoroutine(DelaySceneLoad(3.0f, gameToLoad));
                    }
                    else
                    {
                        onTextReadyForTTS.OnEventRaised(alreadyGameStr);
                        base.ProcessUserInput(alreadyGameStr);
                    }
                    return; // 처리 완료, GPT에 보내지 않음
                }
            }
        }
        #endregion

        #region 게임 종료
            
        // 게임 종료 관련
        string[] exitTargets = { "game", "application", "program" };
        string[] exitActions = { "exit", "quit", "turn off", "close" };

        string exitGameStr = "게임을 종료합니다.";
        
        // --- 게임 종료 명령어 확인 ---
        bool isExitTargetMatch = false;
        foreach (var target in exitTargets)
        {
            if (userText.Contains(target))
            {
                isExitTargetMatch = true;
                break;
            }
        }

        if (isExitTargetMatch)
        {
            foreach (var action in exitActions)
            {
                if (userText.Contains(action))
                {
                    onTextReadyForTTS.OnEventRaised(exitGameStr);
                    base.ProcessUserInput(exitGameStr);
                    
                    StartCoroutine(ExitGame()); // 실제 게임 종료 코드
                    return;
                }
            }
        }
        #endregion
        
        base.ProcessUserInput("");
    }
    
    /// <summary>
    /// 게임 종료 메서드
    /// </summary>
    private IEnumerator ExitGame()
    {
        yield return new WaitForSeconds(3.0f);
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
        loadLocation.OnLoadingRequested(sceneToLoad);
    }
}