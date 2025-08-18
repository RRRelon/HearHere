using System;
using System.Collections.Generic;
using UnityEngine;

namespace HH.Localization
{
    public enum SystemLanguage
    {
        English,
        Korean
    }

    [CreateAssetMenu(fileName = "LocalizationManagerSO", menuName = "Localization/Localization Manager SO")]
    public class LocalizationManagerSO : ScriptableObject
    {
        [Header("Current Language")]
        [SerializeField] private SystemLanguage currentLanguage = SystemLanguage.English;
        
        [Header("Text Resources")]
        [SerializeField] private LocalizationResourceSO englishTexts;
        [SerializeField] private LocalizationResourceSO koreanTexts;
        
        [Header("TTS Voice Settings")]
        [SerializeField] private TTSVoiceConfig englishVoiceConfig;
        [SerializeField] private TTSVoiceConfig koreanVoiceConfig;

        public event Action<SystemLanguage> OnLanguageChanged;

        public SystemLanguage CurrentLanguage 
        { 
            get => currentLanguage; 
            private set 
            { 
                if (currentLanguage != value)
                {
                    currentLanguage = value;
                    OnLanguageChanged?.Invoke(currentLanguage);
                }
            } 
        }

        /// <summary>
        /// Get localized text by key
        /// </summary>
        public string GetText(string key)
        {
            var resource = GetCurrentResource();
            return resource?.GetText(key) ?? $"[MISSING: {key}]";
        }

        /// <summary>
        /// Get TTS voice configuration for current language
        /// </summary>
        public TTSVoiceConfig GetCurrentVoiceConfig()
        {
            return currentLanguage switch
            {
                SystemLanguage.English => englishVoiceConfig,
                SystemLanguage.Korean => koreanVoiceConfig,
                _ => englishVoiceConfig
            };
        }

        /// <summary>
        /// Switch to specified language
        /// </summary>
        public void SetLanguage(SystemLanguage language)
        {
            CurrentLanguage = language;
            Debug.Log($"Language changed to: {language}");
        }

        /// <summary>
        /// Toggle between English and Korean
        /// </summary>
        public void ToggleLanguage()
        {
            SetLanguage(currentLanguage == SystemLanguage.English ? SystemLanguage.Korean : SystemLanguage.English);
        }

        /// <summary>
        /// Auto-detect language based on system locale
        /// </summary>
        public void AutoDetectLanguage()
        {
            var systemLanguage = Application.systemLanguage;
            var detectedLanguage = systemLanguage == UnityEngine.SystemLanguage.Korean 
                ? SystemLanguage.Korean 
                : SystemLanguage.English;
            
            SetLanguage(detectedLanguage);
        }

        private LocalizationResourceSO GetCurrentResource()
        {
            return currentLanguage switch
            {
                SystemLanguage.English => englishTexts,
                SystemLanguage.Korean => koreanTexts,
                _ => englishTexts
            };
        }

        private void OnValidate()
        {
            if (englishVoiceConfig == null || koreanVoiceConfig == null)
            {
                Debug.LogWarning("TTS Voice configurations are not set in LocalizationManager");
            }
        }
    }
}