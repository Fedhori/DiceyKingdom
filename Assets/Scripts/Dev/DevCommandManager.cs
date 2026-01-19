// ... 기존 using 생략

using System;
using System.Collections;
using System.Collections.Generic;
using Data;
using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-10000)]
public sealed class DevCommandManager : MonoBehaviour
{
#if DEVCONSOLE_OFF
    void Awake() => Destroy(gameObject);
#else
    private const string DevInputCtrl = "devconsole_input";
    public static DevCommandManager Instance { get; private set; }

    public bool IsOpen => open;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        open = !startClosed;

        // 명령어
        Register("additem", param =>
        {
            if (param.Length != 2)
            {
                Debug.LogWarning("[DevCommand] Usage: additem <itemId> <slotIndex>");
                return;
            }

            if (ItemSlotManager.Instance == null)
            {
                Debug.LogWarning("[DevCommand] ItemSlotManager.Instance is null.");
                return;
            }

            string itemId = param[0];
            if (!int.TryParse(param[1], out int slotIndex))
            {
                Debug.LogWarning($"[DevCommand] additem invalid slotIndex: {param[1]}");
                return;
            }

            if (!ItemSlotManager.Instance.TryAddItemAt(itemId, slotIndex, out _))
            {
                Debug.LogWarning($"[DevCommand] additem failed: id={itemId}, slot={slotIndex}");
            }
        });

        Register("upgradeitem", param =>
        {
            if (param.Length != 2)
            {
                Debug.LogWarning("[DevCommand] Usage: upgrade <upgradeId> <slotIndex>");
                return;
            }

            if (!UpgradeRepository.IsInitialized)
            {
                Debug.LogWarning("[DevCommand] UpgradeRepository not initialized.");
                return;
            }

            var itemManager = ItemManager.Instance;
            if (itemManager == null)
            {
                Debug.LogWarning("[DevCommand] ItemManager.Instance is null.");
                return;
            }

            var inventory = itemManager.Inventory;
            if (inventory == null)
            {
                Debug.LogWarning("[DevCommand] ItemManager.Inventory is null.");
                return;
            }

            string upgradeId = param[0];
            if (!UpgradeRepository.TryGet(upgradeId, out var dto) || dto == null)
            {
                Debug.LogWarning($"[DevCommand] upgrade not found: {upgradeId}");
                return;
            }

            if (!int.TryParse(param[1], out int slotIndex))
            {
                Debug.LogWarning($"[DevCommand] upgrade invalid slotIndex: {param[1]}");
                return;
            }

            if (slotIndex < 0 || slotIndex >= inventory.SlotCount)
            {
                Debug.LogWarning($"[DevCommand] upgrade invalid slotIndex: {slotIndex}");
                return;
            }

            var targetItem = inventory.GetSlot(slotIndex);
            if (targetItem == null)
            {
                Debug.LogWarning($"[DevCommand] upgrade slot is empty: {slotIndex}");
                return;
            }

            var upgrade = new UpgradeInstance(dto);
            if (!upgrade.IsApplicable(targetItem))
            {
                Debug.LogWarning($"[DevCommand] upgrade not applicable: id={upgradeId}, slot={slotIndex}");
                return;
            }

            var upgradeManager = UpgradeManager.Instance;
            if (upgradeManager == null)
            {
                Debug.LogWarning("[DevCommand] UpgradeManager.Instance is null.");
                return;
            }

            if (!upgradeManager.ApplyUpgrade(targetItem, upgrade))
            {
                Debug.LogWarning($"[DevCommand] upgrade apply failed: id={upgradeId}, slot={slotIndex}");
            }
        });

        Register("addupgrade", param =>
        {
            if (param.Length != 1)
            {
                Debug.LogWarning("[DevCommand] Usage: addupgrade <upgradeId>");
                return;
            }

            if (!UpgradeRepository.IsInitialized)
            {
                Debug.LogWarning("[DevCommand] UpgradeRepository not initialized.");
                return;
            }

            var inventoryManager = UpgradeInventoryManager.Instance;
            if (inventoryManager == null)
            {
                Debug.LogWarning("[DevCommand] UpgradeInventoryManager.Instance is null.");
                return;
            }

            string upgradeId = param[0];
            if (!UpgradeRepository.TryGet(upgradeId, out var dto) || dto == null)
            {
                Debug.LogWarning($"[DevCommand] upgrade not found: {upgradeId}");
                return;
            }

            inventoryManager.Add(new UpgradeInstance(dto));
        });

