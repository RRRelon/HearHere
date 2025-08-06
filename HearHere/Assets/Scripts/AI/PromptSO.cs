using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "AI/PromptSO")]
public class PromptSO : ScriptableObject
{
    [TextArea(5, 30)]
    public string Prompt;
}