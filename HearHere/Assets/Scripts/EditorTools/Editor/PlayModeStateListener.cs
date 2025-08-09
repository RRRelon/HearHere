using System;
using UnityEditor;
using UnityEngine;

public class PlayModeStateListener : MonoBehaviour
{
    public static event Action OnExitPlayMode;
    
    static PlayModeStateListener()
    {
        // playModeStateChanged 이벤트(플레이 종료 시)에 함수들 구독
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    // 플레이 모드 상태가 변경될 때마다 호출될 함수입니다.
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // 플레이 모드에서 에디터 모드로 '나가는 중'일 때를 감지
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            PlayModeStateListener.OnExitPlayMode?.Invoke();
        }
    }
}
