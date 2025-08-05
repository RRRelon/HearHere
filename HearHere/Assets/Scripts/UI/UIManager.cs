using System;

namespace HH.UI
{
    using System.Collections;
    using DG.Tweening;
    using UnityEngine;
    using UnityEngine.UI;

    public class UIManager : MonoBehaviour
    {
        [SerializeField] private UITTSPulse ttsPulse;
        [SerializeField] private UITTSFlash ttsFlash;

        // 텍스트 한 글자당 깜빡임이 지속될 시간 (초)
        [SerializeField] private float durationPerCharacter = 0.06f;

        [Header("Listening to")] [SerializeField]
        private BoolEventChannelSO onGameClear;

        [SerializeField] private StringEventChannelSO onTextReadyForTTS;

        [Header("비주얼 피드백 디버깅")]
        public bool visualFeedback;
        public bool clear;
        public bool onClear;
        
        private void OnEnable()
        {
            onTextReadyForTTS.OnEventRaised += StartTTSVisualFeedback;
            onGameClear.OnEventRaised += ttsFlash.StartFlashSequence;
        }

        private void OnDisable()
        {
            onTextReadyForTTS.OnEventRaised -= StartTTSVisualFeedback;
            onGameClear.OnEventRaised -= ttsFlash.StartFlashSequence;
        }

        private void Update()
        {
            if (visualFeedback)
            {
                visualFeedback = false;
                StartTTSVisualFeedback("Hello World Hello World Hello World Hello World");
            }

            if (clear)
            {
                clear = false;
                ttsFlash.StartFlashSequence(onClear);
            }
        }

        /// <summary>
        /// StringEventChannelSO에서 호출할 공개 메소드. 시각적 피드백을 시작합니다.
        /// Pulse(일렁임), Flash(깜빡임)
        /// </summary>
        private void StartTTSVisualFeedback(string text)
        {
            // 텍스트 길이에 따라 전체 지속 시간 계산
            float totalDuration = text.Length * durationPerCharacter;

            // // 새로운 깜빡임 코루틴 시작
            // ttsFlash.ActivateFlash(totalDuration);

            // TTS Pulse 활성화
            ttsPulse.ActivatePulse(totalDuration);
        }
    }
}