using UnityEngine;
using UnityEngine.InputSystem;
using System;




public class InputService : MonoBehaviour
{
    [SerializeField] private PlayerInput playerInput;
    public event Action OnMenuRequested;

    public InputAction GetAction(string actionName)
    {
        if (playerInput == null || string.IsNullOrEmpty(actionName))
            return null;

        return playerInput.actions.FindAction(actionName, throwIfNotFound: false);
    }

    void OnMenu()
    {
        OnMenuRequested?.Invoke();
    }
}


