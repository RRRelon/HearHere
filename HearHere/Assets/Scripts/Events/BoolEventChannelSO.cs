using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Bool Event Channel")]
public class BoolEventChannelSO : ScriptableObject
{
    public UnityAction<bool> OnEventRaised;

    public void RaiseEvent(bool value)
    {
        if (OnEventRaised != null)
            OnEventRaised.Invoke(value);
    }
}
