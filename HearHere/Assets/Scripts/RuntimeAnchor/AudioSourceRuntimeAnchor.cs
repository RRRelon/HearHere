using UnityEngine;

[CreateAssetMenu(fileName = "AudioSourceRuntimeAnchor", menuName = "RuntimeAnchor/AudioSourceRuntimeAnchorSO")]
public class AudioSourceRuntimeAnchorSO : ScriptableObject
{
    public AudioSource Value;

    public void SetValue(AudioSource value)
    {
        Value = value;
    }

    public float GetClipRemainedTime()
    {
        if (Value.isPlaying && Value.clip != null)
            return Value.clip.length - Value.time;
        return 0;
    }
}
