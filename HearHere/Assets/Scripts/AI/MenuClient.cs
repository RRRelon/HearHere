using System.Collections;
using UnityEngine;
using HH;
using UnityEngine.Serialization;

/// <summary>
/// Unity에서 FastAPI 백엔드 서버와 통신하는 클라이언트 스크립트입니다.
/// TTS, STT, Chat, Voice Chat 기능을 담당합니다.
/// </summary>
public class MenuClient : Client
{
    [Header("Debugging")]
    public string speechTest;
    // Game Management
    [Header("Scene Management")]
    [SerializeField] private GameSceneSO currentlyLoadedScene;
    [SerializeField] private GameSceneSO gameToLoad;
    
    [Header("Broadcasting on")]
    [SerializeField] private LoadEventChannelSO loadLocation;
    [SerializeField] private StringEventChannelSO onTextReadyForTTS;

    /// <summary>
    /// // TODO: 테스트용 코드. 실제로는 위의 코드를 주석 해제해 사용
    /// </summary>
    private void STTTest()
    {
        ProcessUserInput(speechTest);
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
        
        #region 게임 시작
        // 게임 시작 관련
        string[] startKeywords = { "start", "begin", "play" };

        string startGameStr = "Starting the game.";
        string alreadyGameStr = "The game is already in progress.";

        // --- 게임 시작 명령어 확인 ---
        foreach (var keyword in startKeywords)
        {
            if (userText.Contains(keyword))
            {
                // 게임 씬으로 이동하는 명령어 처리
                if (currentlyLoadedScene.SceneType != GameSceneType.Location)
                {
                    onTextReadyForTTS.OnEventRaised(startGameStr);
                    base.ProcessUserInput(startGameStr);
                    StartCoroutine(DelaySceneLoad(3.0f, gameToLoad)); // 씬 로딩
                }
                else
                {
                    onTextReadyForTTS.OnEventRaised(alreadyGameStr);
                    base.ProcessUserInput(alreadyGameStr);
                }
                return; // 처리 완료, GPT에 보내지 않음
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