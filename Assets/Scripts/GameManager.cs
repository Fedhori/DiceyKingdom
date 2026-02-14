using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public RunState CurrentRunState { get; private set; } = new();
    StatService statService;
    ModifierService modifierService;
    IRuleEffectApplier ruleEffectApplier;
    MissionExpeditionService missionExpeditionService;
    TurnLoopService turnLoopService;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureServices();
    }

    void EnsureServices()
    {
        statService ??= new StatService();
        modifierService ??= new ModifierService(statService);
        ruleEffectApplier ??= new EffectApplier(modifierService, statService);
        missionExpeditionService ??= new MissionExpeditionService(
            statService,
            modifierService,
            (missionUid, trigger, context) => ExecuteMissionTriggerInternal(missionUid, trigger, context, null),
            (adventurerUid, trigger, context) => ExecuteAdventurerTriggerInternal(adventurerUid, trigger, context, null));
        turnLoopService ??= new TurnLoopService(statService, modifierService, missionExpeditionService);
    }

    public RunState CreateNewRunState()
    {
        EnsureServices();
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
        EnsureServices();
        CurrentRunState = runState ?? new RunState();
        statService.ClearCache();
    }

    public string ExportRunStateJson(bool prettyPrint = false)
    {
        EnsureServices();
        return JsonUtility.ToJson(CurrentRunState ?? new RunState(), prettyPrint);
    }

    public bool TryImportRunStateJson(string json)
    {
        EnsureServices();
        if (string.IsNullOrWhiteSpace(json))
            return false;

        RunState parsed = JsonUtility.FromJson<RunState>(json);
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
        EnsureServices();
        return statService.GetStat(CurrentRunState, adventurerUid, statId);
    }

    public void MarkAdventurerStatDirty(string adventurerUid)
    {
        EnsureServices();
        statService.MarkDirty(adventurerUid);
    }

    public void AddOrMergeModifier(ModifierInstance modifier)
    {
        EnsureServices();
        modifierService.AddOrMergeModifier(CurrentRunState, modifier);
    }

    public int RemoveMissionLayerModifiers(string missionUid)
    {
        EnsureServices();
        return modifierService.RemoveMissionLayerModifiers(CurrentRunState, missionUid);
    }

    public bool TryAssignAdventurerToMission(string adventurerUid, string missionUid)
    {
        EnsureServices();
        return missionExpeditionService.TryAssignAdventurerToMission(CurrentRunState, adventurerUid, missionUid);
    }

    public bool TryUnassignAdventurer(string adventurerUid)
    {
        EnsureServices();
        return missionExpeditionService.TryUnassignAdventurer(CurrentRunState, adventurerUid);
    }

    public AbilityTestResolveResult ResolveMissionAbilityTestOnce(string missionUid)
    {
        EnsureServices();
        return missionExpeditionService.ResolveAbilityTestOnce(CurrentRunState, missionUid);
    }

    public bool FailMissionExpedition(string missionUid)
    {
        EnsureServices();
        return missionExpeditionService.FailExpedition(CurrentRunState, missionUid);
    }

    public int AdvanceMissionDeadlinesAndRemoveFailedMissions()
    {
        EnsureServices();
        return missionExpeditionService.AdvanceMissionDeadlines(CurrentRunState);
    }

    public bool InitializeRunLoop()
    {
        EnsureServices();
        if (CurrentRunState == null || string.IsNullOrWhiteSpace(CurrentRunState.uid))
            CreateNewRunState();

        return turnLoopService.InitializeRunLoop(CurrentRunState, GameConfigProvider.Current);
    }

    public bool AdvanceTurn()
    {
        EnsureServices();
        return turnLoopService.AdvanceTurn(CurrentRunState, GameConfigProvider.Current);
    }

    public bool TryRecruitCandidate(string candidateUid)
    {
        EnsureServices();
        return turnLoopService.TryRecruitCandidate(CurrentRunState, candidateUid);
    }

    RuleExecutionSummary ExecuteMissionTriggerInternal(string missionUid, string trigger, RuleContext context, IRuleEffectApplier effectApplier = null)
    {
        EnsureServices();
        RuleContext effectiveContext = context?.Clone() ?? new RuleContext();
        effectiveContext.runState = CurrentRunState;
        effectiveContext.missionUid = missionUid ?? string.Empty;
        return RuleRunner.RunTraitsThenMission(CurrentRunState, missionUid, trigger, effectiveContext, effectApplier ?? ruleEffectApplier);
    }

    RuleExecutionSummary ExecuteAdventurerTriggerInternal(string adventurerUid, string trigger, RuleContext context, IRuleEffectApplier effectApplier = null)
    {
        EnsureServices();
        RuleContext effectiveContext = context?.Clone() ?? new RuleContext();
        effectiveContext.runState = CurrentRunState;
        effectiveContext.adventurerUid = adventurerUid ?? string.Empty;
        return RuleRunner.RunTraitRulesByAdventurer(CurrentRunState, adventurerUid, trigger, effectiveContext, effectApplier ?? ruleEffectApplier);
    }
}
