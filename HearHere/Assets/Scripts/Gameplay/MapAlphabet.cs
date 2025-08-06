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
    public override string GetClue(char alphabet)
    {
        if (string.IsNullOrWhiteSpace(alphabet.ToString()))
        {
            Debug.Log($"Invalid clue : {alphabet}");
            return "";
        }
        
        Debug.Log("Clue response");

        if (answerChar == null)
            answerChar = new List<char>();

        string response = "";
        
        // 만약 이미 수집한 단서라면, 새로 추가하지 않는다.
        if (answerChar.Contains(alphabet))
        {
            response = "This clue has already been collected. ";
        }
        else
        {
            answerChar.Add(alphabet);
        }
        
        // 시도 횟수 하나 증가
        tryCount += 1;
        
        response += "The alphabets collected so far: ";
        foreach (char c in answerChar)
            response += c.ToString() + ',';


        return response;
    }

    public override string GetSuccess()
    {
        string response = $"total try is {tryCount}.";
        return response;
    }
}
