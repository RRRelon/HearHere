using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuManager : MonoBehaviour
{
    [Header("Scene List")]
    [SerializeField] private List<GameSceneSO> lowSceneList;
    [SerializeField] private List<GameSceneSO> mediumSceneList;
    [SerializeField] private List<GameSceneSO> highSceneList;
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI currentLocationTMP;
    [SerializeField] private Button startButton;
    
    [Header("Broadcasting on")]
    [SerializeField] private LoadEventChannelSO onStartLocation;

    private GameSceneSO randomLocation;

    public void SelectLevel(int lv)
    {
        // Scene List에서 랜덤하게 골라 전환
        switch ((Level)lv)
        {
            case Level.Low:
                randomLocation = lowSceneList[UnityEngine.Random.Range(0, lowSceneList.Count)];
                break;
            case Level.Medium:
                randomLocation = mediumSceneList[UnityEngine.Random.Range(0, mediumSceneList.Count)];
                break;
            case Level.High:
                randomLocation = highSceneList[UnityEngine.Random.Range(0, highSceneList.Count)];
                break;
            default:
                Debug.LogError("Unknown listening level");
                return;
        }
        currentLocationTMP.text = $"현재 스테이지: {randomLocation.name}";
    }

    public void StartGame()
    {
        onStartLocation.OnLoadingRequested(randomLocation);
    }
}

[Serializable]
public enum Level
{
    Low, 
    Medium,
    High
}