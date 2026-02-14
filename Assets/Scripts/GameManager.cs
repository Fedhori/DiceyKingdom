using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public RunState CurrentRunState { get; private set; } = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public RunState CreateNewRunState()
    {
        var runState = new RunState
        {
            uid = System.Guid.NewGuid().ToString("N"),
            barracksCapacity = GameConfigProvider.Current.barracksCapacity
        };

        CurrentRunState = runState;
        return CurrentRunState;
    }

    public void SetRunState(RunState runState)
    {
        CurrentRunState = runState ?? new RunState();
    }

    public string ExportRunStateJson(bool prettyPrint = false)
    {
        return JsonUtility.ToJson(CurrentRunState ?? new RunState(), prettyPrint);
    }

    public bool TryImportRunStateJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        RunState parsed = JsonUtility.FromJson<RunState>(json);
        if (parsed == null)
            return false;

        CurrentRunState = parsed;
        return true;
    }
}
