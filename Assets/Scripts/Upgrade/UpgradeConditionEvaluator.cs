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
                return target.DamageMultiplier > 0f;
            case UpgradeConditionKind.HasAttackSpeed:
                return target.AttackSpeed > 0f;
            case UpgradeConditionKind.HasProjectile:
                return !string.IsNullOrEmpty(target.ProjectileKey);
            default:
                return false;
        }
    }
}
