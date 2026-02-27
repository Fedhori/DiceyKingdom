using System;
using System.Collections.Generic;
using Game.Domain.Modifiers;
using Game.Infrastructure.Data;
using UnityEngine;

namespace Game.Domain.Duel
{
    [Serializable]
    public sealed class AbilityInstance
    {
        public string instanceId = Guid.NewGuid().ToString("N");
        public string abilityDefId = string.Empty;

        public AbilityType abilityType;
        public int cooldownTurns = 1;
        public int cooldownRemaining;
        public int power;
        public int baseRoll;
        public int powerResult;
        public int rollMinPercent;

        public List<NumericModifier> powerModifiers = new();
        public List<NumericModifier> powerResultModifiers = new();

        public void EnsureInitialized()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(instanceId))
            {
                errors.Add("instanceId is empty.");
            }

            int minimumCooldownTurns = AbilityDef.GetMinimumCooldownTurns(abilityType);
            if (cooldownTurns < minimumCooldownTurns)
            {
                errors.Add(
                    $"cooldownTurns({cooldownTurns}) must be greater than or equal to {minimumCooldownTurns} for type({abilityType}).");
            }

            if (cooldownRemaining < 0)
            {
                errors.Add($"cooldownRemaining({cooldownRemaining}) is negative.");
            }

            if (rollMinPercent < 0 || rollMinPercent > 100)
            {
                errors.Add($"rollMinPercent({rollMinPercent}) must be in range 0..100.");
            }

            if (powerModifiers == null)
            {
                errors.Add("powerModifiers is null.");
            }

            if (powerResultModifiers == null)
            {
                errors.Add("powerResultModifiers is null.");
            }

            if (errors.Count == 0)
            {
                return;
            }

            string message = $"[AbilityInstance] Invalid state: {string.Join(" ", errors)}";
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }
    }
}
