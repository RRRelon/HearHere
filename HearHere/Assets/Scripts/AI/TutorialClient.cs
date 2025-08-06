using System.Collections;
using HH;
using UnityEngine;

public class TutorialClient : Client
{
    [SerializeField] private MapTutorial tutorial;
    [SerializeField] private GameSceneSO menuToLoad;
    
    [Header("Broadcasting on")]
    [SerializeField] private LoadEventChannelSO loadMenu;

    [SerializeField] private float playbackInterval = 20.0f;
    [SerializeField] private float playbackTimer;
    private string playbackStr = "Please describe exactly where the sound is coming from and what it is.";

    protected override void Start()
    {
        base.Start();
        playbackTimer = playbackInterval;
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
    
    protected override async void ProcessUserInput(string userText)
    {
        if (string.IsNullOrWhiteSpace(userText))
        {
            Debug.Log("입력 값이 Null 입니다.");
            base.ProcessUserInput("");
            return;
        }
        
        userText = userText.ToLower().Replace(".", "").Replace("?", "");
        
        #region 메인 메뉴 관련
        string[] menuTargets = { "main", "first" };
        string[] menuActions = { "menu", "screen", "move", "return", "go" };

        string moveMenuStr = "Moving to the main menu.";
        
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
                    onTextReadyForTTS.OnEventRaised(moveMenuStr); // 메인 메뉴 이동 TTS 실행
                    base.ProcessUserInput(moveMenuStr);           // 다시 마이크 모니터링 시작
                    
                    // TTS 응답 속도에 대응하기 위해 조금 기다렸다 씬 로딩 
                    StartCoroutine(DelaySceneLoad(3.0f, menuToLoad));
                    
                    return; // 처리 완료, GPT에 보내지 않음
                }
            }
        }
        #endregion 

        #region 게임 종료
            
        // 게임 종료 관련
        string[] exitTargets = { "game", "application", "program" };
        string[] exitActions = { "exit", "quit", "turn off", "close" };

        string exitGameStr = "Exit Game.";
        
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
            case "success":  // 정답
                // 튜토리얼 끝남
                if (tutorial.TryAdvanceToNextSound())
                    ExitTutorial();
                break;
            default:
                Debug.LogError("정확한 방향과 소리를 다시 말해주세요.");
                onTextReadyForTTS.OnEventRaised(playbackStr);
                playbackTimer = 0;
                break;
        }
        
        Debug.Log($"GPT 응답 : {response.tts_text}");
        
        // GPT 응답 TTS로 전환
        onTextReadyForTTS.OnEventRaised(response.tts_text);
        
        // 다시 마이크 모니터링 시작
        base.ProcessUserInput(response.tts_text);
        #endregion
    }

    private void ExitTutorial()
    {
        onTextReadyForTTS.OnEventRaised("Tutorial complete! Now returning to the main menu.");
        StartCoroutine(DelaySceneLoad(5.0f, menuToLoad));
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
