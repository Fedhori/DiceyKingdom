using Newtonsoft.Json;

public sealed class RunServices : System.IDisposable
{
    public RunState CurrentRunState { get; private set; } = new();
    public GameSceneRefs SceneRefs { get; }

    StatService statService;
    ModifierService modifierService;
    TraitService traitService;
    IRuleEffectApplier ruleEffectApplier;
    MissionExpeditionService missionExpeditionService;
    TurnLoopService turnLoopService;

    public RunServices(GameSceneRefs sceneRefs = null)
    {
        SceneRefs = sceneRefs;
        InitializeServices();
    }

    public void Dispose()
    {
        statService?.ClearCache();
        CurrentRunState = new RunState();
    }

    public RunState CreateNewRunState()
    {
        var runState = new RunState
        {
            uid = System.Guid.NewGuid().ToString("N"),
            barracksCapacity = GameConfigProvider.Current.barracksCapacity
        };

        CurrentRunState = runState;
        statService.ClearCache();
        return CurrentRunState;
    }

    public void SetRunState(RunState runState)
    {
        CurrentRunState = runState ?? new RunState();
        statService.ClearCache();
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

        CurrentRunState = parsed;
        statService.ClearCache();
        return true;
    }

    public RuleExecutionSummary RunMissionTrigger(string missionUid, string trigger, RuleContext context = null, IRuleEffectApplier effectApplier = null)
    {
        return ExecuteMissionTriggerInternal(missionUid, trigger, context, effectApplier);
    }

    public RuleExecutionSummary RunAdventurerTrigger(string adventurerUid, string trigger, RuleContext context = null, IRuleEffectApplier effectApplier = null)
    {
        return ExecuteAdventurerTriggerInternal(adventurerUid, trigger, context, effectApplier);
    }

    public int GetAdventurerStat(string adventurerUid, StatId statId)
    {
        return statService.GetStat(CurrentRunState, adventurerUid, statId);
    }

    public void MarkAdventurerStatDirty(string adventurerUid)
    {
        statService.MarkDirty(adventurerUid);
    }

    public void AddOrMergeModifier(ModifierInstance modifier)
    {
        modifierService.AddOrMergeModifier(CurrentRunState, modifier);
    }

    public int RemoveMissionLayerModifiers(string missionUid)
    {
        return modifierService.RemoveMissionLayerModifiers(CurrentRunState, missionUid);
    }

    public bool TryAssignAdventurerToMission(string adventurerUid, string missionUid)
    {
        return missionExpeditionService.TryAssignAdventurerToMission(CurrentRunState, adventurerUid, missionUid);
    }

    public bool TryUnassignAdventurer(string adventurerUid)
    {
        return missionExpeditionService.TryUnassignAdventurer(CurrentRunState, adventurerUid);
    }

    public AbilityTestResolveResult ResolveMissionAbilityTestOnce(string missionUid)
    {
        return missionExpeditionService.ResolveAbilityTestOnce(CurrentRunState, missionUid);
    }

    public bool FailMissionExpedition(string missionUid)
    {
        return missionExpeditionService.FailExpedition(CurrentRunState, missionUid);
    }

    public int AdvanceMissionDeadlinesAndRemoveFailedMissions()
    {
        return missionExpeditionService.AdvanceMissionDeadlines(CurrentRunState);
    }

    public bool InitializeRunLoop()
    {
        if (CurrentRunState == null || string.IsNullOrWhiteSpace(CurrentRunState.uid))
            CreateNewRunState();

        return turnLoopService.InitializeRunLoop(CurrentRunState, GameConfigProvider.Current);
    }

    public bool AdvanceTurn()
    {
        return turnLoopService.AdvanceTurn(CurrentRunState, GameConfigProvider.Current);
    }

    public bool TryRecruitCandidate(string candidateUid)
    {
        return turnLoopService.TryRecruitCandidate(CurrentRunState, candidateUid);
    }

    public bool SetTraitLocked(string traitUid, bool isLocked)
    {
        return traitService.SetTraitLocked(CurrentRunState, traitUid, isLocked);
    }

    void InitializeServices()
    {
        statService = new StatService();
        modifierService = new ModifierService(statService);
        traitService = new TraitService(modifierService);
        ruleEffectApplier = new EffectApplier(modifierService, statService);
        missionExpeditionService = new MissionExpeditionService(
            statService,
            modifierService,
            traitService,
            () => GameConfigProvider.Current,
            (missionUid, trigger, context) => ExecuteMissionTriggerInternal(missionUid, trigger, context, null),
            (adventurerUid, trigger, context) => ExecuteAdventurerTriggerInternal(adventurerUid, trigger, context, null));
        turnLoopService = new TurnLoopService(statService, modifierService, missionExpeditionService, traitService);
    }

    RuleExecutionSummary ExecuteMissionTriggerInternal(string missionUid, string trigger, RuleContext context, IRuleEffectApplier effectApplier = null)
    {
        RuleContext effectiveContext = context?.Clone() ?? new RuleContext();
        effectiveContext.runState = CurrentRunState;
        effectiveContext.missionUid = missionUid ?? string.Empty;
        return RuleRunner.RunTraitsThenMission(CurrentRunState, missionUid, trigger, effectiveContext, effectApplier ?? ruleEffectApplier);
    }

    RuleExecutionSummary ExecuteAdventurerTriggerInternal(string adventurerUid, string trigger, RuleContext context, IRuleEffectApplier effectApplier = null)
    {
        RuleContext effectiveContext = context?.Clone() ?? new RuleContext();
        effectiveContext.runState = CurrentRunState;
        effectiveContext.adventurerUid = adventurerUid ?? string.Empty;
        return RuleRunner.RunTraitRulesByAdventurer(CurrentRunState, adventurerUid, trigger, effectiveContext, effectApplier ?? ruleEffectApplier);
    }
}
