using Game.Domain.Duel;
using Game.Infrastructure.Data.Effects;

namespace Game.Application.Duel.Effects
{
    public interface IDuelEffectHandler
    {
        DuelEffectOpCode opCode { get; }
        DuelEffectResult Apply(DuelState state, DuelEffectCommand command, DuelEffectContext context);
    }
}
