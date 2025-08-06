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
        tryCount += 1;

        if (answerChar == null)
            answerChar = new List<char>();
        answerChar.Add(alphabet);

        string response = "The alphabets collected so far: ";
        foreach (char c in answerChar)
        {
            response += $"{c},";
        }
        response += ".";

        return response;
    }

    public override string GetSuccess()
    {
        string response = $"total try is {tryCount}.";
        return response;
    }
}
