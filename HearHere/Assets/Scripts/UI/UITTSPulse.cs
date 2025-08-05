using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class UITTSPulse : MonoBehaviour
{
    [SerializeField] private float minPulseScale = 1.1f;    // 커지는 최소 크기 배율
    [SerializeField] private float maxPulseScale = 1.4f;    // 커지는 최대 크기 배율
    [SerializeField] private float minPulseInterval = 0.2f; // 한 번 커졌다 작아지는 최소 시간
    [SerializeField] private float maxPulseInterval = 0.5f; // 한 번 커졌다 작아지는 최대 시간

    private Tween pulseTween; // 실행 중인 Tween 제어
    private Image circle;     // 실제 움직일 이미지 
    private Coroutine pulseCoroutine;

    private void Awake()
    {
        circle = GetComponent<Image>();
    }

    public void ActivatePulse(float duration)
    {
        // 만약 이미 깜빡임 코루틴이 진행 중이라면 중지
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        
        pulseCoroutine = StartCoroutine(PulseRoutine(duration));
    }
    
    private IEnumerator PulseRoutine(float duration)
    {
        // 기존에 실행되던 Pulse가 있다면 중지
        pulseTween?.Kill();

        float elapsedTime = 0f;
        Vector3 originalScale = circle.transform.localScale;

        while (elapsedTime < duration)
        {
            // 이번 Pulse에 사용할 랜덤 값 설정
            float pulseDuration = Random.Range(minPulseInterval, maxPulseInterval);
            float targetScaleMultiplier = Random.Range(minPulseScale, maxPulseScale);
            
            // DOTween 시퀀스 생성
            pulseTween = DOTween.Sequence()
                .Append(circle.transform.DOScale(originalScale * targetScaleMultiplier, pulseDuration / 2)) // 커지는 애니메이션
                .Append(circle.transform.DOScale(originalScale, pulseDuration / 2));                      // 작아지는 애니메이션

            // 이번 Pulse 애니메이션이 끝날 때까지 대기
            yield return pulseTween.WaitForCompletion();

            elapsedTime += pulseDuration;
        }

        // 코루틴이 끝나면 크기를 원래대로 복구
        circle.transform.localScale = originalScale;

        pulseCoroutine = null;
    }
}
