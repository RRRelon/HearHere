using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Gameplay/PlayerDataSO")]
public class PlayerDataSO : ScriptableObject
{
    public List<PlayerData> Datas = new List<PlayerData>();
    public int SequentialDecrease = 0;
    
    private List<PlayerData> temporaryGameData = new List<PlayerData>();
    [SerializeField] private const int gameStageCount = 3; // 총 3개의 게임을 진행

    private void OnEnable()
    {
        temporaryGameData = new List<PlayerData>();
    }

    public void AddGameResult(float time, int tryCount)
    {
        PlayerData newData = new PlayerData(time, tryCount);
        temporaryGameData.Add(newData);
        Debug.Log($"Save {temporaryGameData.Count} game data.");
        
        // 만약 모든 스테이지 클리어 시
        if (temporaryGameData.Count >= gameStageCount)
        {
            float averageTime = temporaryGameData.Average(data => data.Time);
            float averageTryCount = temporaryGameData.Average(data => data.TryCount);
            Datas.Add(new PlayerData(averageTime, averageTryCount));
            Debug.Log($"<color=green>3개 게임 평균 계산 완료 및 저장: AvgTime={averageTime}, AvgTryCount={averageTryCount}</color>");
            temporaryGameData.Clear();
        }
    }
}

[Serializable]
public class PlayerData
{
    public float Time;
    public float TryCount;

    public PlayerData(float time, float tryCount)
    {
        Time = time;
        TryCount = tryCount;
    }

    public float GetAverage() => (Time + TryCount * 50) / 2;
}
    
