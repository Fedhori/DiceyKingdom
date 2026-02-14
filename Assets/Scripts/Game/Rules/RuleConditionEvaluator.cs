using System;
using System.Collections.Generic;

public static class RuleConditionEvaluator
{
    public static bool Evaluate(ConditionDef condition, RuleContext context)
    {
        if (condition == null)
            return false;

        string conditionId = condition.conditionId ?? string.Empty;
        switch (conditionId)
        {
            case RuleConditionIds.Always:
                return true;
            case RuleConditionIds.HpDeltaNegative:
                return context != null && context.hpDelta < 0;
            case RuleConditionIds.ExpeditionSucceeded:
                return context != null && context.expeditionSucceeded == true;
            case RuleConditionIds.ExpeditionFailed:
                return context != null && context.expeditionSucceeded == false;
            case RuleConditionIds.HpBelowMax:
                return IsHpBelowMax(context);
            default:
                return false;
        }
    }

    static bool IsHpBelowMax(RuleContext context)
    {
        if (context == null || context.runState == null || string.IsNullOrWhiteSpace(context.adventurerUid))
            return false;

        if (!TryGetAdventurer(context.runState.adventurers, context.adventurerUid, out AdventurerInstance adventurer))
            return false;

        return adventurer.hp < adventurer.maxHp;
    }

    static bool TryGetAdventurer(IReadOnlyList<AdventurerInstance> adventurers, string uid, out AdventurerInstance adventurer)
    {
        adventurer = null;
        if (adventurers == null || string.IsNullOrWhiteSpace(uid))
            return false;

        for (int i = 0; i < adventurers.Count; i++)
        {
            AdventurerInstance entry = adventurers[i];
            if (entry == null)
                continue;

            if (string.Equals(entry.uid, uid, StringComparison.Ordinal))
            {
                adventurer = entry;
                return true;
            }
        }

        return false;
    }
}
