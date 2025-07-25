using UnityEngine;
using System;

public class AudioPlayer : MonoBehaviour
{
    public AudioSource audioSource;
    
    public event Action OnAudioPlayStart;
    public event Action OnAudioPlayComplete;
    
    void Start()
    {
        // AudioSource가 없으면 자동 생성
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = 0.8f;
        }
        
        Debug.Log("오디오 플레이어 초기화 완료");
    }
    
    public void PlayTTSAudio(string base64AudioData, int sampleRate)
    {
        try
        {
            // Base64를 바이트 배열로 변환
            byte[] audioBytes = Convert.FromBase64String(base64AudioData);
            
            // WAV를 AudioClip으로 변환
            AudioClip clip = ConvertWavToAudioClip(audioBytes, sampleRate, "TTS_Audio");
            
            if (clip != null)
            {
                PlayAudioClip(clip);
            }
            else
            {
                Debug.LogError("AudioClip 변환 실패");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"TTS 오디오 재생 실패: {e.Message}");
        }
    }
    
    public void PlayAudioClip(AudioClip clip)
    {
        if (audioSource == null)
        {
            Debug.LogError("AudioSource가 없습니다!");
            return;
        }
        
        audioSource.clip = clip;
        audioSource.Play();
        
        Debug.Log($"TTS 오디오 재생 시작: {clip.name}");
        OnAudioPlayStart?.Invoke();
        
        // 재생 완료 감지를 위한 코루틴 시작
        StartCoroutine(WaitForAudioComplete());
    }
    
    private System.Collections.IEnumerator WaitForAudioComplete()
    {
        while (audioSource.isPlaying)
        {
            yield return null;
        }
        
        Debug.Log("TTS 오디오 재생 완료");
        OnAudioPlayComplete?.Invoke();
    }
    
    private AudioClip ConvertWavToAudioClip(byte[] wavData, int sampleRate, string clipName)
    {
        try
        {
            // WAV 헤더 건너뛰기 (44 bytes)
            int headerSize = 44;
            if (wavData.Length <= headerSize)
            {
                Debug.LogError("WAV 데이터가 너무 작습니다");
                return null;
            }
            
            int dataSize = wavData.Length - headerSize;
            float[] samples = new float[dataSize / 2]; // 16-bit audio
            
            for (int i = 0; i < samples.Length; i++)
            {
                int byteIndex = headerSize + i * 2;
                if (byteIndex + 1 < wavData.Length)
                {
                    short sample = (short)(wavData[byteIndex] | (wavData[byteIndex + 1] << 8));
                    samples[i] = sample / 32768f; // Convert to float [-1, 1]
                }
            }
            
            AudioClip clip = AudioClip.Create(clipName, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            
            Debug.Log($"AudioClip 변환 성공: {samples.Length} samples, {sampleRate}Hz");
            return clip;
        }
        catch (Exception e)
        {
            Debug.LogError($"WAV → AudioClip 변환 실패: {e.Message}");
            return null;
        }
    }
    
    public void StopAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("오디오 재생 중지");
        }
    }
    
    public bool IsPlaying()
    {
        return audioSource != null && audioSource.isPlaying;
    }
    
    public void SetVolume(float volume)
    {
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(volume);
            Debug.Log($"볼륨 설정: {volume:F2}");
        }
    }
}