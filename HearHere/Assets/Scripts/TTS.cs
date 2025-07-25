// using -> python의 import 느낌
using System.Collections;   // ArrayList, Hashtable, Queue, Stack 등의 컬렉션을 제공함
                                // ArrayList: 필요에 따라 크기가 동적으로 증가하는 개체 배열
                                // Hashtable: 키의 해시 코드에 따라 구성된 키/값 쌍의 컬렉션
using System.IO;            // 파일 및 디렉토리 작업을 위한 기능
using System.Text;          // 문자열 인코딩 같은 기능
using UnityEngine;          // Unity 기본 기능
using UnityEngine.Networking;// InputField, Button 같은 UI 컴포넌트
using UnityEngine.UI;       // UI 관련 기능

// class여서 여러 개의 관련 함수, 변수들을 묶은 단위
// MonoBehaviour 상속: unity 에서는 모든 스크립트가 이걸 상속해서 start(), update() 등 기본 기능 사용가능

public class TTS : MonoBehaviour    // TTS: unity에서 사용하는 TTS script
                                    // MonoBehaviour: unity에서 script를 게임 오브젝트에 붙이기 위한 기본 클래스
{
    // unity editor에서 이 변수들을 inspector 창에 드래그해서 연결해줘야 함
    // public -> unity inspector에서 연결 가능하게 하기 위함
    // private -> 내부에서만 쓰는 변수. 따라서 key는 private함
    // Unity에는 Attribute를 설정할 수 있음
    // 대표적으로 SerializeField: Inspector에서 설정 가능, Header: Inspector에 Header 생성 가능
    [Header("TTS Components")]
    [SerializeField] private InputField inputField;
    [SerializeField] private Button playButton;
    [SerializeField] private AudioSource audioSource;

    private string apiKey;

    // start()는 게임이 실행되면 가장 먼저 호출되는 함수
    // 버튼이 클릭되면 inputfield의 텍스트를 읽어서 requestTTS() 실행
    private void Start()
    {
        // secret.json에서 apiKey 읽기
        TextAsset jsonFile = Resources.Load<TextAsset>("secret"); // 경로 쓸 때 확장자는 쓰지 않고, resources 폴더 기준 상대 경로로 작성해야 함
                                                                        // TextAsset: unity에서 텍스트 파일을 다룰 수 있게 만든 자료형
        SecretData secret = JsonUtility.FromJson<SecretData>(jsonFile.text); // JSON을 C# 객체로 바꾸는 코드
        apiKey = secret.googleTTSapiKey;
        
        playButton.onClick.AddListener(() =>
        {
            StartCoroutine(RequestTTS(inputField.text));
        });
    }
    
    // google TTS API에 POST 요청을 보냄
    // JSON으로 텍스트 요청을 보내고, base64로 된 오디오 데이터를 받아서 저장
    // api 요청 부분이기 때문에 시간이 좀 걸림 -> 따라서 Coroutine(IEnumerator RequestTTS(string text)) 으로 비동기 처리
    private IEnumerator RequestTTS(string text)
    {
        // google TTS api가 요구하는 요청 형식에 맞게 요청 객체 구성
        var request = new GoogleCloudTextToSpeechRequest
        {
            input = new SynthesisInput { text = text },
            voice = new VoiceSelectionParams { languageCode = "en-US", name = "en-US-Standard-C", ssmlGender = "MALE" },
            audioConfig = new AudioConfig { audioEncoding = "MP3" }
        };
        
        string json = JsonUtility.ToJson(request);      // 요청 객체를 JSON 문자열로 바꿈, unity에서는 서버로 데이터를 보낼 때 JSON형태로 변환해서 보내야함
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);  // 요청 객체를 UTF-8 바이트로 변환, 컴퓨터가 이해할 수 있는 byte로 변환

        string url = "https://texttospeech.googleapis.com/v1/text:synthesize?key=" + apiKey;

        using (UnityWebRequest www = new UnityWebRequest(url, "POST")) // UnityWebRequest: unity에서 HTTP요청을 보낼 수 있게 해주는 클래스임
        {
            // HTTP POST 요청 준비
            www.uploadHandler = new UploadHandlerRaw(bodyRaw); //UploadHandlerRaw: google 서버에 보낼 준비 
            www.downloadHandler = new DownloadHandlerBuffer(); // 서버에서 response가 오면 그 데이터를 받기 위한 버퍼임
            www.SetRequestHeader("Content-Type", "application/json"); // 서버에게 JSON파일을 보낸다고 알려주는 코드, 이걸 보고 어떻게 해석할지 결정 가능함
            
            // 요청 보내고 응답 기다리기
            yield return www.SendWebRequest();
            
            // 연결이 실패되거나 응답이 오지 않거나 등 실패했을 때 
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                yield break;
            }
            
            // 응답이 왔을 때 
            string responseJson = www.downloadHandler.text;
            var response = JsonUtility.FromJson<GoogleCloudTextToSpeechResponse>(responseJson);
            
            // 받은 base64 오디어 데이터를 .mp3파일로 저장
            byte[] audioData = System.Convert.FromBase64String(response.audioContent);
            string filePath = Path.Combine(Application.persistentDataPath, "tts.mp3");
            File.WriteAllBytes(filePath, audioData);

            StartCoroutine(PlayAudio(filePath)); // 저장한 mp3 파일을 실제로 재생
        }
    }
    
    // 위에서 저장한 .mp3파일을 읽고 재생하는 함수
    private IEnumerator PlayAudio(string filePath)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG)) // 지정한 경로에 있는 mp3파일을 오디오 클립으로 불러오는 요청
        {
            yield return www.SendWebRequest();  // 실제로 mp3 파일을 비동기적으로 다운로드 시작함
            
            // 요청 실패 시 에러 출력 후 함수 종료
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                yield break;
            }
            
            // 오디오 파일 재생
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);  // www에서 받아온 mp3 오디오 데이터를 AudioClip형식으로 바꿔서 clip 변수에 저장
                                                                        //AudioClip->클래스, clip->변수, DownloadHandlerAudioClip->클래스, GetContent->함수, www->변수 
            audioSource.clip = clip;    // unity의 audioSource 컴포넌트에 mp3 연결
            audioSource.Play();         // mp3 실제로 재생
        }
    }
}

// JSON 데이터 구조
// 내가 만든 사용자 정의 클래스들을 정의함
// [System.Serializable]: unity가 이 클래스를 JSON으로 변환하거나 파싱할 수 있도록 "이건 직렬화 가능하다" 고 알려주는 마크임 
[System.Serializable]
public class SynthesisInput
{
    public string text;
}

[System.Serializable]
public class VoiceSelectionParams
{
    public string languageCode;
    public string name;
    public string ssmlGender;
}

[System.Serializable]
public class AudioConfig
{
    public string audioEncoding;
}

[System.Serializable]
public class GoogleCloudTextToSpeechRequest
{
    public SynthesisInput input;
    public VoiceSelectionParams voice;
    public AudioConfig audioConfig;
}

[System.Serializable]
public class GoogleCloudTextToSpeechResponse
{
    public string audioContent;
}

[System.Serializable]
public class SecretData
{
    public string googleTTSapiKey;
}