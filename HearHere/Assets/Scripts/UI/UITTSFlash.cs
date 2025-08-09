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
        
        private void SetScreenDark(bool isDark)
        {
            targetImage.color = isDark ? blinkColor : defaultColor;
        }
    }
}
    
