using System;
using System.Collections;
using UnityEngine;
using HH;
using OpenAI;
using Samples.Whisper;
using UnityEngine.Serialization;

/// <summary>
/// Unity에서 FastAPI 백엔드 서버와 통신하는 클라이언트 스크립트입니다.
/// TTS, STT, Chat, Voice Chat 기능을 담당합니다.
/// </summary>
public class Client : MonoBehaviour
{
    [Header("Debugging")]
    public string speechTest;
    // Game Management
    [Header("Scene Management")]
    [SerializeField] private GameSceneSO currentlyLoadedScene;
    [SerializeField] private GameSceneSO menuToLoad;
    [SerializeField] private GameSceneSO gameToLoad;
    // input
    [SerializeField] private InputReader inputReader;
    // AI
    [Header("AI")]
    [SerializeField] private AIConversationManagerSO manager;
    [SerializeField] private PromptSO prompt;
    
    [Header("Broadcasting on")]
    [SerializeField] private LoadEventChannelSO loadScene;
    [SerializeField] private StringEventChannelSO onTextReadyForTTS;

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
            Debug.Log($"사용할 마이크: {microphoneDevice}");
        }
        else
        {
            Debug.LogError("오류: 사용 가능한 마이크 장치를 찾을 수 없습니다!");
        }
    }
    
    private void OnEnable()
    {
        // inputReader.SpeechEvent += StartRecording;
        // inputReader.SpeechCancelEvent += EndRecording;
            
        // TODO: 테스트용 코드. 실제로는 위의 코드를 주석 해제해 사용
        inputReader.SpeechEvent += STTTest;
    }

    private void OnDisable()
    {
        // inputReader.SpeechEvent -= StartRecording;
        // inputReader.SpeechCancelEvent -= EndRecording;
            
        // TODO: 테스트용 코드. 실제로는 위의 코드를 주석 해제해 사용
        inputReader.SpeechEvent -= STTTest;
    }
    
    private void StartRecording()
    {
        // 마이크 입력 시작
        Debug.Log("Start Recording");
        clip = Microphone.Start(Microphone.devices[0], false, duration, 44100);
    }

    private async void EndRecording()
    {
        Debug.Log("End Recording");
        
        // 분석할 동안 입력받기 중단
        inputReader.DisableAllInput();
        
        // STT
        Debug.Log("응답 대기중...");
        string userText = await manager.GetTextFromAudio(clip);
        
        // 사용자 입력에 대한 처리
        ProcessUserInput(userText);
        
        // 다시 스페이스바 입력 받기 시작
        inputReader.EnableGameplayInput();
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
        #region 메뉴 설명 관련
        string[] menuInfoTargets = { "메뉴" };
        string[] menuInfoActions = { "알려줘", "뭐 있어", "뭐야", "설명" };

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
                    onTextReadyForTTS.OnEventRaised("사용 가능한 명령어는 게임 시작, 게임 종료입니다.");
                    return; // 처리 완료, GPT에 보내지 않음
                }
            }
        }
        #endregion
        
        #region 메인 메뉴 관련
        string[] menuTargets = { "메인", "처음" };
        string[] menuActions = { "메뉴", "화면", "이동", "돌아가", "가줘" };

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
                        onTextReadyForTTS.OnEventRaised("메인 메뉴로 이동합니다.");
                        
                        // TTS 응답 속도에 대응하기 위해 조금 기다렸다 씬 로딩 
                        StartCoroutine(DelaySceneLoad(3.0f, menuToLoad));
                    }
                    else
                    {
                        onTextReadyForTTS.OnEventRaised("현재 메인 메뉴입니다.");
                    }
                    return; // 처리 완료, GPT에 보내지 않음
                }
            }
        }
        #endregion 

        #region 게임 시작
        // 게임 시작 관련
        string[] startKeywords = { "시작", "시작해", "플레이" };
            
        // 게임 종료 관련
        string[] exitTargets = { "게임", "프로그램" };
        string[] exitActions = { "나가기", "종료", "꺼줘", "끌래" };

        // --- 게임 시작 명령어 확인 ---
        foreach (var keyword in startKeywords)
        {
            if (userText.Contains(keyword))
            {
                // 게임 씬으로 이동하는 명령어 처리
                if (currentlyLoadedScene.SceneType != GameSceneType.Location)
                {
                    onTextReadyForTTS.OnEventRaised("게임을 시작합니다.");
                        
                    // TTS 응답 속도에 대응하기 위해 조금 기다렸다 씬 로딩 
                    StartCoroutine(DelaySceneLoad(3.0f, gameToLoad));
                }
                else
                {
                    onTextReadyForTTS.OnEventRaised("현재 게임 중 입니다.");
                }
                return; // 처리 완료, GPT에 보내지 않음
            }
        }

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
                    Debug.Log("게임을 종료합니다.");
                    // 실제 게임 종료 코드
        #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
        #else
                    Application.Quit();
        #endif
                    return;
                }
            }
        }
        #endregion

        #region 게임 내용에 대한 GPT 응답
        GPTResponse response = await manager.GetGPTResponseFromText(userText, prompt.Prompt);
        // GPT 응답에 따른 액션 수행
        switch (response.response_type)
        {
            case "clue": // 단서 소리
                OnClueAction(response.command, response.command_arg);
                break;
            case "dialogue": // 일반 상호작용
                OnDialogueAction(response.tts_text);
                break;
            default:
                onTextReadyForTTS.OnEventRaised("게임과 관련 없는 내용입니다.");
                break;
        }
        #endregion
    }
    
    /// <summary>
    /// 일반 상호작용
    /// </summary>
    private void OnDialogueAction(string text)
    {
        Debug.Log("게임 내 상호작용");
        onTextReadyForTTS.OnEventRaised(text);
    }

    /// <summary>
    /// 단서 소리 발견 시
    /// </summary>
    private void OnClueAction(string command, string arg)
    {
        // TODO: 일단 바로 게임 클리어 되도록
        // 근데 그냥 이렇게 구현해도 될 것 같기도 하고? 여러 개 찾아서 해야 할까
        GameClear();
    }

    /// <summary>
    /// 게임 클리어 시 메인 메뉴로 돌아가기
    /// </summary>
    private void GameClear()
    {
        StartCoroutine(DelaySceneLoad(3.0f, menuToLoad));
    }

    private IEnumerator DelaySceneLoad(float waitTime, GameSceneSO sceneToLoad)
    {
        yield return new WaitForSeconds(waitTime);
        loadScene.OnLoadingRequested(gameToLoad);
    }

    private IEnumerator OnGameClear()
    {
        inputReader.DisableAllInput();
        
        // 화면 점등
        
        yield return new WaitForSeconds(3.0f);
        
        inputReader.EnableGameplayInput();
    }
}

