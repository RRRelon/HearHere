using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/AudioClip Event Channel")]
public class AudioClipEventChannelSO : ScriptableObject
{
    public UnityAction<AudioClip, bool> OnEventRaised;

    public void RaiseEvent(AudioClip clip, bool isPrioirty = false)
    {
        if (OnEventRaised != null)
            OnEventRaised.Invoke(clip, isPrioirty);
    }
}
