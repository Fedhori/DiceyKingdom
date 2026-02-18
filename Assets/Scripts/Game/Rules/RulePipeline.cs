using System;
using System.Collections.Generic;

public interface IRuleEffectApplier
{
    void ApplyEffect(EffectDef effect, RuleEffectApplyContext context);
}




public sealed class NoopRuleEffectApplier : IRuleEffectApplier
{
    public static readonly NoopRuleEffectApplier Shared = new();

    NoopRuleEffectApplier()
    {
    }

    public void ApplyEffect(EffectDef effect, RuleEffectApplyContext context)
    {
    }
}




public static class RulePipeline
{
    public static RuleExecutionSummary Execute(
        IReadOnlyList<RuleDef> rules,
        string trigger,
        RuleContext context,
        RuleSourceType sourceType,
        string sourceUid,
        string missionUid,
        string ownerAdventurerUid,
        IRuleEffectApplier effectApplier = null)
    {
        var summary = new RuleExecutionSummary();
        if (rules == null || rules.Count == 0 || string.IsNullOrWhiteSpace(trigger))
            return summary;

        effectApplier ??= NoopRuleEffectApplier.Shared;

        for (int ruleIndex = 0; ruleIndex < rules.Count; ruleIndex++)
        {
            RuleDef rule = rules[ruleIndex];
            if (rule == null)
                continue;

            if (!string.Equals(rule.trigger, trigger, StringComparison.Ordinal))
                continue;

            summary.matchedRuleCount++;

            if (!RuleConditionEvaluator.Evaluate(rule.condition, context))
                continue;

            summary.executedRuleCount++;

            if (rule.effects == null || rule.effects.Count == 0)
                continue;

            for (int effectIndex = 0; effectIndex < rule.effects.Count; effectIndex++)
            {
                EffectDef effect = rule.effects[effectIndex];
                if (effect == null)
                    continue;

                var applyContext = new RuleEffectApplyContext
                {
                    runState = context?.runState,
                    ruleContext = context,
                    sourceType = sourceType,
                    sourceUid = sourceUid ?? string.Empty,
                    missionUid = missionUid ?? string.Empty,
                    ownerAdventurerUid = ownerAdventurerUid ?? string.Empty,
                    ruleIndex = ruleIndex,
                    effectIndex = effectIndex
                };

                effectApplier.ApplyEffect(effect, applyContext);
                summary.appliedEffectCount++;
            }
        }

        return summary;
    }
}