        Register("addcurrency", param =>
        {
            if (param.Length != 1)
                return;

            if (CurrencyManager.Instance == null)
                return;

            CurrencyManager.Instance.AddCurrency(int.Parse(param[0]));
        });

        Register("clearplay", param =>
        {
            if (param.Length != 0)
            {
                Debug.LogWarning("[DevCommand] Usage: clearplay");
                return;
            }

            if (StageManager.Instance != null && StageManager.Instance.CurrentPhase != StagePhase.Play)
            {
                Debug.LogWarning("[DevCommand] clearplay is only available during Play phase.");
                return;
            }

            BlockManager.Instance?.ClearAllBlocks();
            PlayManager.Instance?.FinishPlay();
        });
    }

    [Header("Toggle")] public KeyCode toggleKey = KeyCode.BackQuote;
    public bool startClosed = true;

    private readonly Dictionary<string, Action<string[]>> handlers =
        new(StringComparer.OrdinalIgnoreCase);

    public static void Register(string command, Action<string[]> handler)
    {
        if (Instance == null) return;
        Instance.handlers[command] = handler;
    }

    private bool pendingClear;
    private bool open;
    private string line = "";
    private GUIStyle inputStyle;

    public void ToggleOpen()
    {
        open = !open;
        if (open) pendingClear = true; // 다음 OnGUI에서 한 번만 비움+포커스
    }

    private void OnGUI()
    {
        if (!open) return;

        // 스타일 캐싱(원하면 fontSize만 세팅)
        if (inputStyle == null || inputStyle.fontSize != 32)
        {
            inputStyle = new GUIStyle(GUI.skin.textField) { fontSize = 32, alignment = TextAnchor.MiddleLeft };
        }

        float h = Mathf.Ceil(inputStyle.lineHeight) + inputStyle.padding.vertical;
        var rect = new Rect(8, 8, Screen.width - 16, h);

        if (pendingClear)
        {
            line = string.Empty;
            GUI.FocusControl(DevInputCtrl);
            pendingClear = false;
        }

        GUI.SetNextControlName(DevInputCtrl);
        line = GUI.TextField(rect, line, inputStyle);
        GUI.FocusControl(DevInputCtrl);

        var e = Event.current;
        if (GUI.GetNameOfFocusedControl() == DevInputCtrl)
        {
            // KeyDown(Return/KeypadEnter)
            if (e.type == EventType.KeyDown &&
                (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                Execute(line);
                line = "";
                e.Use();
            }
            // 일부 IME/플랫폼 보정: character 버전
            else if (e.type == EventType.KeyDown && (e.character == '\n' || e.character == '\r'))
            {
                Execute(line);
                line = "";
                e.Use();
            }
            // 드물게 KeyUp만 오는 경우 보정
            else if (e.type == EventType.KeyUp &&
                     (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                Execute(line);
                line = "";
                e.Use();
            }
        }
    }

    private void Execute(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine)) return;
        commandLine = commandLine.Trim();

        // 공백 기준으로 토큰 분리
        string[] tokens = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return;

        string cmd = tokens[0];

        string[] args = Array.Empty<string>();
        if (tokens.Length > 1)
        {
            args = new string[tokens.Length - 1];
            Array.Copy(tokens, 1, args, 0, args.Length);
        }

        if (handlers.TryGetValue(cmd, out var handler))
        {
            try
            {
                handler(args);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DevConsole] {cmd} error: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"[DevConsole] Unknown command: {cmd}");
        }

        SuppressUiSubmitForOneFrame();
    }

    // 상점 나가기 버튼이 포커스된 상태에서, Enter키를 누를 경우 submit 되면서 상점이 나가지는 버그가 있었음.
    // 콘솔창에서 Enter 입력 시 1프레임을 무시하는 식으로 대응.
    void SuppressUiSubmitForOneFrame()
    {
        var es = EventSystem.current;
        if (es == null)
            return;

        StartCoroutine(SuppressSubmitCoroutine(es));
    }

    IEnumerator SuppressSubmitCoroutine(EventSystem es)
    {
        bool prev = es.sendNavigationEvents;
        es.sendNavigationEvents = false;
        yield return null;
        if (es != null)
            es.sendNavigationEvents = prev;
    }
#endif
}
