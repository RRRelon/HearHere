using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class MapInfo : MonoBehaviour
{
    [SerializeField] protected string answer;         // 실제 정답
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
