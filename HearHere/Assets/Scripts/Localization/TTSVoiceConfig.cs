using UnityEngine;

namespace HH.Localization
{
    [CreateAssetMenu(fileName = "TTSVoiceConfig", menuName = "Localization/TTS Voice Config")]
    public class TTSVoiceConfig : ScriptableObject
    {
        [Header("Voice Settings")]
        [SerializeField] private string voiceName = "alloy";
        [SerializeField] private string model = "tts-1";
        [SerializeField] private string languageCode = "en-US";
        
        [Header("Audio Settings")]
        [Range(0.25f, 4.0f)]
        [SerializeField] private float speakingRate = 1.0f;
        [SerializeField] private string audioEncoding = "mp3";

        public string VoiceName => voiceName;
        public string Model => model;
        public string LanguageCode => languageCode;
        public float SpeakingRate => speakingRate;
        public string AudioEncoding => audioEncoding;

        /// <summary>
        /// Validate voice configuration for OpenAI TTS
        /// </summary>
        private void OnValidate()
        {
            // Validate OpenAI TTS voice names
            string[] validVoices = { "alloy", "echo", "fable", "onyx", "nova", "shimmer" };
            bool isValidVoice = false;
            
            foreach (string validVoice in validVoices)
            {
                if (voiceName == validVoice)
                {
                    isValidVoice = true;
                    break;
                }
            }

            if (!isValidVoice)
            {
                Debug.LogWarning($"'{voiceName}' may not be a valid OpenAI TTS voice. Valid voices: {string.Join(", ", validVoices)}");
            }

            // Validate model
            if (model != "tts-1" && model != "tts-1-hd")
            {
                Debug.LogWarning($"'{model}' may not be a valid OpenAI TTS model. Valid models: tts-1, tts-1-hd");
            }
        }
    }
}