using System.Collections.Generic;
using UnityEngine;

public class MapAlphabet : MapInfo
{
    public override void GetDialogue()
    {
        Debug.Log("Normal 응답");
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
            Debug.Log($"잘못된 단서 : {alphabet}");
            return "";
        }
        
        Debug.Log("Clue 응답");
        tryCount += 1;

        if (answerChar == null)
            answerChar = new List<char>();
        answerChar.Add(alphabet);

        string response = "현재까지 모인 알파벳은 ";
        foreach (char c in answerChar)
        {
            response += c + ',';
        }
        response += " 입니다.";

        return response;
    }

    public override string GetSuccess()
    {
        string response = $"total try is {tryCount}.";
        return response;
    }
}
