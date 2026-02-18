using System;
using Newtonsoft.Json;

[Serializable]
public sealed class RunState
{
    public string uid = string.Empty;
    public int seed;
    public int tick;
    public int primaryValue;
    public int secondaryValue;
}

public sealed class RunServices : IDisposable
{
    public RunState CurrentRunState { get; private set; } = new();
    public GameSceneRefs SceneRefs { get; }
    public IReadOnlyObservableValue<int> Tick => tick;
    public IReadOnlyObservableValue<int> PrimaryValue => primaryValue;
    public IReadOnlyObservableValue<int> SecondaryValue => secondaryValue;
    public IReadOnlyObservableValue<int> UiRevision => uiRevision;

    readonly ObservableValue<int> tick = new();
    readonly ObservableValue<int> primaryValue = new();
    readonly ObservableValue<int> secondaryValue = new();
    readonly ObservableValue<int> uiRevision = new();

    public RunServices(GameSceneRefs sceneRefs = null)
    {
        SceneRefs = sceneRefs;
        EnsureRunStateObject();
        SyncUiBindings(forceRevision: true);
    }

    public void Dispose()
    {
        ClearUiBindingListeners();
        CurrentRunState = new RunState();
        SyncUiBindings(forceRevision: true);
    }

    public void NotifyStatePossiblyChanged()
    {
        SyncUiBindings(forceRevision: true);
    }

    public bool InitializeRunIfNeeded(int? seed = null)
    {
        if (!string.IsNullOrWhiteSpace(CurrentRunState?.uid))
            return false;

        CreateNewRunState(seed);
        return true;
    }

    public RunState CreateNewRunState(int? seed = null)
    {
        GameConfigData config = GameConfigProvider.IsLoaded
            ? GameConfigProvider.Current
            : new GameConfigData();

        CurrentRunState = new RunState
        {
            uid = Guid.NewGuid().ToString("N"),
            seed = seed ?? config.defaultRunSeed,
            tick = 0,
            primaryValue = config.startingPrimaryValue,
            secondaryValue = config.startingSecondaryValue
        };
        SyncUiBindings(forceRevision: true);
        return CurrentRunState;
    }

    public void SetRunState(RunState runState)
    {
        CurrentRunState = runState ?? new RunState();
        EnsureRunStateObject();
        SyncUiBindings(forceRevision: true);
    }

    public string ExportRunStateJson(bool prettyPrint = false)
    {
        return JsonConvert.SerializeObject(
            CurrentRunState ?? new RunState(),
            prettyPrint ? Formatting.Indented : Formatting.None);
    }

    public bool TryImportRunStateJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        RunState parsed = JsonConvert.DeserializeObject<RunState>(json);
        if (parsed == null)
            return false;

        SetRunState(parsed);
        return true;
    }

    public void IncrementTick(int amount = 1)
    {
        EnsureRunStateObject();
        if (amount <= 0)
            amount = 1;

        CurrentRunState.tick += amount;
        SyncUiBindings(forceRevision: true);
    }

    public int AddPrimaryValue(int delta)
    {
        EnsureRunStateObject();
        CurrentRunState.primaryValue += delta;
        SyncUiBindings(forceRevision: true);
        return CurrentRunState.primaryValue;
    }

    public int AddSecondaryValue(int delta)
    {
        EnsureRunStateObject();
        CurrentRunState.secondaryValue += delta;
        SyncUiBindings(forceRevision: true);
        return CurrentRunState.secondaryValue;
    }

    void EnsureRunStateObject()
    {
        CurrentRunState ??= new RunState();
    }

    void SyncUiBindings(bool forceRevision = false)
    {
        RunState state = CurrentRunState ?? new RunState();
        bool changed =
            SetObservableValue(tick, state.tick) |
            SetObservableValue(primaryValue, state.primaryValue) |
            SetObservableValue(secondaryValue, state.secondaryValue);

        if (forceRevision || changed)
            uiRevision.Value = uiRevision.Value + 1;
    }

    static bool SetObservableValue(ObservableValue<int> observable, int nextValue)
    {
        if (observable == null)
            return false;

        if (observable.Value == nextValue)
            return false;

        observable.Value = nextValue;
        return true;
    }

    void ClearUiBindingListeners()
    {
        tick.ClearListeners();
        primaryValue.ClearListeners();
        secondaryValue.ClearListeners();
        uiRevision.ClearListeners();
    }
}

