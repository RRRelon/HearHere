using System;
using System.Collections.Generic;
using UnityEngine;
using HH.Localization;
using SystemLanguage = HH.Localization.SystemLanguage;

[System.Serializable]
public struct LocalizedAnswer
{
    public string englishAnswer;
    public string koreanAnswer;
    
    public string GetAnswerForLanguage(SystemLanguage language)
    {
        return language switch
        {
            SystemLanguage.Korean => koreanAnswer,
            SystemLanguage.English => englishAnswer,
            _ => englishAnswer
        };
    }
    
    public bool ContainsAnswer(string userInput, SystemLanguage language)
    {
        string answerForLanguage = GetAnswerForLanguage(language);
        return !string.IsNullOrEmpty(answerForLanguage) && userInput.ToLower().Contains(answerForLanguage.ToLower());
    }
    
    public bool ContainsAnswerInAnyLanguage(string userInput)
    {
        return ContainsAnswer(userInput, SystemLanguage.English) || 
               ContainsAnswer(userInput, SystemLanguage.Korean);
    }
}

public abstract class MapInfo : MonoBehaviour
{
    [Header("Localization")]
    [SerializeField] protected LocalizationManagerSO localizationManager;
    
    [Header("Answer Settings")]
    [SerializeField] protected LocalizedAnswer answer;         // 실제 정답
    // Debugging 용 Serialize
    [SerializeField] protected int tryCount;          // 클라이언트의 시도 횟수
    [SerializeField] protected List<char> answerChar; // 클라이언트가 수집한 단서

    private void Awake()
    {
        if (answerChar == null)
            answerChar = new List<char>();
    }

    public abstract void GetDialogue();
    public abstract MapResult GetClue(char c);
    public abstract MapResult GetSuccess(char c);
    public int GetTryCount() => tryCount;
    
    /// <summary>
    /// 현재 언어에 맞는 정답 반환
    /// </summary>
    protected string GetCurrentAnswer()
    {
        if (localizationManager != null)
        {
            return answer.GetAnswerForLanguage(localizationManager.CurrentLanguage);
        }
        return answer.englishAnswer;
    }
    
    /// <summary>
    /// 사용자 입력이 정답을 포함하는지 확인 (모든 언어)
    /// </summary>
    public bool CheckAnswerInUserInput(string userInput)
    {
        return answer.ContainsAnswerInAnyLanguage(userInput);
    }
}

/// <summary>
/// 성공 여부(IsSuccess)와 결과 메시지(Message)
/// </summary>
public struct MapResult
{
    // 1. 성공 여부를 담을 bool 변수
    public bool IsValid;

    // 2. 결과 메시지를 담을 string 변수
    public string Message;

    // 3. 이 구조체를 쉽게 생성할 수 있도록 도와주는 생성자
    public MapResult(bool isValid, string message)
    {
        IsValid = isValid;
        Message = message;
    }
}
