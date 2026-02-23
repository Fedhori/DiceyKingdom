using System;
using Game.Domain.Modifiers;

namespace Game.Application.Duel.Effects
{
    [Serializable]
    public sealed class DuelEffectCommand
    {
        public DuelEffectOpCode opCode;
        public string sourceId = string.Empty;
        public string actionId = string.Empty;

        public int clashIndex = -1;
        public int fromClashIndex = -1;
        public int toClashIndex = -1;

        public bool isPlayerSide = true;

        public NumericModifierOperation modifierOperation = NumericModifierOperation.Add;
        public ModifierLayer modifierLayer = ModifierLayer.Duel;
        public DuelModifierTarget modifierTarget = DuelModifierTarget.Attack;
        public int amount;
    }
}
