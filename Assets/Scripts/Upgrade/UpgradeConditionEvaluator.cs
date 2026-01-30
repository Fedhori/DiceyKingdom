using Data;

public static class UpgradeConditionEvaluator
{
    public static bool IsSatisfied(UpgradeConditionDto condition, ItemInstance target)
    {
        if (condition == null || target == null)
            return false;

        switch (condition.conditionKind)
        {
            case UpgradeConditionKind.HasDamageMultiplier:
                return target.IsWeapon();
            case UpgradeConditionKind.HasAttackSpeed:
                return target.AttackSpeed > 0f;
            case UpgradeConditionKind.HasProjectile:
                return !string.IsNullOrEmpty(target.ProjectileKey);
            case UpgradeConditionKind.HasNoHoming:
                return !string.IsNullOrEmpty(target.ProjectileKey) && !target.ProjectileIsHoming;
            case UpgradeConditionKind.HasNoBounce:
                return target.ProjectileHitBehavior != ProjectileHitBehavior.Bounce;
            case UpgradeConditionKind.HasItemRarity:
                return target.Rarity == condition.rarity;
            case UpgradeConditionKind.HasTriggerRule:
                return HasTriggerRule(target, condition.triggerType);
            default:
                return false;
        }
    }

    static bool HasTriggerRule(ItemInstance target, ItemTriggerType trigger)
    {
        if (target == null || trigger == ItemTriggerType.Unknown)
            return false;

        var rules = target.Rules;
        if (rules == null || rules.Count == 0)
            return false;

        for (int i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (rule == null)
                continue;

            if (rule.triggerType == trigger)
                return true;
        }

        return false;
    }
}
