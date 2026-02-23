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
        public int attack;
        public int baseRoll;
        public int attackResult;

        public List<NumericModifier> attackModifiers = new();
        public List<NumericModifier> attackResultModifiers = new();
        public List<string> tags = new();

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

            if (attackModifiers == null)
            {
                attackModifiers = new List<NumericModifier>();
                Debug.LogWarning("[AbilityInstance] attackModifiers was null and has been auto-initialized.");
            }

            if (attackResultModifiers == null)
            {
                attackResultModifiers = new List<NumericModifier>();
                Debug.LogWarning("[AbilityInstance] attackResultModifiers was null and has been auto-initialized.");
            }

            if (tags == null)
            {
                tags = new List<string>();
                Debug.LogWarning("[AbilityInstance] tags was null and has been auto-initialized.");
            }
        }
    }
}
