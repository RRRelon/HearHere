using UnityEngine;

public class MapAlphabet : MapInfo
{
    public override void GetNormal()
    {
        Debug.Log("Normal 응답");
        tryCount += 1;
    }

    public override void GetClue()
    {
        Debug.Log("Clue 응답");
        tryCount += 1;
    }

    public override void GetHint()
    {
        Debug.Log("Hint 응답");
        tryCount += 1;
    }

    public override void GetResult()
    {
        Debug.Log("Result 응답");
        tryCount += 1;
        
        // if ()
    }

    public override void CorrectAnswer()
    {
        Debug.Log("Correct Answer !!!");
    }
}
