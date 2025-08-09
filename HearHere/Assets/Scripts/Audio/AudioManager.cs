using System.Collections;
using System.Collections.Generic;
using HH;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AIConversationManagerSO manager;
    [SerializeField] private AudioSourceRuntimeAnchorSO ttsRuntimeAnchor;
    
    [Header("Listening to")]
    [SerializeField] private TTSEventChannelSO onTextReadyForTTS;
    [SerializeField] private AudioClipEventChannelSO onAudioClipReadyForTTS;
    
    private AudioSource audioSource;
    private Queue<AudioClip> ttsRequest = new Queue<AudioClip>();
    private Queue<AudioClip> ttsPrioirtyRequest = new Queue<AudioClip>();
    private bool isPlaying = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            Debug.LogError("There is no audio source");
        else
            ttsRuntimeAnchor.SetValue(audioSource);
    }

    private void OnEnable()
    {
        onTextReadyForTTS.OnEventRaised += RequestTTS;
        onAudioClipReadyForTTS.OnEventRaised += AddAudioClipInQueue;
    }

    private void OnDisable()
    {
        onTextReadyForTTS.OnEventRaised -= RequestTTS;
    }

    private void Update()
    {
        // 우선 순위있는 Request는 조건없이 먼저 처리
        if (ttsPrioirtyRequest.Count > 0)
        {
            isPlaying = true;
            AudioClip nextClip = ttsPrioirtyRequest.Dequeue();
            StartCoroutine(PlayAudio(nextClip));
        }
        // Request를 순차적으로 처리
        else if (!isPlaying && ttsRequest.Count > 0)
        {
            isPlaying = true;
            Debug.Log("하나 꺼내기~");
            AudioClip nextClip = ttsRequest.Dequeue();
            StartCoroutine(PlayAudio(nextClip));
        }
    }
    
    /// <summary>
    /// string 타입 text를 오디오 크릷으로 바꾸어 재생
    /// </summary>
    private async void RequestTTS(string text, bool isPriority)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            Debug.Log($"{text} 추가");
            AudioClip textToSpeech = await manager.RequestTextToSpeech(text);
            if (isPriority)
                ttsPrioirtyRequest.Enqueue(textToSpeech);
            else
                ttsRequest.Enqueue(textToSpeech);
        }
    }

    private void AddAudioClipInQueue(AudioClip clip, bool isPriority)
    {
        if (clip != null)
        {
            Debug.Log($"{clip} 추가");
            if (isPriority)
                ttsPrioirtyRequest.Enqueue(clip);
            else
                ttsRequest.Enqueue(clip);
        }
    }

    /// <summary>
    /// AudioClip을 재생하는 함수
    /// </summary>
    private IEnumerator PlayAudio(AudioClip TTSClip)
    {
        audioSource.clip = TTSClip; // unity의 audioSource 컴포넌트에 mp3 연결
        audioSource.Play();         // mp3 실제로 재생
        yield return new WaitWhile(() => audioSource.isPlaying);

        isPlaying = false;
    }
    
    public bool IsPlayingTTS() => isPlaying;
}
