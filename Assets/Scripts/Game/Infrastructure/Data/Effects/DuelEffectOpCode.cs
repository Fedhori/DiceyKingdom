namespace Game.Infrastructure.Data.Effects
{
    public enum DuelEffectOpCode
    {
        ModifyPowerResult = 0,
        MoveAbility = 1,
        MoveOpponentAbility = 2,
        ModifyTotalPower = 3,
        ModifyHealth = 4,
        AddPowerModifier = 5,
        PreventOutgoingDamageOnWin = 6
    }
}
