using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    [SerializeField] bool autoStartOnAwake = true;
    [SerializeField] bool useFixedSeed;
    [SerializeField] int fixedSeed = 1001;
    [SerializeField] GameSceneRefs sceneRefs = new();
    bool ownsRun;

    public RunState CurrentRunState => GetRunServices()?.CurrentRunState;

    void Awake()
    {
        if (!autoStartOnAwake)
            return;
        if (useFixedSeed)
            Random.InitState(fixedSeed);

        var app = GameApp.I;
        if (app == null)
            return;

        app.BeginRun(sceneRefs);
        ownsRun = true;
    }

    void OnDestroy()
    {
        if (!ownsRun)
            return;

        var app = GameApp.I;
        if (app == null)
            return;

        app.EndRun();
    }

    public RunState CreateNewRunState()
    {
        return GetRunServices()?.CreateNewRunState();
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

    public RuleExecutionSummary RunMissionTrigger(string missionUid, string trigger, RuleContext context = null, IRuleEffectApplier effectApplier = null)
    {
        return GetRunServices()?.RunMissionTrigger(missionUid, trigger, context, effectApplier) ?? new RuleExecutionSummary();
    }

    public RuleExecutionSummary RunAdventurerTrigger(string adventurerUid, string trigger, RuleContext context = null, IRuleEffectApplier effectApplier = null)
    {
        return GetRunServices()?.RunAdventurerTrigger(adventurerUid, trigger, context, effectApplier) ?? new RuleExecutionSummary();
    }

    public int GetAdventurerStat(string adventurerUid, StatId statId)
    {
        return GetRunServices()?.GetAdventurerStat(adventurerUid, statId) ?? 0;
    }

    public void MarkAdventurerStatDirty(string adventurerUid)
    {
        GetRunServices()?.MarkAdventurerStatDirty(adventurerUid);
    }

    public void AddOrMergeModifier(ModifierInstance modifier)
    {
        GetRunServices()?.AddOrMergeModifier(modifier);
    }

    public int RemoveMissionLayerModifiers(string missionUid)
    {
        return GetRunServices()?.RemoveMissionLayerModifiers(missionUid) ?? 0;
    }

    public bool TryAssignAdventurerToMission(string adventurerUid, string missionUid)
    {
        return GetRunServices()?.TryAssignAdventurerToMission(adventurerUid, missionUid) ?? false;
    }

    public bool TryUnassignAdventurer(string adventurerUid)
    {
        return GetRunServices()?.TryUnassignAdventurer(adventurerUid) ?? false;
    }

    public AbilityTestResolveResult ResolveMissionAbilityTestOnce(string missionUid)
    {
        return GetRunServices()?.ResolveMissionAbilityTestOnce(missionUid) ?? new AbilityTestResolveResult();
    }

    public bool FailMissionExpedition(string missionUid)
    {
        return GetRunServices()?.FailMissionExpedition(missionUid) ?? false;
    }

    public int AdvanceMissionDeadlinesAndRemoveFailedMissions()
    {
        return GetRunServices()?.AdvanceMissionDeadlinesAndRemoveFailedMissions() ?? 0;
    }

    public bool InitializeRunLoop()
    {
        return GetRunServices()?.InitializeRunLoop() ?? false;
    }

    public bool AdvanceTurn()
    {
        return GetRunServices()?.AdvanceTurn() ?? false;
    }

    public bool TryRecruitCandidate(string candidateUid)
    {
        return GetRunServices()?.TryRecruitCandidate(candidateUid) ?? false;
    }

    public bool SetTraitLocked(string traitUid, bool isLocked)
    {
        return GetRunServices()?.SetTraitLocked(traitUid, isLocked) ?? false;
    }

    RunServices GetRunServices()
    {
        var app = GameApp.I;
        if (app == null)
        {
            Debug.LogError("[GameManager] GameApp is missing.");
            return null;
        }

        if (app.Run == null)
            app.BeginRun(sceneRefs);

        return app.Run;
    }
}
