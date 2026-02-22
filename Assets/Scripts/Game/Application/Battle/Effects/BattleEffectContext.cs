using Game.Domain.Battle;

namespace Game.Application.Battle.Effects
{
    public sealed class BattleEffectContext
    {
        public bool hasOutcome;
        public BattleOutcome outcome = BattleOutcome.Draw;
    }
}
