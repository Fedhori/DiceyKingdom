using Game.Domain.Battle;

namespace Game.Application.Battle.Effects
{
    public interface IBattleEffectHandler
    {
        BattleEffectOpCode opCode { get; }
        BattleEffectResult Apply(BattleState state, BattleEffectCommand command, BattleEffectContext context);
    }
}
