using System;

namespace HH.UI
{
    using System.Collections;
    using DG.Tweening;
    using UnityEngine;
    using UnityEngine.UI;
    
    public class UITTSFlash : MonoBehaviour
    {
        [Header("정답 : 짧은 깜빡임 (초록색)")]
        [SerializeField] private Color shortFlashColor = Color.green;
        [SerializeField] private float shortFlashDuration = 0.3f;   // 짧은 깜빡임의 속도
        [SerializeField] private int shortFlashCount = 3;

        [Header("오답 : 긴 깜빡임 (빨간색)")]
        [SerializeField] private Color longFlashColor = Color.red;
        [SerializeField] private float longFlashDuration = 0.8f;    // 긴 깜빡임의 속도
        [SerializeField] private int longFlashCount = 2;
        
        // [Header("TTS Visual Feedback")]
        // [SerializeField] private Color flashColor = Color.white;                  // 깜빡일 색상
        // [SerializeField] private float durationPerCharacter = 0.06f; // 텍스트 한 글자당 깜빡임이 지속될 시간 (초)
        // [SerializeField] private float minFlashInterval = 0.1f;     // 최소 깜빡임 간격
        // [SerializeField] private float maxFlashInterval = 0.3f;     // 최대 깜빡임 간격
        
        [Header("마이크 입력 시")]
        [SerializeField] private Color blinkColor = Color.blue;
        
        [Header("Listening on")]
        [SerializeField] private BoolEventChannelSO blinkScreenDark;
        
        private Sequence flashSequence;
        private Coroutine flashCoroutine;
        private Color defaultColor;
        private Image targetImage;
        
        private void Awake()
        {
            targetImage = GetComponent<Image>();
            defaultColor = targetImage.color;
        }

        private void OnEnable()
        {
            blinkScreenDark.OnEventRaised += SetScreenDark;
        }

        private void OnDisable()
        {
            blinkScreenDark.OnEventRaised -= SetScreenDark;
        }

        /// <summary>
        /// 정답/오답 시 이미지 깜빡임 애니메이션 시퀀스
        /// </summary>
        public void StartFlashSequence(bool isClear)
        {
            // 만약 이미 TTS 음성 깜빡임 코루틴이 진행 중이라면 중지
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }
            
            // 만약 이미 정답/오답 시퀀스 애니메이션이 진행 중이라면 중지
            if (flashSequence != null && flashSequence.IsActive())
                flashSequence.Kill();

            // 시퀀스 초기화
            flashSequence = DOTween.Sequence();

            var vibrateAction = new TweenCallback(() =>
            {
                #if UNITY_ANDROID
                Handheld.Vibrate();
                #endif
            });
            
            // 게임 클리어(성공) 시 초록색으로 짧게 3번 점등
            if (isClear)
            {
                for (int i = 0; i < 3; ++i)
                {
                    flashSequence.Append(targetImage.DOColor(shortFlashColor, shortFlashDuration))
                        .AppendCallback(vibrateAction)
                        .Append(targetImage.DOColor(default, shortFlashDuration));
                }
            }
            // 게임 오버(실패) 시 빨간색으로 길게 2번 점등
            else
            {
                for (int i = 0; i < 2; ++i)
                {
                    flashSequence.Append(targetImage.DOColor(longFlashColor, longFlashDuration))
                        .AppendCallback(vibrateAction)
                        .Append(targetImage.DOColor(default, longFlashDuration));
                }
            }

            flashSequence.OnComplete(() => { targetImage.color = defaultColor; });
        }
        //
        // /// <summary>
        // /// 정답/오답 시 화면 깜빡임 실행
        // /// </summary>
        // public void ActivateFlash(float duration)
        // {
        //     // 만약 이미 정답/오답 깜빡임 코루틴이 진행 중이라면 중지
        //     if (flashCoroutine != null)
        //     {
        //         StopCoroutine(flashCoroutine);
        //         flashCoroutine = null;
        //     }
        //     
        //     flashCoroutine = StartCoroutine(FlashRoutine(duration));
        // }
        //
        // /// <summary>
        // /// 지정된 시간 동안 이미지를 랜덤하게 깜빡이는 코루틴
        // /// </summary>
        // private IEnumerator FlashRoutine(float totalDuration)
        // {
        //     float elapsedTime = 0f;
        //
        //     while (elapsedTime < totalDuration)
        //     {
        //         // 어두운 색으로 변경
        //         targetImage.DOColor(flashColor, minFlashInterval / 2).SetEase(Ease.OutQuad);
        //         float waitTime1 = UnityEngine.Random.Range(minFlashInterval, maxFlashInterval);
        //         yield return new WaitForSeconds(waitTime1);
        //         elapsedTime += waitTime1;
        //
        //         // 다시 원래 색으로 변경
        //         targetImage.DOColor(defaultColor, minFlashInterval / 2).SetEase(Ease.OutQuad);
        //         float waitTime2 = UnityEngine.Random.Range(minFlashInterval, maxFlashInterval);
        //         yield return new WaitForSeconds(waitTime2);
        //         elapsedTime += waitTime2;
        //     }
        //
        //     // 코루틴이 끝나면 확실하게 기본 색상으로 복원
        //     targetImage.color = defaultColor;
        //     flashCoroutine = null;
        // }
        
        private void SetScreenDark(bool isDark)
        {
            targetImage.color = isDark ? blinkColor : defaultColor;
        }
    }
}
    
