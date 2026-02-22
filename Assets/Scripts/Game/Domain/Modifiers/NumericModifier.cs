using System;

namespace Game.Domain.Modifiers
{
    [Serializable]
    public sealed class NumericModifier
    {
        public NumericModifierOperation operation = NumericModifierOperation.Add;
        public int value;
        public ModifierLayer layer = ModifierLayer.Battle;
        public string sourceId = string.Empty;
    }
}
