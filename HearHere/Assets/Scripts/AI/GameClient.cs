using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HH;

/// <summary>
/// Unity에서 FastAPI 백엔드 서버와 통신하는 클라이언트 스크립트입니다.
/// TTS, STT, Chat, Voice Chat 기능을 담당합니다.
/// </summary>
public class GameClient : Client
{
    [Header("Clue/Answer Setting")]
    [SerializeField] private List<ClueSound> soundSettings;
    [SerializeField] private string answer;
    
    [Header("Map Info")]
    [SerializeField] private MapInfo mapInfo;
    // Game Management
    [Header("Scene Management")]
    [SerializeField] private GameSceneSO currentlyLoadedScene;
    [SerializeField] private GameSceneSO sceneToLoadOnClear;
    [SerializeField] private GameSceneSO menuToLoad;
    
    [Header("Player Data")]
    [SerializeField] private PlayerDataSO playerData;
    
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
        
        // 메뉴 설명
        if (CheckSystemOperationInput(userText, menuInfoTargets, menuInfoActions))
        {
            base.ProcessUserInput("Available commands are Go to Main Menu, and Exit Game.");
            return;
        }
        
        // 메인 메뉴 관련
        if (CheckSystemOperationInput(userText, menuTargets, menuActions))
        {
            if (currentlyLoadedScene.SceneType != GameSceneType.Menu)
            {
                base.ProcessUserInput("Moving to the main menu.");
                // TTS 응답 속도에 대응하기 위해 조금 기다렸다 씬 로딩 
                StartCoroutine(DelaySceneLoad(5.0f, menuToLoad));
            }
            else
                base.ProcessUserInput("You are currently in the main menu.");

            return;
        }
        
        // 게임 종료
        if (CheckSystemOperationInput(userText, exitTargets, exitActions))
        {
            base.ProcessUserInput("Exit game.");
            StartCoroutine(ExitGame(5.0f));
            return;
        }
        
        // 게임 내 적용
        MapResult result; // 맵에서 가져온 결과
        
        // 정답 소리 따로 처리
        if (userText.Contains(answer))
        {
            result = mapInfo.GetSuccess('1');
            // 정답 뒤에 Try 횟수 붙이기
            string ttsText = "Congratulations! You did it!" + result.Message + FormatPlayTime(totalPlayTime); 
            base.ProcessUserInput(ttsText);
            GameClear();
            return;
        }
        
        // 단서 소리는 따로 처리
        foreach (ClueSound clue in soundSettings)
        {
            bool hasX = ContainsAny(userText, clue.X);
            bool hasY = ContainsAny(userText, clue.Y);
            bool hasName = ContainsAny(userText, clue.Name);

            if (hasX && hasY && hasName)
            {
                result = mapInfo.GetClue(clue.Argument);
                // Map에서 전달받은 메시지를 추가
                if (result.IsValid)
                {
                    onGameClear.OnEventRaised(true);
                }
                else
                {
                    onGameClear.OnEventRaised(false);
                }
                string ttsText = $"You correctly identified the {clue.Name} sound." + result.Message;
                base.ProcessUserInput(ttsText);
                return;
            }
        }

        // 위에서 처리 못한 게임 내용에 대한 GPT 응답
        GPTResponse response = await manager.GetGPTResponseFromText(userText, prompt.Prompt);
        if (response == null)
        {
            base.ProcessUserInput("Sorry. I can't understand. Try again.");
            return;
        }
        
        Debug.Log($"GPT 응답:\n{response}");
        
        // GPT 응답에 따른 액션 수행
        switch (response.response_type)
        {
            case "dialogue": // 일반 상호작용(아무 소리, 오답)
                mapInfo.GetDialogue();
                base.ProcessUserInput(response.tts_text);
                return;
            case "clue":     // 단서 소리
                // 얻은 Response에 대한 Map의 응답
                if (response.argument.Length <= 0)
                {
                    base.ProcessUserInput(response.tts_text);
                    onGameClear.OnEventRaised(false);
                    return;   
                }
                result = mapInfo.GetClue(response.argument[0]);
                // Map에서 전달받은 메시지를 추가
                if (result.IsValid)
                {
                    onGameClear.OnEventRaised(true);
                }
                else
                {
                    onGameClear.OnEventRaised(false);
                }
                response.tts_text += result.Message;
                base.ProcessUserInput(response.tts_text);
                return;
            case "success":  // 정답
                if (response.argument.Length <= 0)
                {
                    base.ProcessUserInput(response.tts_text);
                    return;   
                }
                result = mapInfo.GetSuccess(response.argument[0]);
                // 유효한 정답일 경우
                if (result.IsValid)
                {
                    // 정답 뒤에 Try 횟수 붙이기
                    response.tts_text += result.Message;
                    // 정답 뒤에 걸린 시간 넣기
                    response.tts_text += FormatPlayTime(totalPlayTime);
                    StartCoroutine(PlayTTS(response.tts_text));
                    base.ProcessUserInput(response.tts_text);
                    GameClear();
                    return;
                }
                else
                {
                    onGameClear.OnEventRaised(false);
                }
                // 유효하지 않은 정답일 경우
                response.tts_text += result.Message;
                base.ProcessUserInput(response.tts_text);
                return;
            default:
                Debug.LogError($"Invalid Response type: {response.response_type}");
                base.ProcessUserInput(response.tts_text);
                break;
        }
        
        // 아무 처리도 못했을 경우
        base.ProcessUserInput("Please say that again with the correct answer.");
    }
    
    /// <summary>
    /// 게임 클리어 시 메인 메뉴로 돌아가기
    /// 데이터 저장
    /// </summary>
    private void GameClear()
    {
        playerData.AddGameResult(totalPlayTime, mapInfo.GetTryCount());
        StartCoroutine(OnGameClear(8.0f, sceneToLoadOnClear));
    }

    private IEnumerator OnGameClear(float waitTime, GameSceneSO sceneToLoad)
    {
        DisableInput();
        
        // 화면 점등
        onGameClear.OnEventRaised(true);
        yield return new WaitForSeconds(waitTime);
        
        EnableInput();
        
        // 메인 메뉴로 이동
        StartCoroutine(DelaySceneLoad(3.0f, sceneToLoadOnClear));
    }
    
    /// <summary>
    /// 게임 종료 메서드
    /// </summary>
    private IEnumerator ExitGame(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
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
        loadMenu.OnLoadingRequested(sceneToLoad, true, true);
    }

    /// <summary>
    /// keywords 중 text를 찾는 함수이다. 있으면 True, 없으면 False 반환
    /// </summary>
    private bool ContainsAny(string text, string[] keywords)
    {
        // keywords가 비어있다면 조건이 없는 것, True를 반환한다.
        if (keywords == null || keywords.Length == 0) return true;
        string lowerText = text.ToLower();
        return keywords.Any(keyword => lowerText.Contains(keyword.ToLower()));
    }
}

[Serializable]
public struct ClueSound
{
    // X, Y, Name의 요소 중 하나씩은 말해야 옳게 말한 것으로 인정한다.
    public string[] X;
    public string[] Y;
    public string[] Name;
    public char Argument;
}