using UnityEngine;

public sealed class GameService : MonoBehaviour
{
    [SerializeField] bool autoStartOnAwake = true;
    [SerializeField] bool useFixedSeed;
    [SerializeField] int fixedSeed = 1001;
    [SerializeField] GameSceneRefs sceneRefs = new();
    bool ownsRun;
    RunServices startedRun;
    bool missingRunLogged;

    public RunState CurrentRunState => GameApp.I?.Run?.CurrentRunState;

    void Awake()
    {
        if (!autoStartOnAwake)
            return;

        EnsureRunStarted();
    }

    void OnDestroy()
    {
        if (!ownsRun)
            return;

        var app = GameApp.I;
        if (app == null)
            return;

        if (ReferenceEquals(app.Run, startedRun))
            app.EndRun();
    }

    public bool EnsureRunStarted()
    {
        var app = GameApp.I;
        if (app == null)
        {
            Debug.LogError("[GameService] GameApp is missing.");
            return false;
        }

        if (app.Run == null)
        {
            app.BeginRun(sceneRefs);
            ownsRun = true;
            startedRun = app.Run;
        }

        RunServices run = app.Run;
        if (run == null)
            return false;

        run.InitializeRunIfNeeded(useFixedSeed ? fixedSeed : null);
        return true;
    }

    public RunState CreateNewRunState()
    {
        RunServices run = GetRunServices();
        if (run == null)
            return null;

        return run.CreateNewRunState(useFixedSeed ? fixedSeed : null);
    }

    public void SetRunState(RunState runState)
    {
        GetRunServices()?.SetRunState(runState);
    }

    public string ExportRunStateJson(bool prettyPrint = false)
    {
        return GetRunServices()?.ExportRunStateJson(prettyPrint) ?? "{}";
    }

    public bool TryImportRunStateJson(string json)
    {
        return GetRunServices()?.TryImportRunStateJson(json) ?? false;
    }

    public bool InitializeRunLoop()
    {
        return EnsureRunStarted();
    }

    public bool AdvanceTurn()
    {
        RunServices run = GetRunServices();
        if (run == null)
        {
            return false;
        }

        run.IncrementTick();
        return true;
    }

    public int AddPrimaryValue(int delta)
    {
        RunServices run = GetRunServices();
        if (run == null)
            return 0;

        return run.AddPrimaryValue(delta);
    }

    public int AddSecondaryValue(int delta)
    {
        RunServices run = GetRunServices();
        if (run == null)
            return 0;

        return run.AddSecondaryValue(delta);
    }

    RunServices GetRunServices()
    {
        var app = GameApp.I;
        if (app == null)
        {
            if (!missingRunLogged)
            {
                Debug.LogError("[GameService] GameApp is missing.");
                missingRunLogged = true;
            }
            return null;
        }

        if (app.Run == null && !EnsureRunStarted())
            return null;

        if (app.Run == null)
        {
            if (!missingRunLogged)
            {
                Debug.LogError("[GameService] RunServices is null. BeginRun must be called by scene entrypoint.");
                missingRunLogged = true;
            }
            return null;
        }

        missingRunLogged = false;
        return app.Run;
    }
}


