using System.Collections.Generic;
using UnityEngine;

namespace Game.Domain.Modifiers
{
    public static class NumericModifierCalculator
    {
        public static int Apply(
            int baseValue,
            IReadOnlyList<NumericModifier> modifiers,
            int minValue = 1,
            string logContext = "NumericModifierCalculator")
        {
            int addTotal = 0;
            int percentBonusTotal = 0;

            if (modifiers != null)
            {
                for (int i = 0; i < modifiers.Count; i++)
                {
                    NumericModifier modifier = modifiers[i];
                    if (modifier == null)
                    {
                        Debug.LogWarning($"[{logContext}] modifiers[{i}] was null and has been ignored.");
                        continue;
                    }

                    switch (modifier.operation)
                    {
                        case NumericModifierOperation.Add:
                            addTotal += modifier.value;
                            break;
                        case NumericModifierOperation.PercentBonus:
                            percentBonusTotal += modifier.value;
                            break;
                        default:
                            Debug.LogWarning(
                                $"[{logContext}] Unknown NumericModifierOperation({modifier.operation}) was ignored. sourceId={modifier.sourceId}");
                            break;
                    }
                }
            }

            float rawValue = (baseValue + addTotal) * (1f + (percentBonusTotal / 100f));
            int finalValue = Mathf.FloorToInt(rawValue);

            if (finalValue < minValue)
            {
                finalValue = minValue;
            }

            return finalValue;
        }

        public static int ClearByLayer(List<NumericModifier> modifiers, ModifierLayer layer)
        {
            if (modifiers == null)
            {
                return 0;
            }

            int removedCount = 0;

            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                NumericModifier modifier = modifiers[i];
                if (modifier == null || modifier.layer != layer)
                {
                    continue;
                }

                modifiers.RemoveAt(i);
                removedCount += 1;
            }

            return removedCount;
        }
    }
}
