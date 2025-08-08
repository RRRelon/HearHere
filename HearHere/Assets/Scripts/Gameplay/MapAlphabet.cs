using System.Collections.Generic;
using UnityEngine;

public class MapAlphabet : MapInfo
{
    public override void GetDialogue()
    {
        Debug.Log("Normal response");
        tryCount += 1;
    }

    /// <summary>
    /// 단서 소리 획득 시 알파벳 한 단어를 준다.
    /// </summary>
    /// <param name="alphabet"></param>
    public override MapResult GetClue(char alphabet)
    {
        MapResult result; // 반환한 응답 결과
        
        // 1. Clue 유효성 검사
        if (string.IsNullOrWhiteSpace(alphabet.ToString()))
        {
            Debug.Log($"Invalid clue: {alphabet}");
            result = new MapResult(false, "");
            return result;
        }
        
        // 2. 중복 처리
        if (answerChar.Contains(alphabet))
        {
            Debug.Log($"Duplicated clue: {alphabet}");
            string duplicatedMessage = "This clue has already been collected. ";
            result = new MapResult(false, duplicatedMessage);
            return result;
        }
        
        // 3. 단서 수집
        answerChar.Add(alphabet);
        tryCount += 1;        // 시도 횟수 하나 증가

        string message = "";
        message += "The alphabets collected so far is ";
        foreach (char c in answerChar)
            message += c.ToString() + ',';

        result = new MapResult(true, message);
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
        if (answerChar.Count < answer.Length)
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