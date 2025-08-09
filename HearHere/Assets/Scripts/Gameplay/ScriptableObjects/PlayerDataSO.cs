using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Gameplay/PlayerDataSO")]
public class PlayerDataSO : ScriptableObject
{
    // 게임 별 시간, 시도 횟수 기록
    public List<PlayerData> Datas = new List<PlayerData>();
    // 전체 평가 지표를 첫번째 결과를 0으로 잡고 기록한다.
    // ex. 0, -1, 0, 1, 2
    public List<int> SequentialRecords = new List<int>();
    // 몇 번 연속 평가가 감소하는지 기록
    public int SequentialDecrease = 0;
    
    [SerializeField] private int gameStageCount = 3; // 총 3개의 게임을 진행
    private List<PlayerData> temporaryGameData = new List<PlayerData>();

    private void OnEnable()
    {
        temporaryGameData = new List<PlayerData>();
        // #if UNITY_EDITOR
        // PlayModeStateListener.OnExitPlayMode += ResetData;
        // #endif
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
            // Pitch 기록
            // 만약 데이터가 없으면 0 추가
            if (SequentialRecords.Count == 0)
                SequentialRecords.Add(0);
            else if (IsImproveThanPrevious())
                SequentialRecords.Add(SequentialRecords[^1] + 1);
            else
                SequentialRecords.Add(SequentialRecords[^1] - 1);
            
            Debug.Log($"<color=green>3개 게임 평균 계산 완료 및 저장: AvgTime={averageTime}, AvgTryCount={averageTryCount}</color>");
            temporaryGameData.Clear();
        }
    }

    public bool IsImproveThanFirst()
    {
        if (Datas.Count <= 1) 
            return false;
        return Datas[0].GetAverage() > Datas[^1].GetAverage();
    }

    public bool IsImproveThanPrevious()
    {
        if (Datas.Count <= 1) 
            return false;
        return Datas[^2].GetAverage() > Datas[^1].GetAverage();
    }

    public int GetImprovementPercentageThanFirst()
    {
        if (Datas.Count <= 1) 
            return -1;

        float firstRecord = Datas[0].GetAverage();
        float lastRecord = Datas[^1].GetAverage();
        if (firstRecord > 0)
        {
            // 향상률(%) = (이전 값 - 현재 값) / 이전 값 * 100
            float improvement = ((firstRecord - lastRecord) / firstRecord) * 100f;
            int improvementPercentage = Mathf.RoundToInt(improvement);

            return improvementPercentage;
        }

        return -1;
    }
    
    public int GetImprovementPercentageThanPrevious()
    {
        if (Datas.Count <= 1) 
            return -1;

        float firstRecord = Datas[0].GetAverage();
        float previousRecord = Datas[^2].GetAverage();
        if (firstRecord > 0)
        {
            // 향상률(%) = (이전 값 - 현재 값) / 이전 값 * 100
            float improvement = ((firstRecord - previousRecord) / firstRecord) * 100f;
            int improvementPercentage = Mathf.RoundToInt(improvement);

            return improvementPercentage;
        }

        return -1;
    }
    
    // #if UNITY_EDITOR
    // private void OnDisable()
    // {
    //     PlayModeStateListener.OnExitPlayMode -= ResetData;
    // }
    //
    // private void ResetData()
    // {
    //     // Datas = new List<PlayerData>();
    //     // SequentialRecords = new List<int>();
    //     // SequentialDecrease = 0;
    // }
    // #endif
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
    
