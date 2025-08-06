using System.Collections.Generic;
using UnityEngine;

public class MapAlphabet : MapInfo
{
    public override void GetDialogue()
    {
        Debug.Log("Diaglogue 응답");
        tryCount += 1;
    }

    /// <summary>
    /// 단서 소리 획득 시 알파벳 한 단어를 준다.
    /// </summary>
    /// <param name="alphabet"></param>
    public override string GetClue(char alphabet)
    {
        Debug.Log("Clue 응답");
        
        // 만약 단서가 제대로 오지 않았을 경우, 공백 반환
        if (string.IsNullOrWhiteSpace(alphabet.ToString()))
        {
            Debug.Log($"잘못된 단서 : {alphabet}");
            return "";
        }

        // tts text에 추가할 Clue에 대한 정보를 string으로 반환  
        string response = "";
        if (answerChar == null)
            answerChar = new List<char>();
        
        // 만약 이미 수집한 단서라면, 새로 추가하지 않는다.
        if (answerChar.Contains(alphabet))
        {
            response = "이미 수집한 단서입니다. ";
        }
        else
        {
            answerChar.Add(alphabet);
        }
        
        // 시도 횟수 하나 증가
        tryCount += 1;
        
        response += "현재까지 모인 알파벳은 ";
        foreach (char c in answerChar)
            response += c.ToString() + ',';
        response += " 입니다.";

        return response;
    }

    public override string GetSuccess()
    {
        string response = $"total try is {tryCount}.";
        return response;
    }
}
