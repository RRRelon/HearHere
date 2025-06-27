using UnityEngine;

public class MikeListener : MonoBehaviour
{
    public AudioSource audioSource;
    private AudioClip recordedClip;

    void Start()
    {
        Debug.Log("녹음 시작!");
        recordedClip = Microphone.Start(null, false, 5, 48000);
        Invoke(nameof(StopRecordingAndPlay), 5f);
    }

    void StopRecordingAndPlay()
    {
        Microphone.End(null);
        Debug.Log("녹음 종료. 재생 시작!");
        audioSource.clip = recordedClip;
        WavUtility.SaveClipAsWav(recordedClip, Application.persistentDataPath + "/test_record.wav");
    }
}
