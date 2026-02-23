using Game.Domain.Duel;

namespace Game.Application.Duel.Effects
{
    public sealed class DuelEffectContext
    {
        public bool hasOutcome;
        public DuelOutcome outcome = DuelOutcome.Draw;
    }
}
