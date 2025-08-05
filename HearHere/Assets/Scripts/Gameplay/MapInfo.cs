using System.Collections.Generic;
using UnityEngine;

public abstract class MapInfo : MonoBehaviour
{
    // Debugging 용 Serialize
    [SerializeField] protected int totalCorrect;
    [SerializeField] protected int tryCount;
    [SerializeField] protected List<char> answerChar;

    public abstract void GetDialogue();
    public abstract string GetClue(char c);
}
