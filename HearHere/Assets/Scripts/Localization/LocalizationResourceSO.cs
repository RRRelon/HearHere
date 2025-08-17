using System;
using System.Collections.Generic;
using UnityEngine;

namespace HH.Localization
{
    [Serializable]
    public class LocalizationEntry
    {
        public string key;
        [TextArea(2, 5)]
        public string value;
    }

    [CreateAssetMenu(fileName = "LocalizationResource", menuName = "Localization/Localization Resource")]
    public class LocalizationResourceSO : ScriptableObject
    {
        [Header("Language Info")]
        [SerializeField] private SystemLanguage language;
        [SerializeField] private string languageName;
        
        [Header("Text Entries")]
        [SerializeField] private LocalizationEntry[] entries;

        private Dictionary<string, string> textDictionary;

        public SystemLanguage Language => language;
        public string LanguageName => languageName;

        private void OnEnable()
        {
            BuildDictionary();
        }

        /// <summary>
        /// Get localized text by key
        /// </summary>
        public string GetText(string key)
        {
            if (textDictionary == null)
                BuildDictionary();

            return textDictionary.TryGetValue(key, out string value) ? value : null;
        }

        /// <summary>
        /// Check if key exists in this resource
        /// </summary>
        public bool HasKey(string key)
        {
            if (textDictionary == null)
                BuildDictionary();

            return textDictionary.ContainsKey(key);
        }

        /// <summary>
        /// Get all available keys
        /// </summary>
        public string[] GetAllKeys()
        {
            if (textDictionary == null)
                BuildDictionary();

            var keys = new string[textDictionary.Count];
            textDictionary.Keys.CopyTo(keys, 0);
            return keys;
        }

        private void BuildDictionary()
        {
            textDictionary = new Dictionary<string, string>();
            
            if (entries == null) return;

            foreach (var entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.key))
                {
                    if (textDictionary.ContainsKey(entry.key))
                    {
                        Debug.LogWarning($"Duplicate key '{entry.key}' found in {name}");
                    }
                    else
                    {
                        textDictionary[entry.key] = entry.value;
                    }
                }
            }
        }

        private void OnValidate()
        {
            // Validate entries in editor
            if (entries != null)
            {
                var keySet = new HashSet<string>();
                foreach (var entry in entries)
                {
                    if (!string.IsNullOrEmpty(entry.key))
                    {
                        if (keySet.Contains(entry.key))
                        {
                            Debug.LogError($"Duplicate key '{entry.key}' found in {name}");
                        }
                        keySet.Add(entry.key);
                    }
                }
            }
        }
    }
}