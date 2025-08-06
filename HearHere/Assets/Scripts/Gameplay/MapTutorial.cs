using System;
using System.Collections.Generic;
using UnityEngine;

public class MapTutorial : MonoBehaviour
{
    [SerializeField] private List<GameObject> audioSources;
    
    // Debugging 용
    [SerializeField] private int currentSoundIndex;

    private void Awake()
    {
        for (int i = 0; i < audioSources.Count; ++i)
        {
            if (i == 0)
                audioSources[i].SetActive(true);
            else
                audioSources[i].SetActive(false);
        }
    }

    /// <summary>
    /// 현재 sound index에 맞는 오브젝트만 Active하고 나머지는 Deactive 한다.
    /// 만약 모든 오브젝트가 한 번씩 Active 됐다면 게임을 종료한다.
    /// </summary>
    /// <returns> 튜토리얼이 끝났으면 True, 아니면 False </returns>
    public bool TryAdvanceToNextSound(int argument)
    {
        if (currentSoundIndex != argument)
            return false;
        
        for (int i = 0; i < audioSources.Count; ++i)
        {
            if (i == currentSoundIndex)
                audioSources[i].SetActive(true);
            else
                audioSources[i].SetActive(false);
        }
        
        currentSoundIndex += 1;
        
        if (currentSoundIndex >= audioSources.Count)
        {
            return true;
        }
        return false;
    }

    public bool IsSameSound(int argument)
    {
        if (currentSoundIndex != argument)
        {
            Debug.Log("다른 토픽 말하는 중");
            return false;
        }
            

        return true;
    }
}
