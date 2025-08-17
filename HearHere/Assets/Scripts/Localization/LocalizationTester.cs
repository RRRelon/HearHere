using UnityEngine;
using HH.Localization;

namespace HH.Localization
{
    /// <summary>
    /// Component for testing localization functionality
    /// </summary>
    public class LocalizationTester : MonoBehaviour
    {
        [Header("Localization")]
        [SerializeField] private LocalizationManagerSO localizationManager;
        
        [Header("TTS Testing")]
        [SerializeField] private TTSEventChannelSO onTextReadyForTTS;
        
        [Header("Test Keys")]
        [SerializeField] private string[] testKeys = 
        {
            LocalizationKeys.HEADPHONES_RECOMMENDATION,
            LocalizationKeys.MENU_COMMANDS,
            LocalizationKeys.STARTING_GAME,
            LocalizationKeys.IMPROVEMENT_PREVIOUS
        };

        private void Start()
        {
            if (localizationManager != null)
            {
                localizationManager.OnLanguageChanged += OnLanguageChanged;
            }
        }

        private void OnDestroy()
        {
            if (localizationManager != null)
            {
                localizationManager.OnLanguageChanged -= OnLanguageChanged;
            }
        }

        [ContextMenu("Test English TTS")]
        public void TestEnglishTTS()
        {
            if (localizationManager != null)
            {
                localizationManager.SetLanguage(SystemLanguage.English);
                TestTTSWithCurrentLanguage();
            }
        }

        [ContextMenu("Test Korean TTS")]
        public void TestKoreanTTS()
        {
            if (localizationManager != null)
            {
                localizationManager.SetLanguage(SystemLanguage.Korean);
                TestTTSWithCurrentLanguage();
            }
        }

        [ContextMenu("Toggle Language")]
        public void ToggleLanguage()
        {
            if (localizationManager != null)
            {
                localizationManager.ToggleLanguage();
            }
        }

        [ContextMenu("Auto Detect Language")]
        public void AutoDetectLanguage()
        {
            if (localizationManager != null)
            {
                localizationManager.AutoDetectLanguage();
            }
        }

        [ContextMenu("Test All Localizations")]
        public void TestAllLocalizations()
        {
            if (localizationManager == null)
            {
                Debug.LogError("LocalizationManager not assigned!");
                return;
            }

            Debug.Log($"Current Language: {localizationManager.CurrentLanguage}");
            Debug.Log($"Current Voice Config: {localizationManager.GetCurrentVoiceConfig()?.VoiceName}");

            foreach (string key in testKeys)
            {
                string localizedText = localizationManager.GetText(key);
                Debug.Log($"Key: {key} | Text: {localizedText}");
            }
        }

        private void TestTTSWithCurrentLanguage()
        {
            if (localizationManager == null || onTextReadyForTTS == null)
            {
                Debug.LogError("Required components not assigned for TTS testing!");
                return;
            }

            string testMessage = localizationManager.GetText(LocalizationKeys.HEADPHONES_RECOMMENDATION);
            onTextReadyForTTS.OnEventRaised(testMessage, true);
            
            Debug.Log($"Testing TTS with {localizationManager.CurrentLanguage}: {testMessage}");
        }

        private void OnLanguageChanged(SystemLanguage newLanguage)
        {
            Debug.Log($"Language changed to: {newLanguage}");
            
            var voiceConfig = localizationManager.GetCurrentVoiceConfig();
            if (voiceConfig != null)
            {
                Debug.Log($"Voice Config - Name: {voiceConfig.VoiceName}, Model: {voiceConfig.Model}, Language: {voiceConfig.LanguageCode}");
            }
        }

        private void Update()
        {
            // Keyboard shortcuts for testing
            if (Input.GetKeyDown(KeyCode.E))
            {
                TestEnglishTTS();
            }
            else if (Input.GetKeyDown(KeyCode.K))
            {
                TestKoreanTTS();
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                ToggleLanguage();
            }
        }
    }
}