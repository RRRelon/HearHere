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

        [SerializeField] private AudioManager audioManager;
        
        private void OnEnable()
        {
            onGameClear.OnEventRaised += ttsFlash.StartFlashSequence;
        }

        private void OnDisable()
        {
            onGameClear.OnEventRaised -= ttsFlash.StartFlashSequence;
        }

        private void Update()
        {
            // TTS 실행 중 처리
            if (audioManager.IsPlayingTTS())
            {
                ttsPulse.ActivatePulse();
            }
            // TTS 실행 아닐 시
            else
            {
                ttsPulse.DeactivatePulse();
            }
        }
    }
}