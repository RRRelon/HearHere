using System;
using UnityEngine;

[Serializable]
public class Save
{
    public PlayerDataSO PlayerData;
    
    public string ToJson() => JsonUtility.ToJson(this);

    public void LoadFromJson(string json)
    {
        JsonUtility.FromJsonOverwrite(json, this);   
    }
}
