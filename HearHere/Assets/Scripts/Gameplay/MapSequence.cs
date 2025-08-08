using System;
using System.Collections.Generic;
using UnityEngine;

public class MapSequence : MapInfo
{
    [SerializeField] private int currentSequence;
    
    public override void GetDialogue()
    {
        Debug.Log("Normal response");
        tryCount += 1;
    }

    /// <summary>
    /// 단서 소리 획득 시 Sequence 단계 올림
    /// </summary>
    public override MapResult GetClue(char sequenceChar)
    {
        MapResult result; // 반환한 응답 결과
        int sequenceNum = 0;  // sequence character를 int로 바꿀 변수
        
        // 1. Clue 유효성 검사
        if (!int.TryParse(sequenceChar.ToString(), out sequenceNum))
        {
            Debug.Log($"Invalid clue: {sequenceChar}");
            result = new MapResult(false, "");
            return result;
        }
        
        // 2. 중복 처리 & 현재 순서가 아닌 단서가 왔을 경우
        if (answerChar.Contains(sequenceChar))
        {
            Debug.Log($"Duplicated clue: {sequenceChar}");
            result = new MapResult(false, "");
            return result;
        }

        if (sequenceNum != currentSequence)
        {
            Debug.Log($"Not this time: {sequenceChar}, squence num: {sequenceNum}, current squence num: {currentSequence}");
            result = new MapResult(false, "");
            return result;
        }
        
        // Success 처리 여기서
        if (sequenceNum == 2)
        {
            result = new MapResult(true, "-1");
            return result;
        }
        
        // 3. 단서 수집 및 현재 시퀀스 증가
        answerChar.Add(sequenceChar);
        tryCount += 1;        // 시도 횟수 하나 증가
        currentSequence += 1; // 시퀀스 하나 추가

        result = new MapResult(true, $"Collected so far: {answerChar.Count}");
        return result;
    }

    public override MapResult GetSuccess(char isSuccessChar)
    {
        MapResult result;
        int isSuccess = 0;  // 0이면 오답, 1이면 정답
        
        // 1. Clue 유효성 검사
        if (!int.TryParse(isSuccessChar.ToString(), out isSuccess))
        {
            Debug.Log($"Invalid clue: {isSuccessChar}");
            result = new MapResult(false, "You don't have all the clues yet.");
            return result;
        }
        
        // 1. 만약 모든 단서를 수집하지 않은 경우, False 반환 
        if (currentSequence < answer.Length - 1)
        {
            result = new MapResult(false, "You don't have all the clues yet.");
            return result;
        }
        // 2. 만약 오답이라면,
        if (isSuccess == 0)
        {
            result = new MapResult(false, $"That's not answer");
            return result;
        }
        // 3. 만약 정답이라면,
        else
        {
            result = new MapResult(true, $"total try is {tryCount}");
            return result;
        }
    }
}
