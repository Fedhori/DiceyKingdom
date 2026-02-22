using System;
using Game.Domain.Modifiers;

namespace Game.Application.Battle.Effects
{
    [Serializable]
    public sealed class BattleEffectCommand
    {
        public BattleEffectOpCode opCode;
        public string sourceId = string.Empty;
        public string troopId = string.Empty;

        public int battlefieldIndex = -1;
        public int fromBattlefieldIndex = -1;
        public int toBattlefieldIndex = -1;

        public bool isPlayerSide = true;

        public NumericModifierOperation modifierOperation = NumericModifierOperation.Add;
        public ModifierLayer modifierLayer = ModifierLayer.Battle;
        public int amount;

        public BattleOutcomeTransformKind transformKind = BattleOutcomeTransformKind.None;
    }
}
