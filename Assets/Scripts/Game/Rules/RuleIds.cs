/// <summary>
/// Static helper class for rule trigger related operations.
/// </summary>
public static class RuleTriggerIds
{
    public const string OnAbilityValueCalculation = "onAbilityValueCalculation";
    public const string OnExpeditionResolved = "onExpeditionResolved";
    public const string OnMissionFailed = "onMissionFailed";
    public const string OnTurnSettlement = "onTurnSettlement";
    public const string OnHpChanged = "onHpChanged";
}

/// <summary>
/// Static helper class for rule condition related operations.
/// </summary>
public static class RuleConditionIds
{
    public const string Always = "always";
    public const string HpBelowMax = "hpBelowMax";
    public const string HpDeltaNegative = "hpDeltaNegative";
    public const string ExpeditionSucceeded = "expeditionSucceeded";
    public const string ExpeditionFailed = "expeditionFailed";
}

public enum RuleSourceType
{
    None = 0,
    Trait = 1,
    Mission = 2
}

