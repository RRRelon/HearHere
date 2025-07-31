using System;
using HH;
using UnityEngine;
using UnityEngine.Serialization;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AIConversationManagerSO manager;
    
    [Header("Listening to")]
    [SerializeField] private StringEventChannelSO onTextReadyForTTS;
    
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        onTextReadyForTTS.OnEventRaised += RequestTTS;
    }

    private void OnDisable()
    {
        onTextReadyForTTS.OnEventRaised -= RequestTTS;
    }

    /// <summary>
    /// string 타입 text를 오디오 크릷으로 바꾸어 재생
    /// </summary>
    private async void RequestTTS(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            AudioClip textToSpeech = await manager.RequestTextToSpeech(text);
            PlayAudio(textToSpeech);    
        }
    }

    /// <summary>
    /// AudioClip을 재생하는 함수
    /// </summary>
    private void PlayAudio(AudioClip TTSClip)
    {
        //AudioClip->클래스, clip->변수, DownloadHandlerAudioClip->클래스, GetContent->함수, www->변수 
        audioSource.clip = TTSClip;    // unity의 audioSource 컴포넌트에 mp3 연결
        audioSource.Play();         // mp3 실제로 재생
    }
}
