using System;
using System.Collections.Generic;
using UnityEngine;

public class MapTutorial : MapInfo
{
    [SerializeField] private List<GameObject> audioSources;
    
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

    public override void GetDialogue()
    {
        
    }

    public override MapResult GetClue(char sequenceChar)
    {
        MapResult result; // 반환한 응답 결과
        int sequenceNum = 0;  // sequence character를 int로 바꿀 변수
        
        // 1. Clue 유효성 검사
        if (!int.TryParse(sequenceChar.ToString(), out sequenceNum))
        {
            Debug.Log($"Invalid clue: {sequenceChar}");
            result = new MapResult(false, "");
            return result;
        }
        
        // 2. 중복 처리 & 현재 순서가 아닌 단서가 왔을 경우
        if (answerChar.Contains(sequenceChar))
        {
            Debug.Log($"Duplicated clue: {sequenceChar}");
            result = new MapResult(false, "");
            return result;
        }

        if (sequenceNum != currentSoundIndex)
        {
            Debug.Log($"Not this time: {sequenceChar}, squence num: {sequenceNum}, current squence num: {currentSoundIndex}");
            result = new MapResult(false, "");
            return result;
        }

        // Success 처리 여기서
        if (sequenceNum == 3)
        {
            result = new MapResult(true, "-1");
            return result;
        }
        
        // 3. 단서 수집 및 현재 시퀀스 증가
        answerChar.Add(sequenceChar);
        tryCount += 1;        // 시도 횟수 하나 증가
        currentSoundIndex += 1; // 시퀀스 하나 추가
        
        // 4. 다음 오디오 소스 틀기
        for (int i = 0; i < audioSources.Count; ++i)
        {
            if (i == currentSoundIndex)
                audioSources[i].SetActive(true);
            else
                audioSources[i].SetActive(false);
        }

        result = new MapResult(true, "");
        return result;
    }
    
    public override MapResult GetSuccess(char isSuccessChar)
    {
        MapResult result;
        int isSuccess = 0;  // 0이면 오답, 1이면 정답
        
        // 1. Clue 유효성 검사
        if (!int.TryParse(isSuccessChar.ToString(), out isSuccess))
        {
            Debug.Log($"Invalid clue: {isSuccessChar}");
            result = new MapResult(false, "You don't have all the clues yet.");
            return result;
        }
        
        // 1. 만약 모든 단서를 수집하지 않은 경우, False 반환 
        if (answerChar.Count < answer.Length)
        {
            result = new MapResult(false, "You don't have all the clues yet.");
            return result;
        }
        // 2. 만약 오답이라면,
        if (isSuccess == 0)
        {
            result = new MapResult(false, $"That's not answer");
            return result;
        }
        // 3. 만약 정답이라면,
        else
        {
            result = new MapResult(true, $"Tutorial Success");
            return result;
        }
    }
}
