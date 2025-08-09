using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/TTS Event Channel")]
public class TTSEventChannelSO : ScriptableObject
{
    public UnityAction<string, bool> OnEventRaised;

    public void RaiseEvent(string text, bool isPrioirty = false)
    {
        if (OnEventRaised != null)
            OnEventRaised.Invoke(text, isPrioirty);
    }
}
