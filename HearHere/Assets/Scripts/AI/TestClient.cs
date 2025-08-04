using System.Collections;
using UnityEngine;
using HH;

/// <summary>
/// Unity에서 FastAPI 백엔드 서버와 통신하는 클라이언트 스크립트입니다.
/// TTS, STT, Chat, Voice Chat 기능을 담당합니다.
/// </summary>
public class TestClient : MonoBehaviour
{
    [Header("Debugging")]
    public string speechTest;
    // input
    [SerializeField] private InputReader inputReader;
    // AI
    [Header("AI")]
    [SerializeField] private AIConversationManagerSO manager;
    [SerializeField] private PromptSO prompt;
    [SerializeField] private int promptNum = 0;
    

    // input
    private string microphoneDevice;
    private AudioClip recordedClip;
    // STT    
    private int duration = 5;
    private AudioClip clip;
    private bool isRecording;
    private float time;
     
    
    private void Awake()
    {
        // 사용 가능한 마이크 장치 확인
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            // Debug.Log($"사용할 마이크: {microphoneDevice}");
        }
        else
        {
            Debug.LogError("오류: 사용 가능한 마이크 장치를 찾을 수 없습니다!");
        }
    }
    
    private void OnEnable()
    {
        inputReader.SpeechEvent += STTTest;
    }

    private void OnDisable()
    {
        inputReader.SpeechEvent -= STTTest;
    }

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
    private async void ProcessUserInput(string userText)
    {
        #region 메뉴 설명
        // string[] menuInfoTargets = { "메뉴" };
        // string[] menuInfoActions = { "알려줘", "뭐 있어", "뭐야", "설명" };
        string[] menuInfoTargets = { "menu" };
        string[] menuInfoActions = { "tell me", "what is", "explain", "describe" };

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
                    Debug.Log($"user input: {userText}, response: Available commands are Go to Main Menu, and Exit Game.");
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
                    Debug.Log($"user input: {userText}, response: 메인 메뉴로 이동합니다.");
                    return; // 처리 완료, GPT에 보내지 않음
                }
            }
        }
        #endregion 

        #region 게임 종료
            
        // 게임 종료 관련
        // string[] exitTargets = { "게임", "프로그램" };
        // string[] exitActions = { "나가기", "종료", "꺼줘", "끌래" };
        string[] exitTargets = { "game", "application", "program" };
        string[] exitActions = { "exit", "quit", "turn off", "close" };

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
                    Debug.Log($"user input: {userText}, response: 게임을 종료합니다.");
                    return;
                }
            }
        }
        #endregion

        #region 게임 내용에 대한 GPT 응답
        GPTResponse response = await manager.GetGPTResponseFromText(userText, prompt.Prompts[promptNum].Content);
        // GPT 응답에 따른 액션 수행
        Debug.Log($"user input: {userText}, gpt response: {response}");
        #endregion
    }
}

