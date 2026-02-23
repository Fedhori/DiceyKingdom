using System;

namespace Game.Domain.Modifiers
{
    [Serializable]
    public sealed class NumericModifier
    {
        public NumericModifierOperation operation = NumericModifierOperation.Add;
        public int value;
        public ModifierLayer layer = ModifierLayer.Duel;
        public string sourceId = string.Empty;
    }
}
