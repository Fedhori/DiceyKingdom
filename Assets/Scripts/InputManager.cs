using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [SerializeField] private PlayerInput playerInput;

    [SerializeField] private int maxSlotCount = 9;

    // 슬롯별 액션과, 그 액션에 붙일 콜백 델리게이트를 캐시
    private InputAction[] slotActions;
    private Action<InputAction.CallbackContext>[] slotCallbacks;

    private string playerMapName = "Player";

    private InputActionMap playerMap;
    private InputAction moveAction;

    private void CacheMaps()
    {
        if (playerInput == null) return;
        var asset = playerInput.actions;
        if (playerMap == null) playerMap = asset.FindActionMap(playerMapName, throwIfNotFound: true);

        if (moveAction == null)
            moveAction = asset.FindAction("Move", throwIfNotFound: false);
    }

    private void SetConsoleMode(bool on)
    {
        CacheMaps();
        if (on)
        {
            playerMap.Disable();
        }
        else
        {
            playerMap.Enable();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        RegisterSlotActions();
    }

    private void OnDisable()
    {
        UnregisterSlotActions();
    }

    public InputAction GetAction(string actionName)
    {
        if (playerInput == null || string.IsNullOrEmpty(actionName))
            return null;

        return playerInput.actions.FindAction(actionName, throwIfNotFound: false);
    }

    public float GetMoveX()
    {
        CacheMaps();
        var action = moveAction ?? GetAction("Move");
        if (action == null)
            return 0f;

        Vector2 v = action.ReadValue<Vector2>();
        return v.x;
    }

    public Vector2 GetMoveVector()
    {
        if (Application.isMobilePlatform)
        {
            var joystick = VirtualJoystickController.Instance;
            return joystick != null ? joystick.GetMoveVector() : Vector2.zero;
        }

        CacheMaps();
        var action = moveAction ?? GetAction("Move");
        if (action == null)
            return Vector2.zero;

        return action.ReadValue<Vector2>();
    }

    public InputAction GetSelectSlotAction(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSlotCount)
            return null;

        string actionName = $"SelectSlot{slotIndex + 1}";
        return GetAction(actionName);
    }

    private void RegisterSlotActions()
    {
        if (playerInput == null) return;

        // 배열 준비
        if (slotActions == null || slotActions.Length != maxSlotCount)
            slotActions = new InputAction[maxSlotCount];

        if (slotCallbacks == null || slotCallbacks.Length != maxSlotCount)
            slotCallbacks = new Action<InputAction.CallbackContext>[maxSlotCount];

        // 각 슬롯에 대한 performed 핸들러 등록
        for (int i = 0; i < maxSlotCount; i++)
        {
            var action = GetSelectSlotAction(i);
            slotActions[i] = action;

            if (action == null)
                continue;

            int capturedIndex = i;
            slotCallbacks[i] = _ => OnSelectSlot(capturedIndex);
            action.performed += slotCallbacks[i];
            if (!action.enabled)
                action.Enable();
        }
    }

    private void UnregisterSlotActions()
    {
        if (slotActions == null || slotCallbacks == null) return;

        for (int i = 0; i < maxSlotCount; i++)
        {
            var action = slotActions[i];
            var cb = slotCallbacks[i];

            if (action != null && cb != null)
            {
                action.performed -= cb;
            }
        }
    }
    
    private void OnSelectSlot(int slotIndex0Based)
    {
       
    }

    public void OnToggleOptions()
    {
        OptionManager.Instance.ToggleOption();
    }

    void OnOpenDevCommand()
    {
        var mgr = DevCommandManager.Instance;
        if (mgr == null) return;

        mgr.ToggleOpen();
        SetConsoleMode(mgr.IsOpen);
    }

    void OnPause()
    {
        var gsm = GameSpeedManager.Instance;
        if (gsm == null) return;
        gsm.IsPaused = !gsm.IsPaused;
    }

    void OnFunction1()
    {
        var gsm = GameSpeedManager.Instance;
        if (gsm == null) return;
        gsm.GameSpeed = 1f;
    }

    void OnFunction2()
    {
        var gsm = GameSpeedManager.Instance;
        if (gsm == null) return;
        gsm.GameSpeed = 2f;
    }

    void OnFunction3()
    {
        var gsm = GameSpeedManager.Instance;
        if (gsm == null) return;
        gsm.GameSpeed = 3f;
    }
}
