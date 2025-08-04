using System.Collections.Generic;
using UnityEngine;

public class MapInfo : MonoBehaviour
{
    [SerializeField] private string answer;
    
    // Debugging 용 Serialize
    [SerializeField] protected int totalCorrect;
    [SerializeField] protected int tryCount;
    
    private List<char> answerChar;
    
    public virtual void GetNormal() { }
    public virtual void GetClue() { }
    public virtual void GetHint() { }
    public virtual void GetResult() { }
    public virtual void CorrectAnswer() { }
}
