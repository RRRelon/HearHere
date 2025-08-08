using System.Collections;
using System.Collections.Generic;
using HH;
using UnityEngine;

public class TutorialClient : Client
{
    [Header("Clue/Answer Setting")]
    [SerializeField] private List<ClueSound> soundSettings;
    [SerializeField] private string[] answerTargets;
    [SerializeField] private string answer;
    
    [SerializeField] private MapInfo mapInfo;
    [SerializeField] private GameSceneSO menuToLoad;
    
    [Header("Broadcasting on")]
    [SerializeField] private LoadEventChannelSO loadMenu;
    [SerializeField] private BoolEventChannelSO onGameClear;
    
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
        
        // 게임 종료
        if (CheckSystemOperationInput(userText, exitTargets, exitActions))
        {
            base.ProcessUserInput("Exit game.");
            return;
        }
        
        // 게임 내 적용
        MapResult result; // 맵에서 가져온 결과
        
        // 정답 소리 따로 처리
        if (userText.Contains(answer) && ContainsAny(userText, answerTargets))
        {
            result = mapInfo.GetSuccess('1');
            // 정답 뒤에 Try 횟수 붙이기
            string ttsText = "Congratulations! You did it!" + result.Message + "Going to the main menu"; 
            base.ProcessUserInput(ttsText);
            ExitTutorial();
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
                
                // 정답일 경우
                if (result.Message == "-1")
                {
                    base.ProcessUserInput("Congratulations! You did it!" + result.Message + "Going to the main menu");
                    ExitTutorial();
                    return;
                }
                    
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

        // 게임 내용에 대한 GPT 응답
        GPTResponse response = await manager.GetGPTResponseFromText(userText, prompt.Prompt);
        if (response == null)
        {
            base.ProcessUserInput("Sorry. I can't understand. Try again.");
            return;    
        }
        
        Debug.Log($"GPT 응답:\n{response}");
        
        switch (response.response_type)
        {
            case "dialogue": // 일반 상호작용(아무 소리, 오답)
                mapInfo.GetDialogue();
                base.ProcessUserInput(response.tts_text);
                return;
            case "clue":
                result = mapInfo.GetClue(response.argument[0]);
                response.tts_text += result.Message;
                base.ProcessUserInput(response.tts_text);
                return;
            case "success":  // 정답
                result = mapInfo.GetSuccess(response.argument[0]);
                // 유효한 정답일 경우
                if (result.IsValid)
                {
                    string ttsText = "Congratulations! You did it!" + result.Message + "Going to the main menu";
                    base.ProcessUserInput(ttsText);
                    ExitTutorial();
                    return;
                }
                // 유효하지 않은 정답일 경우
                response.tts_text += result.Message;
                base.ProcessUserInput(response.tts_text);
                return;
            default: // 오답
                Debug.LogError($"Invalid Response type: {response.response_type}");
                base.ProcessUserInput(response.tts_text);
                break;
        }
        
        // 아무 처리도 못했을 경우
        base.ProcessUserInput(playbackStr);
    }

    private void ExitTutorial()
    {
        StartCoroutine(PlayTTS("Tutorial complete! Now returning to the main menu."));
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
        loadMenu.OnLoadingRequested(sceneToLoad, true, true);
    }
}
