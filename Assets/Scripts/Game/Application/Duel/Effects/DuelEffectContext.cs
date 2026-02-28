using Game.Domain.Duel;

namespace Game.Application.Duel.Effects
{
    public sealed class DuelEffectContext
    {
        public bool hasOutcome;
        public DuelOutcome outcome = DuelOutcome.Draw;
        public bool hasHealthLost;
        public bool healthLostIsPlayerSide;
        public int healthLostAmount;
    }
}
