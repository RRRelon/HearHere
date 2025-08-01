using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("깜빡임 설정")]
    [SerializeField] private Image targetImage;

    [Header("짧은 깜빡임 (초록색)")]
    [SerializeField] private Color shortFlashColor = Color.green;
    [SerializeField] private float shortFlashDuration = 0.3f; // 짧은 깜빡임의 속도
    [SerializeField] private int shortFlashCount = 3;

    [Header("긴 깜빡임 (빨간색)")]
    [SerializeField] private Color longFlashColor = Color.red;
    [SerializeField] private float longFlashDuration = 0.8f; // 긴 깜빡임의 속도
    [SerializeField] private int longFlashCount = 2;
    
    [Header("Listening to")]
    [SerializeField] private BoolEventChannelSO onGameClear;
    
    private Sequence flashSequence;
    private Color defaultColor;

    private void Awake()
    {
        defaultColor = targetImage.color;
    }

    private void OnEnable()
    {
        onGameClear.OnEventRaised += StartFlashSequence;
    }

    private void OnDisable()
    {
        onGameClear.OnEventRaised -= StartFlashSequence;
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
            Debug.Log("짧은 시퀀스 넣음");
            flashSequence.Append(
                targetImage.DOColor(shortFlashColor, shortFlashDuration)
                    .SetLoops(shortFlashCount * 2, LoopType.Yoyo));
        }
        // 게임 오버(실패) 시 빨간색으로 길게 2번 점등
        else
        {
            Debug.Log("긴 시퀀스 넣음");
            flashSequence.Append(
                targetImage.DOColor(longFlashColor, longFlashDuration)
                    .SetLoops(longFlashCount * 2, LoopType.Yoyo));
        }
            

        flashSequence.OnComplete(() => 
        {
            targetImage.color = defaultColor;
            Debug.Log("깜빡임 시퀀스 완료");
        });
    }
}
