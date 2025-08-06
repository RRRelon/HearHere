using System.Collections;
using UnityEngine;
using HH;

/// <summary>
/// Unity에서 FastAPI 백엔드 서버와 통신하는 클라이언트 스크립트입니다.
/// TTS, STT, Chat, Voice Chat 기능을 담당합니다.
/// </summary>
public class GameClient : Client
{
    [Header("Map Info")]
    [SerializeField] private MapInfo mapInfo;
    // Game Management
    [Header("Scene Management")]
    [SerializeField] private GameSceneSO currentlyLoadedScene;
    [SerializeField] private GameSceneSO menuToLoad;
    
    [Header("Broadcasting on")]
    [SerializeField] private LoadEventChannelSO loadMenu;
    [SerializeField] private BoolEventChannelSO onGameClear;

    /// <summary>
    /// 사용자 입력에 대한 처리를 우선적으로 한 뒤 필요 시 GPT 응답에 대한 처리 진행
    /// </summary>
    protected override async void ProcessUserInput(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText))
        {
            Debug.Log("입력 값이 Null 입니다.");
            base.ProcessUserInput("");
            return;
        }
        
        userText = userText.ToLower().Replace(".", "").Replace("?", "");
        
        #region 메뉴 설명
        // string[] menuInfoTargets = { "메뉴" };
        // string[] menuInfoActions = { "알려줘", "뭐 있어", "뭐야", "설명" };
        string[] menuInfoTargets = { "menu" };
        string[] menuInfoActions = { "tell me", "what is", "explain", "describe" };

        string explainMenuStr = "Available commands are Go to Main Menu, and Exit Game.";
        
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
                    onTextReadyForTTS.OnEventRaised(explainMenuStr); // 메뉴 설명 TTS 실행
                    base.ProcessUserInput(explainMenuStr);           // 다시 마이크 모니터링 시작
                    
                    return; // 처리 완료, GPT에 보내지 않음
                }
            }
        }
        #endregion
        
        #region 메인 메뉴 관련
        // string[] menuTargets = { "메인", "처음" };
        // string[] menuActions = { "메뉴", "화면", "이동", "돌아가", "가줘" };
        string[] menuTargets = { "main", "first" };
        string[] menuActions = { "menu", "screen", "move", "return", "go" };

        string moveMenuStr = "Moving to the main menu.";
        string alreadyMenuStr = "You are currently in the main menu.";
        
        // --- 메인 메뉴 이동 명령어 확인 ---
        bool isMenuTargetMatch = false;
        foreach (var target in menuTargets)
        {
            if (userText.Contains(target))
            {
                isMenuTargetMatch = true;
                break;
            }
        }
        if (isMenuTargetMatch)
        {
            foreach (var action in menuActions)
            {
                if (userText.Contains(action))
                {
                    // 메인 메뉴로 이동하는 명령어 처리
                    if (currentlyLoadedScene.SceneType != GameSceneType.Menu)
                    {
                        onTextReadyForTTS.OnEventRaised(moveMenuStr); // 메인 메뉴 이동 TTS 실행
                        base.ProcessUserInput(moveMenuStr);           // 다시 마이크 모니터링 시작
                        
                        // TTS 응답 속도에 대응하기 위해 조금 기다렸다 씬 로딩 
                        StartCoroutine(DelaySceneLoad(3.0f, menuToLoad));
                    }
                    else
                    {
                        onTextReadyForTTS.OnEventRaised(alreadyMenuStr); // 이미 메인 메뉴라는 TTS 실행
                        base.ProcessUserInput(alreadyMenuStr);           // 다시 마이크 모니터링 시작
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

        #region 게임 내용에 대한 GPT 응답
        GPTResponse response = await manager.GetGPTResponseFromText(userText, prompt.Prompts[promptNum].Content);
        if (response == null)
        {
            // 다시 마이크 모니터링 시작
            string retryStr = "Sorry. I can't understand. Try again.";
            onTextReadyForTTS.OnEventRaised(retryStr);
            base.ProcessUserInput(retryStr);
            return;
        }
        
        // GPT 응답에 따른 액션 수행
        switch (response.response_type)
        {
            case "dialogue": // 일반 상호작용(아무 소리, 오답)
                mapInfo.GetDialogue();
                break;
            case "clue":     // 단서 소리 발견
                response.tts_text += mapInfo.GetClue(response.argument[0]);
                break;
            case "success":  // 정답
                // 정답 뒤에 Try 횟수 붙이기
                response.tts_text += mapInfo.GetSuccess();
                onTextReadyForTTS.OnEventRaised(response.tts_text);
                GameClear();
                return;
            default:
                Debug.LogError($"잘못된 Response type: {response.response_type}");
                break;
        }
        
        Debug.Log($"GPT 응답 : {response.tts_text}");
        
        // GPT 응답 TTS로 전환
		onTextReadyForTTS.OnEventRaised(response.tts_text);
        
        // 다시 마이크 모니터링 시작
        base.ProcessUserInput(response.tts_text);
        #endregion
    }
    
    /// <summary>
    /// 게임 클리어 시 메인 메뉴로 돌아가기
    /// </summary>
    private void GameClear()
    {
        StartCoroutine(OnGameClear(3.0f, menuToLoad));
    }

    private IEnumerator OnGameClear(float waitTime, GameSceneSO sceneToLoad)
    {
        DisableInput();
        
        // 화면 점등
        onGameClear.OnEventRaised(true);
        
        yield return new WaitForSeconds(3.0f);
        
        EnableInput();
        
        // 메인 메뉴로 이동
        onTextReadyForTTS.OnEventRaised("Moving to the main menu.");
        StartCoroutine(DelaySceneLoad(3.0f, menuToLoad));
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
        loadMenu.OnLoadingRequested(sceneToLoad);
    }
}

