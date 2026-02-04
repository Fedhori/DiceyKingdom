using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-10000)]
public sealed class DevCommandManager : MonoBehaviour
{
#if DEVCONSOLE_OFF
    void Awake() => Destroy(gameObject);
#else
    const string DevInputCtrl = "devconsole_input";
    public static DevCommandManager Instance { get; private set; }

    public bool IsOpen => open;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        open = !startClosed;
    }

    [Header("Toggle")]
    public KeyCode toggleKey = KeyCode.BackQuote;
    public bool startClosed = true;

    readonly Dictionary<string, Action<string[]>> handlers =
        new(StringComparer.OrdinalIgnoreCase);

    public static void Register(string command, Action<string[]> handler)
    {
        if (Instance == null)
            return;

        if (string.IsNullOrWhiteSpace(command) || handler == null)
            return;

        Instance.handlers[command] = handler;
    }

    bool pendingClear;
    bool open;
    string line = "";
    GUIStyle inputStyle;
    const int MaxHistory = 10;
    readonly List<string> history = new();
    int historyIndex = -1;
    bool wasInputFocused;

    void OnGUI()
    {
        if (!open)
            return;

        if (inputStyle == null || inputStyle.fontSize != 32)
        {
            inputStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 32,
                alignment = TextAnchor.MiddleLeft
            };
        }

        float h = Mathf.Ceil(inputStyle.lineHeight) + inputStyle.padding.vertical;
        var rect = new Rect(8, 8, Screen.width - 16, h);

        if (pendingClear)
        {
            line = string.Empty;
            GUI.FocusControl(DevInputCtrl);
            pendingClear = false;
        }

        var e = Event.current;
        if (wasInputFocused)
        {
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.UpArrow)
            {
                RecallPrevious();
                e.Use();
            }
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.DownArrow)
            {
                RecallNext();
                e.Use();
            }
        }

        GUI.SetNextControlName(DevInputCtrl);
        line = GUI.TextField(rect, line, inputStyle);
        GUI.FocusControl(DevInputCtrl);

        if (GUI.GetNameOfFocusedControl() == DevInputCtrl)
        {
            if (e.type == EventType.KeyDown &&
                (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                Execute(line);
                line = "";
                e.Use();
            }
            else if (e.type == EventType.KeyDown && (e.character == '\n' || e.character == '\r'))
            {
                Execute(line);
                line = "";
                e.Use();
            }
            else if (e.type == EventType.KeyUp &&
                     (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                Execute(line);
                line = "";
                e.Use();
            }
        }

        wasInputFocused = GUI.GetNameOfFocusedControl() == DevInputCtrl;
    }

    void Execute(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            return;

        commandLine = commandLine.Trim();

        string[] tokens = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0)
            return;

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

        PushHistory(commandLine);
        SuppressUiSubmitForOneFrame();
    }

    void PushHistory(string commandLine)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
            return;

        history.Add(commandLine);
        if (history.Count > MaxHistory)
            history.RemoveAt(0);

        historyIndex = history.Count;
    }

    void RecallPrevious()
    {
        if (history.Count == 0)
            return;

        if (historyIndex < 0 || historyIndex > history.Count)
            historyIndex = history.Count;

        if (historyIndex > 0)
            historyIndex--;

        line = history[historyIndex];
    }

    void RecallNext()
    {
        if (history.Count == 0)
            return;

        if (historyIndex < 0)
            historyIndex = history.Count;

        if (historyIndex < history.Count - 1)
        {
            historyIndex++;
            line = history[historyIndex];
            return;
        }

        historyIndex = history.Count;
        line = string.Empty;
    }

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

    public void ToggleOpen()
    {
        open = !open;
        if (open)
            pendingClear = true;
    }
#endif
}
