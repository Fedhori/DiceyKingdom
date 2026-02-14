using UnityEngine;

public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public RunState CurrentRunState { get; private set; } = new();
    StatService statService;
    ModifierService modifierService;
    IRuleEffectApplier ruleEffectApplier;

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
        EnsureServices();
        RuleContext effectiveContext = context?.Clone() ?? new RuleContext();
        effectiveContext.runState = CurrentRunState;
        effectiveContext.missionUid = missionUid ?? string.Empty;
        return RuleRunner.RunTraitsThenMission(CurrentRunState, missionUid, trigger, effectiveContext, effectApplier ?? ruleEffectApplier);
    }

    public RuleExecutionSummary RunAdventurerTrigger(string adventurerUid, string trigger, RuleContext context = null, IRuleEffectApplier effectApplier = null)
    {
        EnsureServices();
        RuleContext effectiveContext = context?.Clone() ?? new RuleContext();
        effectiveContext.runState = CurrentRunState;
        effectiveContext.adventurerUid = adventurerUid ?? string.Empty;
        return RuleRunner.RunTraitRulesByAdventurer(CurrentRunState, adventurerUid, trigger, effectiveContext, effectApplier ?? ruleEffectApplier);
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
}
