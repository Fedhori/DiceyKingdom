namespace Game.Application.Duel.Effects
{
    public enum DuelEffectOpCode
    {
        ModifyAttackResult = 0,
        MoveAction = 1,
        MoveOpponentAction = 2,
        ModifyTotalAttack = 3,
        TransformOutcome = 4,
        ModifyHealth = 5,
        AddAttackModifier = 6
    }
}
