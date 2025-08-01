using System.Collections;
using UnityEngine;
using HH;
using UnityEngine.Serialization;

/// <summary>
/// Unity에서 FastAPI 백엔드 서버와 통신하는 클라이언트 스크립트입니다.
/// TTS, STT, Chat, Voice Chat 기능을 담당합니다.
/// </summary>
public class MenuClient : MonoBehaviour
{
    [Header("Debugging")]
    public string speechTest;
    // Game Management
    [Header("Scene Management")]
    [SerializeField] private GameSceneSO currentlyLoadedScene;
    [SerializeField] private GameSceneSO gameToLoad;
    // input
    [SerializeField] private InputReader inputReader;
    // AI
    [Header("AI")]
    [SerializeField] private AIConversationManagerSO manager;
    
    [Header("Broadcasting on")]
    [SerializeField] private LoadEventChannelSO loadLocation;
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
            // Debug.Log($"사용할 마이크: {microphoneDevice}");
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

        StartCoroutine(StartAnnounce());
    }

    private IEnumerator StartAnnounce()
    {
        yield return new WaitForSeconds(2.0f);
        onTextReadyForTTS.OnEventRaised("안녕하세요. 저는 게임 비서 입니다. 메뉴 설명이라고 말씀하시면 메뉴 목록 확인 가능합니다.");
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
    private void ProcessUserInput(string userText)
    {
        #region 메뉴 설명
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
        #endregion

        #region 게임 종료
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
    }

    private IEnumerator DelaySceneLoad(float waitTime, GameSceneSO sceneToLoad)
    {
        yield return new WaitForSeconds(waitTime);
        loadLocation.OnLoadingRequested(sceneToLoad);
    }
}

