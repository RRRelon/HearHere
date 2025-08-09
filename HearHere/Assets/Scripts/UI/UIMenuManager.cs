using System;

namespace HH.UI
{
    using System.Collections;
    using DG.Tweening;
    using UnityEngine;
    using UnityEngine.UI;
    
    public class UIMenuManager : MonoBehaviour
    {
        [Header("Touch Visual Feedback")]
        [SerializeField] private Image targetImage;
        [SerializeField] private Color pressColor;
        
        [Header("TTS Visual Feedback")]
        [SerializeField] private Color flashColor;                  // 깜빡일 색상
        [SerializeField] private float durationPerCharacter = 0.06f; // 텍스트 한 글자당 깜빡임이 지속될 시간 (초)
        [SerializeField] private float minFlashInterval = 0.1f;     // 최소 깜빡임 간격
        [SerializeField] private float maxFlashInterval = 0.3f;     // 최대 깜빡임 간격
        
        [SerializeField] private AudioManager audioManager;
        
        private Coroutine flashCoroutine;
        private Color defaultColor;

        private void Awake()
        {
            defaultColor = targetImage.color;
        }

        private void Update()
        {
            if (audioManager.IsPlayingTTS())
            {
                
            }
        }

        /// <summary>
        /// StringEventChannelSO에서 호출할 공개 메소드. 시각적 피드백을 시작합니다.
        /// </summary>
        private void StartTTSVisualFeedback(string text, bool _)
        {
            // 이전에 실행 중인 코루틴이 있다면 중지
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                // DOTween 애니메이션도 즉시 중지하고 기본 색상으로 복원
                targetImage.DOKill();
                targetImage.color = defaultColor;
            }

            // 텍스트 길이에 따라 전체 지속 시간 계산
            float totalDuration = text.Length * durationPerCharacter;
            
            // 새로운 깜빡임 코루틴 시작
            flashCoroutine = StartCoroutine(FlashRoutine(totalDuration));
        }
        
        /// <summary>
        /// 지정된 시간 동안 이미지를 랜덤하게 깜빡이는 코루틴
        /// </summary>
        private IEnumerator FlashRoutine(float totalDuration)
        {
            float elapsedTime = 0f;

            while (elapsedTime < totalDuration)
            {
                // 어두운 색으로 변경
                targetImage.DOColor(flashColor, minFlashInterval / 2).SetEase(Ease.OutQuad);
                float waitTime1 = UnityEngine.Random.Range(minFlashInterval, maxFlashInterval);
                yield return new WaitForSeconds(waitTime1);
                elapsedTime += waitTime1;

                // 다시 원래 색으로 변경
                targetImage.DOColor(defaultColor, minFlashInterval / 2).SetEase(Ease.OutQuad);
                float waitTime2 = UnityEngine.Random.Range(minFlashInterval, maxFlashInterval);
                yield return new WaitForSeconds(waitTime2);
                elapsedTime += waitTime2;
            }

            // 코루틴이 끝나면 확실하게 기본 색상으로 복원
            targetImage.color = defaultColor;
            flashCoroutine = null;
        }
    }
}