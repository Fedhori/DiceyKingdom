using System;
using Game.Domain.Modifiers;

namespace Game.Infrastructure.Data.Effects
{
    [Serializable]
    public sealed class DuelEffectCommand
    {
        public DuelEffectOpCode opCode;
        public string sourceId = string.Empty;
        public string abilityId = string.Empty;

        public int combatIndex = -1;
        public int fromCombatIndex = -1;
        public int toCombatIndex = -1;

        public bool isPlayerSide = true;

        public NumericModifierOperation modifierOperation = NumericModifierOperation.Add;
        public ModifierLayer modifierLayer = ModifierLayer.Duel;
        public DuelModifierTarget modifierTarget = DuelModifierTarget.Power;
        public int amount;
    }
}
