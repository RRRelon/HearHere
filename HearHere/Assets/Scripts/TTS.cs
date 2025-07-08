using System.Collections;   // ArrayList, Hashtable, Queue, Stack 등의 컬렉션을 제공함
                                // ArrayList: 필요에 따라 크기가 동적으로 증가하는 개체 배열
                                // Hashtable: 키의 해시 코드에 따라 구성된 키/값 쌍의 컬렉션
using System.IO;            // 파일 쓰기 기능??
using System.Text;          // 문자열 인코딩 같은 기능??
using UnityEngine;          // Unity 기본 기능
using UnityEngine.Networking;// InputField, Button 같은 UI 컴포넌트
using UnityEngine.UI;       // 웹 요청 보내는 기능 / 이게 왜 필요함?????????????

public class TTS : MonoBehaviour    // TTS: unity에서 사용하는 TTS script
                                    // MonoBehaviour: unity에서 script를 게임 오브젝트에 붙이기 위한 기본 클래스
{
    // unity editor에서 이 변수들을 inspector 창에 드래그해서 연결해줘야 함
    public InputField inputField;
    public Button playButton;
    public AudioSource audioSource;

    private string apiKey = "!!!!!!!!!!!!!!";// key 넣기!!!!!!!!1

    // start()는 게임이 실행되면 가장 먼저 호출되는 함수
    // 버튼이 클릭되면 inputfield의 텍스트를 읽어서 requestTTS() 실행
    void Start()
    {
        playButton.onClick.AddListener(() =>
        {
            StartCoroutine(RequestTTS(inputField.text));
        });
    }
    
    // google TTS API에 POST 요청을 보냄
    // JSON으로 텍스트 요청을 보내고, base64로 된 오디오 데이터를 받아서 저장
    IEnumerator RequestTTS(string text)
    {
        // 요청 객체 구성
        var request = new GoogleCloudTextToSpeechRequest
        {
            input = new SynthesisInput { text = text },
            voice = new VoiceSelectionParams { languageCode = "ko-KR", name = "ko-KR-Standard-C", ssmlGender = "MALE" }, // language 수정해야 할 듯 -> 입력이 english니까
            audioConfig = new AudioConfig { audioEncoding = "MP3" }
        };
        
        string json = JsonUtility.ToJson(request);      // 요청 객체를 JSON 문자열로 바꿈
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);  // 요청 객체를 UTF-8 바이트로 변환

        string url = "https://texttospeech.googleapis.com/v1/text:synthesize?key=" + apiKey;

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            // HTTP POST 요청 준비
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            
            // 요청 보내고 응답 기다리기
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                yield break;
            }

            string responseJson = www.downloadHandler.text;
            var response = JsonUtility.FromJson<GoogleCloudTextToSpeechResponse>(responseJson);
            
            // 받은 base64 오디어 데이터를 .mp3파일로 저장
            byte[] audioData = System.Convert.FromBase64String(response.audioContent);
            string filePath = Path.Combine(Application.persistentDataPath, "tts.mp3");
            File.WriteAllBytes(filePath, audioData);

            StartCoroutine(PlayAudio(filePath));
        }
    }
    
    // 위에서 저장한 .mp3파일을 읽고 재생하는 함수
    IEnumerator PlayAudio(string filePath)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(www.error);
                yield break;
            }
            
            // 오디오
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}

// JSON 데이터 구조
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
