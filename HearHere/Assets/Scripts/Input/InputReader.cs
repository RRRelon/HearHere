using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "InputReader", menuName = "Game/Input Reader")]
public class InputReader : ScriptableObject, GameInput.IGameplayActions
{
    public event UnityAction SpeechEvent = delegate { };
    public event UnityAction SpeechCancelEvent = delegate { };
    
    private GameInput gameInput;

    private void OnEnable()
    {
        if (gameInput == null)
        {
            gameInput = new GameInput();

            gameInput.Gameplay.SetCallbacks(this);
            gameInput.Gameplay.Enable();
        }
    }

    private void OnDisable()
    {
        DisableAllInput();
    }

    public void EnableGameplayInput()
    {
        gameInput.Gameplay.Enable();
    }

    public void DisableAllInput()
    {
        gameInput.Gameplay.Disable();
    }
    
    public void OnSpeech(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
            SpeechEvent.Invoke();
        else if (context.phase == InputActionPhase.Canceled)
            SpeechCancelEvent.Invoke();
    }
}
