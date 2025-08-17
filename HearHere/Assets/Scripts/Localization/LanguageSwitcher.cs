using UnityEngine;
using HH.Localization;

namespace HH.Localization
{
    /// <summary>
    /// Component that allows switching languages through voice commands or UI
    /// </summary>
    public class LanguageSwitcher : MonoBehaviour
    {
        [Header("Localization")]
        [SerializeField] private LocalizationManagerSO localizationManager;
        
        [Header("TTS Feedback")]
        [SerializeField] private TTSEventChannelSO onTextReadyForTTS;
        
        [Header("Voice Commands")]
        [SerializeField] private string[] englishSwitchCommands = { "english", "switch to english", "language english" };
        [SerializeField] private string[] koreanSwitchCommands = { "korean", "switch to korean", "language korean", "한국어", "한국어로 바꿔" };
        
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
        
        /// <summary>
        /// Process user input for language switching commands
        /// </summary>
        public bool ProcessLanguageCommand(string userInput)
        {
            if (string.IsNullOrWhiteSpace(userInput) || localizationManager == null)
                return false;
                
            userInput = userInput.ToLower().Trim();
            
            // Check for English switch commands
            foreach (string command in englishSwitchCommands)
            {
                if (userInput.Contains(command))
                {
                    SwitchToEnglish();
                    return true;
                }
            }
            
            // Check for Korean switch commands
            foreach (string command in koreanSwitchCommands)
            {
                if (userInput.Contains(command))
                {
                    SwitchToKorean();
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Switch to English language
        /// </summary>
        public void SwitchToEnglish()
        {
            if (localizationManager != null)
            {
                localizationManager.SetLanguage(SystemLanguage.English);
            }
        }
        
        /// <summary>
        /// Switch to Korean language
        /// </summary>
        public void SwitchToKorean()
        {
            if (localizationManager != null)
            {
                localizationManager.SetLanguage(SystemLanguage.Korean);
            }
        }
        
        /// <summary>
        /// Toggle between English and Korean
        /// </summary>
        public void ToggleLanguage()
        {
            if (localizationManager != null)
            {
                localizationManager.ToggleLanguage();
            }
        }
        
        /// <summary>
        /// Auto-detect language based on system settings
        /// </summary>
        public void AutoDetectLanguage()
        {
            if (localizationManager != null)
            {
                localizationManager.AutoDetectLanguage();
            }
        }
        
        private void OnLanguageChanged(SystemLanguage newLanguage)
        {
            // Provide TTS feedback when language changes
            if (onTextReadyForTTS != null && localizationManager != null)
            {
                string feedbackKey = newLanguage == SystemLanguage.English 
                    ? LocalizationKeys.LANGUAGE_CHANGED_TO_ENGLISH 
                    : LocalizationKeys.LANGUAGE_CHANGED_TO_KOREAN;
                
                string feedbackMessage = localizationManager.GetText(feedbackKey);
                onTextReadyForTTS.OnEventRaised(feedbackMessage, true);
            }
            
            Debug.Log($"Language switched to: {newLanguage}");
        }
    }
}