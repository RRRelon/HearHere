using System.Collections.Generic;
using UnityEngine;

public abstract class MapInfo : MonoBehaviour
{
    [SerializeField] protected string answer;
    // Debugging 용 Serialize
    [SerializeField] protected int tryCount;
    [SerializeField] protected List<char> answerChar;

    public abstract void GetDialogue();
    public abstract string GetClue(char c);
    public abstract string GetSuccess();
}
