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

        var app = GameApp.I;
        if (app == null)
        {
            Debug.LogError("[GameService] GameApp is missing.");
            return;
        }

        if (app.Run != null)
            return;

        if (useFixedSeed)
            Random.InitState(fixedSeed);

        app.BeginRun(sceneRefs);
        ownsRun = true;
        startedRun = app.Run;
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
            if (!missingRunLogged)
            {
                Debug.LogError("[GameService] GameApp is missing.");
                missingRunLogged = true;
            }
            return null;
        }

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


