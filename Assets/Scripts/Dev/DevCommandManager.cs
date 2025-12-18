// ... 기존 using 생략

using System;
using System.Collections.Generic;
using UnityEngine;

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
        Register("spawnpin", param =>
        {
            if (param.Length != 3)
                return;

            if (PinManager.Instance == null)
                return;

            PinManager.Instance.TryReplace(param[0], int.Parse(param[1]), int.Parse(param[2]));
        });

        Register("spawnball", param =>
        {
            if (param.Length != 1)
                return;

            if (BallFactory.Instance == null)
                return;

            if (!Enum.TryParse<BallRarity>(param[0], true, out var rarity))
                rarity = BallRarity.Common;

            BallFactory.Instance.SpawnBall(rarity);
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
    }
#endif
}
