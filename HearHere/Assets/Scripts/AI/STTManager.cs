using OpenAI;
using Samples.Whisper;
using UnityEngine;


namespace HH
{
    // <summary>
    /// 1. space press 시 마이크 입력 시작, 떼면 마이크 입력 종료
    /// 2. STT로 바꾼 String을 GPT에 넣음
    /// </summary>
    public class STTManager : MonoBehaviour
    {
        [SerializeField] private InputReader inputReader;

        [Header("Broadcasting on")]
        [SerializeField] private StringEventChannelSO onSTTCompleted; 
    
        private const string fileName = "output.wav";
        private int duration = 5;
    
        private AudioClip clip;
        private bool isRecording;
        private float time;
        private OpenAIApi openai = new OpenAIApi(); 

        private void OnEnable()
        {
            inputReader.SpeechEvent += StartRecording;
            inputReader.SpeechCancelEvent += EndRecording;
        }

        private void OnDisable()
        {
            inputReader.SpeechEvent -= StartRecording;
            inputReader.SpeechCancelEvent -= EndRecording;
        }
        private void StartRecording()
        {
            // 마이크 입력 시작
            Debug.Log("Start Recording");
            clip = Microphone.Start(Microphone.devices[0], false, duration, 44100);
        }

        private async void EndRecording()
        {
            // 분석할 동안 입력받기 중단
            inputReader.DisableAllInput();
        
            // STT
            Debug.Log("Transcripting...");
            byte[] data = SaveWav.Save(fileName, clip);
            var req = new CreateAudioTranscriptionsRequest
            {
                FileData = new FileData() {Data = data, Name = "audio.wav"},
                // File = Application.persistentDataPath + "/" + fileName,
                Model = "whisper-1",
                Language = "ko"
            };
            var res = await openai.CreateAudioTranscription(req);
            Debug.Log("End Recording");
            // 다시 스페이스바 입력 받기 시작
            inputReader.EnableGameplayInput();
        
            // GPT에 넘기는 이벤트 실행
            Debug.Log($"STT : {res.Text}");
            onSTTCompleted.OnEventRaised(res.Text);
        }
    }
}