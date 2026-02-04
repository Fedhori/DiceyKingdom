using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [SerializeField] private PlayerInput playerInput;
    public event Action OnMenuRequested;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

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
