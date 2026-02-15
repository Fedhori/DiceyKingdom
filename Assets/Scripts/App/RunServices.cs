using Newtonsoft.Json;




public sealed class RunServices : System.IDisposable
{
    public RunState CurrentRunState { get; private set; } = new();
    public GameSceneRefs SceneRefs { get; }
    public IReadOnlyObservableValue<int> Gold => gold;
    public IReadOnlyObservableValue<int> Stability => stability;
    public IReadOnlyObservableValue<int> StabilityMax => stabilityMax;
    public IReadOnlyObservableValue<int> Turn => turn;
    public IReadOnlyObservableValue<int> BarracksCapacity => barracksCapacity;
    public IReadOnlyObservableValue<int> CandidatesCount => candidatesCount;
    public IReadOnlyObservableValue<int> AdventurersCount => adventurersCount;
    public IReadOnlyObservableValue<int> MissionsCount => missionsCount;
    public IReadOnlyObservableValue<int> UiRevision => uiRevision;

    StatService statService;
    ModifierService modifierService;
    TraitService traitService;
    IRuleEffectApplier ruleEffectApplier;
    MissionExpeditionService missionExpeditionService;
    TurnLoopService turnLoopService;
    readonly ObservableValue<int> gold = new();
    readonly ObservableValue<int> stability = new();
    readonly ObservableValue<int> stabilityMax = new();
    readonly ObservableValue<int> turn = new();
    readonly ObservableValue<int> barracksCapacity = new();
    readonly ObservableValue<int> candidatesCount = new();
    readonly ObservableValue<int> adventurersCount = new();
    readonly ObservableValue<int> missionsCount = new();
    readonly ObservableValue<int> uiRevision = new();

    public RunServices(GameSceneRefs sceneRefs = null)
    {
        SceneRefs = sceneRefs;
        InitializeServices();
        SyncUiBindingsFromRunState();
    }

    public void Dispose()
    {
        statService?.ClearCache();
        ClearUiBindingListeners();
        CurrentRunState = new RunState();
        SyncUiBindingsFromRunState();
    }

    public void NotifyStatePossiblyChanged()
    {
        SyncUiBindingsFromRunState(forceRevision: true);
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
        SyncUiBindingsFromRunState(forceRevision: true);
        return CurrentRunState;
    }

    public void SetRunState(RunState runState)
    {
        CurrentRunState = runState ?? new RunState();
        statService.ClearCache();
        SyncUiBindingsFromRunState(forceRevision: true);
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
        SyncUiBindingsFromRunState(forceRevision: true);
        return true;
    }

    public RuleExecutionSummary RunMissionTrigger(string missionUid, string trigger, RuleContext context = null, IRuleEffectApplier effectApplier = null)
    {
        RuleExecutionSummary summary = ExecuteMissionTriggerInternal(missionUid, trigger, context, effectApplier);
        bool changed = summary != null && (summary.appliedEffectCount > 0 || summary.executedRuleCount > 0);
        SyncUiBindingsFromRunState(forceRevision: changed);
        return summary;
    }

    public RuleExecutionSummary RunAdventurerTrigger(string adventurerUid, string trigger, RuleContext context = null, IRuleEffectApplier effectApplier = null)
    {
        RuleExecutionSummary summary = ExecuteAdventurerTriggerInternal(adventurerUid, trigger, context, effectApplier);
        bool changed = summary != null && (summary.appliedEffectCount > 0 || summary.executedRuleCount > 0);
        SyncUiBindingsFromRunState(forceRevision: changed);
        return summary;
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
        if (modifier == null)
            return;

        modifierService.AddOrMergeModifier(CurrentRunState, modifier);
        SyncUiBindingsFromRunState(forceRevision: true);
    }

    public int RemoveMissionLayerModifiers(string missionUid)
    {
        int removed = modifierService.RemoveMissionLayerModifiers(CurrentRunState, missionUid);
        if (removed > 0)
            SyncUiBindingsFromRunState(forceRevision: true);
        return removed;
    }

