namespace HH.UI
{
    using System.Collections;
    using DG.Tweening;
    using UnityEngine;
    using UnityEngine.UI;
    
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private InputReader inputReader;
        
        [Header("깜빡임 설정")]
        [SerializeField] private Image targetImage;

        [Header("화면 터치 시 색")]
        [SerializeField] private Color pressColor;

        [Header("짧은 깜빡임 (초록색)")]
        [SerializeField] private Color shortFlashColor;
        [SerializeField] private float shortFlashDuration = 0.3f;   // 짧은 깜빡임의 속도
        [SerializeField] private int shortFlashCount = 3;

        [Header("긴 깜빡임 (빨간색)")]
        [SerializeField] private Color longFlashColor;
        [SerializeField] private float longFlashDuration = 0.8f;    // 긴 깜빡임의 속도
        [SerializeField] private int longFlashCount = 2;
        
        [Header("TTS Visual Feedback")]
        [SerializeField] private Color flashColor;                  // 깜빡일 색상
        [SerializeField] private float durationPerCharacter = 0.06f; // 텍스트 한 글자당 깜빡임이 지속될 시간 (초)
        [SerializeField] private float minFlashInterval = 0.1f;     // 최소 깜빡임 간격
        [SerializeField] private float maxFlashInterval = 0.3f;     // 최대 깜빡임 간격
        
        [Header("Listening to")]
        [SerializeField] private BoolEventChannelSO onGameClear;
        [SerializeField] private StringEventChannelSO onTextReadyForTTS;
        
        private Sequence flashSequence;
        private Coroutine flashCoroutine;
        private Color defaultColor;

        private void Awake()
        {
            defaultColor = targetImage.color;
        }

        private void OnEnable()
        {
            inputReader.SpeechEvent += OnScreenPressed; 
            inputReader.SpeechCancelEvent += OnScreenReleased;
            onGameClear.OnEventRaised += StartFlashSequence;
            onTextReadyForTTS.OnEventRaised += StartTTSVisualFeedback;
        }

        private void OnDisable()
        {
            inputReader.SpeechEvent -= OnScreenPressed; 
            inputReader.SpeechCancelEvent -= OnScreenReleased;
            onGameClear.OnEventRaised -= StartFlashSequence;
            onTextReadyForTTS.OnEventRaised -= StartTTSVisualFeedback;
        }

        private void OnScreenPressed()
        {
            Debug.Log("화면 터치 시 이벤트");
            targetImage.DOColor(pressColor, 0.1f);
        }

        private void OnScreenReleased()
        {
            targetImage.DOColor(defaultColor, 0.1f);
        }

        /// <summary>
        /// 이미지 깜빡임 애니메이션 시퀀스
        /// </summary>
        private void StartFlashSequence(bool isClear)
        {
            // 만약 이미 시퀀스 애니메이션이 진행 중이라면 중지
            if (flashSequence != null && flashSequence.IsActive())
                flashSequence.Kill();

            // 시퀀스 초기화
            flashSequence = DOTween.Sequence();

            // 게임 클리어(성공) 시 초록색으로 짧게 3번 점등
            if (isClear)
            {
                flashSequence.Append(
                    targetImage.DOColor(shortFlashColor, shortFlashDuration)
                        .SetLoops(shortFlashCount * 2, LoopType.Yoyo));
            }
            // 게임 오버(실패) 시 빨간색으로 길게 2번 점등
            else
            {
                flashSequence.Append(
                    targetImage.DOColor(longFlashColor, longFlashDuration)
                        .SetLoops(longFlashCount * 2, LoopType.Yoyo));
            }

            flashSequence.OnComplete(() => { targetImage.color = defaultColor; });
        }
        
        /// <summary>
        /// StringEventChannelSO에서 호출할 공개 메소드. 시각적 피드백을 시작합니다.
        /// </summary>
        private void StartTTSVisualFeedback(string text)
        {
            // 화면 깜빡임 이벤트 중이면 실행 안 함
            if (flashSequence != null)
                return;
            
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
            Debug.Log($"total duration: {totalDuration}");
            
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
    
