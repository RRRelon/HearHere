using System;
using System.Collections.Generic;
using HH;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TTSManager : MonoBehaviour
{
    [SerializeField] private AIConversationManagerSO manager;

    private AudioSource audioSource;
    private Queue<string> ttsRequest = new Queue<string>();
    private bool isPlaying = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (!isPlaying && ttsRequest.Count > 0)
        {
            isPlaying = true;
            string nextText = ttsRequest.Dequeue();
            
        }
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
        audioSource.clip = TTSClip; // unity의 audioSource 컴포넌트에 mp3 연결
        audioSource.Play();         // mp3 실제로 재생
    }
}
