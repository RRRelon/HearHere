using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FadeController : MonoBehaviour
{
    [SerializeField] private Image imageComponent;
    
    [Header("Listening to")]
    [SerializeField] private FadeChannelSO fadeChannel;

    private void OnEnable()
    {
        fadeChannel.OnEventRaised += InitiateFade;
    }

    private void OnDisable()
    {
        fadeChannel.OnEventRaised -= InitiateFade;
    }

    private void InitiateFade(bool fadeIn, float duration, Color desiredColor)
    {
        imageComponent.DOBlendableColor(desiredColor, duration);
    }
}