using System;

[Serializable]
/// <summary>
/// Carries rule execution context data.
/// </summary>
public sealed class RuleContext
{
    public RunState runState;
    public string missionUid = string.Empty;
    public string adventurerUid = string.Empty;
    public int hpDelta;
    public bool? expeditionSucceeded;

    public RuleContext Clone()
    {
        return new RuleContext
        {
            runState = runState,
            missionUid = missionUid,
            adventurerUid = adventurerUid,
            hpDelta = hpDelta,
            expeditionSucceeded = expeditionSucceeded
        };
    }
}

/// <summary>
/// Carries rule effect apply execution context data.
/// </summary>
public sealed class RuleEffectApplyContext
{
    public RunState runState;
    public RuleContext ruleContext;
    public RuleSourceType sourceType;
    public string sourceUid = string.Empty;
    public string missionUid = string.Empty;
    public string ownerAdventurerUid = string.Empty;
    public int ruleIndex;
    public int effectIndex;
}

/// <summary>
/// Core class that defines rule execution summary responsibilities.
/// </summary>
public sealed class RuleExecutionSummary
{
    public int matchedRuleCount;
    public int executedRuleCount;
    public int appliedEffectCount;

    public void Add(RuleExecutionSummary other)
    {
        if (other == null)
            return;

        matchedRuleCount += other.matchedRuleCount;
        executedRuleCount += other.executedRuleCount;
        appliedEffectCount += other.appliedEffectCount;
    }
}

