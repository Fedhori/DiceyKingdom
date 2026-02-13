using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    [SerializeField] bool autoStartOnAwake = true;
    [SerializeField] bool useFixedSeed;
    [SerializeField] int fixedSeed = 1001;

    readonly Dictionary<string, SituationDef> situationDefById = new(StringComparer.Ordinal);
    readonly Dictionary<string, AgentDef> agentDefById = new(StringComparer.Ordinal);

    public static GameManager Instance { get; private set; }

    public System.Random Rng { get; private set; } = new();
    public GameStaticDataSet StaticData { get; private set; }
    public GameRunState CurrentRunState { get; private set; }
    public bool IsRunOver { get; private set; }

    public event Action<GameRunState> RunStarted;
    public event Action<GameRunState> RunEnded;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        if (autoStartOnAwake)
            StartNewRun();
    }

    public void StartNewRun(int? seedOverride = null)
    {
        int seed = seedOverride ?? (useFixedSeed ? fixedSeed : Environment.TickCount);

        StaticData = GameStaticDataLoader.LoadAll();
        BuildDefinitionLookups(StaticData);
        ResetRng(seed);

        CurrentRunState = GameRunBootstrap.CreateNewRun(StaticData, seed);
        IsRunOver = false;

        PlayerManager.Instance.InitializeForRun(CurrentRunState);
        AgentManager.Instance.InitializeForRun(CurrentRunState, agentDefById);
        SituationManager.Instance.InitializeForRun(CurrentRunState, StaticData, situationDefById);
        DuelManager.Instance.InitializeForRun(CurrentRunState);
        PhaseManager.Instance.InitializeForRun(CurrentRunState);

        RunStarted?.Invoke(CurrentRunState);
        SituationManager.Instance.NotifyStageSpawned(
            CurrentRunState.stage.stageNumber,
            CurrentRunState.stage.activePresetId);
        PhaseManager.Instance.AdvanceToDecisionPoint();
    }

    public bool EvaluateGameOver()
    {
        if (IsRunOver)
            return true;
        if (PlayerManager.Instance.Stability > 0)
            return false;

        IsRunOver = true;
        RunEnded?.Invoke(CurrentRunState);
        return true;
    }

    public bool TryGetSituationDef(string situationDefId, out SituationDef def)
    {
        if (string.IsNullOrWhiteSpace(situationDefId))
        {
            def = null;
            return false;
        }

        return situationDefById.TryGetValue(situationDefId, out def);
    }

    public bool TryGetAgentDef(string agentDefId, out AgentDef def)
    {
        if (string.IsNullOrWhiteSpace(agentDefId))
        {
            def = null;
            return false;
        }

        return agentDefById.TryGetValue(agentDefId, out def);
    }

    public void ResetRng(int seed)
    {
        Rng = new System.Random(seed);
    }

    public bool TryUseSkillBySlotIndex(
        int skillSlotIndex,
        string selectedAgentInstanceId = null,
        string selectedSituationInstanceId = null,
        int selectedDieIndex = -1)
    {
        return false;
    }

    public bool CanUseSkillBySlotIndex(int skillSlotIndex)
    {
        return false;
    }

    public bool SkillRequiresSituationTargetBySlotIndex(int skillSlotIndex)
    {
        return false;
    }

    public bool TryLoad(out string payloadJson)
    {
        payloadJson = string.Empty;

        var save = SaveManager.Instance;
        if (save == null)
            return false;

        return save.TryLoad(out payloadJson);
    }

    public bool Save(string payloadJson)
    {
        var save = SaveManager.Instance;
        if (save == null)
            return false;

        return save.Save(payloadJson);
    }

    void BuildDefinitionLookups(GameStaticDataSet dataSet)
    {
        situationDefById.Clear();
        agentDefById.Clear();

        if (dataSet?.situationDefs != null)
        {
            for (int i = 0; i < dataSet.situationDefs.Count; i++)
            {
                var def = dataSet.situationDefs[i];
                if (def == null || string.IsNullOrWhiteSpace(def.situationId))
                    continue;

                situationDefById[def.situationId] = def;
            }
        }

        if (dataSet?.agentDefs != null)
        {
            for (int i = 0; i < dataSet.agentDefs.Count; i++)
            {
                var def = dataSet.agentDefs[i];
                if (def == null || string.IsNullOrWhiteSpace(def.agentId))
                    continue;

                agentDefById[def.agentId] = def;
            }
        }
    }
}
