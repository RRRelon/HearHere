# HearHere Localization System Setup Guide

## Overview
This localization system provides dynamic language switching between English and Korean with appropriate TTS voice selection for the HearHere audio game.

## Features
- **Dual Language Support**: English and Korean text localization
- **Dynamic TTS Voice Selection**: Automatically selects appropriate voice models based on language
- **Voice Command Language Switching**: Switch languages using voice commands
- **Auto Language Detection**: Automatically detects system language on startup
- **Centralized Key Management**: Type-safe localization keys prevent typos

## Setup Instructions

### 1. Unity Asset Configuration
Create the following ScriptableObject assets in Unity:

#### English Resources (`EnglishTexts.asset`)
- Language: English
- Contains all English text entries
- Reference this in LocalizationManager

#### Korean Resources (`KoreanTexts.asset`)  
- Language: Korean (한국어)
- Contains all Korean text entries
- Reference this in LocalizationManager

#### Voice Configurations
- `EnglishVoiceConfig.asset`: voice="alloy", languageCode="en-US"
- `KoreanVoiceConfig.asset`: voice="onyx", languageCode="ko-KR"

#### Localization Manager (`LocalizationManager.asset`)
- Set currentLanguage to desired default (English=0, Korean=1)
- Assign English/Korean text resources
- Assign English/Korean voice configs

### 2. Scene Setup

#### Required Components
1. **LocalizationManager**: Add to a persistent manager object
2. **LanguageSwitcher**: Add to Client objects that need language switching
3. **LocalizationTester**: Add for testing functionality

#### Component References
Ensure all Client scripts have references to:
- `LocalizationManager` - for getting localized text
- `LanguageSwitcher` - for processing language commands

### 3. Voice Commands

#### English Switch Commands
- "english"
- "switch to english" 
- "language english"

#### Korean Switch Commands
- "korean"
- "switch to korean"
- "language korean"
- "한국어"
- "한국어로 바꿔"

### 4. Code Integration

#### In Client Scripts
```csharp
// Check for language commands first
if (TryProcessLanguageCommand(userText))
{
    return; // Language command processed, exit early
}

// Get localized text
string message = localizationManager?.GetText(LocalizationKeys.MENU_COMMANDS) 
                 ?? "Fallback English text";
```

#### Adding New Text
1. Add key to `LocalizationKeys.cs`
2. Add entries to both English and Korean resource assets
3. Use in code: `localizationManager.GetText(LocalizationKeys.YOUR_KEY)`

## Testing

### Manual Testing
Use the `LocalizationTester` component:
- **E Key**: Test English TTS
- **K Key**: Test Korean TTS  
- **T Key**: Toggle language
- Context menu options available in editor

### Voice Command Testing
1. Say language switch commands during gameplay
2. Verify TTS voice changes appropriately
3. Confirm text content changes language

### Verification Checklist
- [ ] Text changes language when switching
- [ ] TTS voice changes to appropriate model
- [ ] Voice commands work in both languages
- [ ] Auto-detection works on startup
- [ ] Fallback text displays if keys missing
- [ ] Language change feedback TTS plays

## TTS Voice Models

### English (OpenAI TTS)
- **Voice**: "alloy" (clear, neutral voice)
- **Model**: "tts-1" 
- **Language Code**: "en-US"

### Korean (OpenAI TTS)
- **Voice**: "onyx" (good for Korean pronunciation)
- **Model**: "tts-1"
- **Language Code**: "ko-KR"

## File Structure
```
Assets/
├── Scripts/
│   └── Localization/
│       ├── LocalizationManager.cs
│       ├── LocalizationResourceSO.cs
│       ├── TTSVoiceConfig.cs
│       ├── LocalizationKeys.cs
│       ├── LanguageSwitcher.cs
│       └── LocalizationTester.cs
└── Resources/
    └── Localization/
        ├── LocalizationManager.asset
        ├── EnglishTexts.asset
        ├── KoreanTexts.asset
        ├── EnglishVoiceConfig.asset
        └── KoreanVoiceConfig.asset
```

## Troubleshooting

### Common Issues
1. **Missing Text**: Check if key exists in LocalizationKeys and both resource files
2. **Voice Not Changing**: Verify voice configs are assigned to LocalizationManager
3. **Commands Not Working**: Ensure LanguageSwitcher is assigned and language commands are in array
4. **No Auto-Detection**: Check if LocalizationManager.AutoDetectLanguage() is called on startup

### Debug Options
- Enable Debug.Log in LocalizationManager for language changes
- Use LocalizationTester context menu for manual testing
- Check Unity Console for missing key warnings