using System;
using Game.Common;
using Game.Data;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.App
{
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
            RunState state = RequireCurrentRunState(nameof(InitializeRunIfNeeded));
            if (!string.IsNullOrWhiteSpace(state.uid))
            {
                return false;
            }

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
            if (runState == null)
            {
                const string message = "[RunServices] SetRunState failed: runState is null.";
                Debug.LogError(message);
                throw new ArgumentNullException(nameof(runState), message);
            }

            CurrentRunState = runState;
            SyncUiBindings(forceRevision: true);
        }

        public string ExportRunStateJson(bool prettyPrint = false)
        {
            RunState state = RequireCurrentRunState(nameof(ExportRunStateJson));
            return JsonConvert.SerializeObject(
                state,
                prettyPrint ? Formatting.Indented : Formatting.None);
        }

        public bool TryImportRunStateJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            RunState parsed = JsonConvert.DeserializeObject<RunState>(json);
            if (parsed == null)
            {
                return false;
            }

            SetRunState(parsed);
            return true;
        }

        public void IncrementTick(int amount = 1)
        {
            RunState state = RequireCurrentRunState(nameof(IncrementTick));
            if (amount <= 0)
            {
                amount = 1;
            }

            state.tick += amount;
            SyncUiBindings(forceRevision: true);
        }

        public int AddPrimaryValue(int delta)
        {
            RunState state = RequireCurrentRunState(nameof(AddPrimaryValue));
            state.primaryValue += delta;
            SyncUiBindings(forceRevision: true);
            return state.primaryValue;
        }

        public int AddSecondaryValue(int delta)
        {
            RunState state = RequireCurrentRunState(nameof(AddSecondaryValue));
            state.secondaryValue += delta;
            SyncUiBindings(forceRevision: true);
            return state.secondaryValue;
        }

        void SyncUiBindings(bool forceRevision = false)
        {
            RunState state = RequireCurrentRunState(nameof(SyncUiBindings));
            bool changed =
                SetObservableValue(tick, state.tick) |
                SetObservableValue(primaryValue, state.primaryValue) |
                SetObservableValue(secondaryValue, state.secondaryValue);

            if (forceRevision || changed)
            {
                uiRevision.Value = uiRevision.Value + 1;
            }
        }

        static bool SetObservableValue(ObservableValue<int> observable, int nextValue)
        {
            if (observable == null)
            {
                return false;
            }

            if (observable.Value == nextValue)
            {
                return false;
            }

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

        RunState RequireCurrentRunState(string caller)
        {
            if (CurrentRunState != null)
            {
                return CurrentRunState;
            }

            string message =
                $"[RunServices] Invalid state at {caller}: CurrentRunState is null. " +
                "Call CreateNewRunState or SetRunState with a valid object first.";
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }
    }
}