    public bool TryAssignAdventurerToMission(string adventurerUid, string missionUid)
    {
        bool assigned = missionExpeditionService.TryAssignAdventurerToMission(CurrentRunState, adventurerUid, missionUid);
        if (assigned)
            SyncUiBindingsFromRunState(forceRevision: true);
        return assigned;
    }

    public bool TryUnassignAdventurer(string adventurerUid)
    {
        bool unassigned = missionExpeditionService.TryUnassignAdventurer(CurrentRunState, adventurerUid);
        if (unassigned)
            SyncUiBindingsFromRunState(forceRevision: true);
        return unassigned;
    }

    public AbilityTestResolveResult ResolveMissionAbilityTestOnce(string missionUid)
    {
        AbilityTestResolveResult result = missionExpeditionService.ResolveAbilityTestOnce(CurrentRunState, missionUid);
        if (result != null && result.outcome != AbilityTestResolveOutcome.Invalid)
            SyncUiBindingsFromRunState(forceRevision: true);
        return result;
    }

    public bool FailMissionExpedition(string missionUid)
    {
        bool failed = missionExpeditionService.FailExpedition(CurrentRunState, missionUid);
        if (failed)
            SyncUiBindingsFromRunState(forceRevision: true);
        return failed;
    }

    public int AdvanceMissionDeadlinesAndRemoveFailedMissions()
    {
        bool hadMissions = CurrentRunState?.missions != null && CurrentRunState.missions.Count > 0;
        int removed = missionExpeditionService.AdvanceMissionDeadlines(CurrentRunState);
        if (hadMissions)
            SyncUiBindingsFromRunState(forceRevision: true);
        return removed;
    }

    public bool InitializeRunLoop()
    {
        if (CurrentRunState == null || string.IsNullOrWhiteSpace(CurrentRunState.uid))
            CreateNewRunState();

        bool initialized = turnLoopService.InitializeRunLoop(CurrentRunState, GameConfigProvider.Current);
        if (initialized)
            SyncUiBindingsFromRunState(forceRevision: true);
        return initialized;
    }

    public bool AdvanceTurn()
    {
        bool advanced = turnLoopService.AdvanceTurn(CurrentRunState, GameConfigProvider.Current);
        if (advanced)
            SyncUiBindingsFromRunState(forceRevision: true);
        return advanced;
    }

    public bool TryRecruitCandidate(string candidateUid)
    {
        bool recruited = turnLoopService.TryRecruitCandidate(CurrentRunState, candidateUid);
        if (recruited)
            SyncUiBindingsFromRunState(forceRevision: true);
        return recruited;
    }

    public bool SetTraitLocked(string traitUid, bool isLocked)
    {
        bool locked = traitService.SetTraitLocked(CurrentRunState, traitUid, isLocked);
        if (locked)
            SyncUiBindingsFromRunState(forceRevision: true);
        return locked;
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

    void SyncUiBindingsFromRunState(bool forceRevision = false)
    {
        RunState state = CurrentRunState ?? new RunState();
        bool changed =
            SetObservableValue(gold, state.gold) |
            SetObservableValue(stability, state.stability) |
            SetObservableValue(stabilityMax, state.stabilityMax) |
            SetObservableValue(turn, state.turn) |
            SetObservableValue(barracksCapacity, state.barracksCapacity) |
            SetObservableValue(candidatesCount, state.candidates?.Count ?? 0) |
            SetObservableValue(adventurersCount, state.adventurers?.Count ?? 0) |
            SetObservableValue(missionsCount, state.missions?.Count ?? 0);

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
        gold.ClearListeners();
        stability.ClearListeners();
        stabilityMax.ClearListeners();
        turn.ClearListeners();
        barracksCapacity.ClearListeners();
        candidatesCount.ClearListeners();
        adventurersCount.ClearListeners();
        missionsCount.ClearListeners();
        uiRevision.ClearListeners();
    }
}

