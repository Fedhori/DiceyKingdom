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
        public int cooldownTurns;
        public int cooldownRemaining;
        public int power;
        public int baseRoll;
        public int powerResult;

        public List<NumericModifier> powerModifiers = new();
        public List<NumericModifier> powerResultModifiers = new();

        public void EnsureInitialized()
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                instanceId = Guid.NewGuid().ToString("N");
                Debug.LogWarning("[AbilityInstance] instanceId was empty and has been regenerated.");
            }

            if (cooldownTurns < 0)
            {
                cooldownTurns = 0;
                Debug.LogWarning("[AbilityInstance] cooldownTurns was negative and has been clamped to 0.");
            }

            if (cooldownRemaining < 0)
            {
                cooldownRemaining = 0;
                Debug.LogWarning("[AbilityInstance] cooldownRemaining was negative and has been clamped to 0.");
            }

            if (powerModifiers == null)
            {
                powerModifiers = new List<NumericModifier>();
                Debug.LogWarning("[AbilityInstance] powerModifiers was null and has been auto-initialized.");
            }

            if (powerResultModifiers == null)
            {
                powerResultModifiers = new List<NumericModifier>();
                Debug.LogWarning("[AbilityInstance] powerResultModifiers was null and has been auto-initialized.");
            }

        }
    }
}
