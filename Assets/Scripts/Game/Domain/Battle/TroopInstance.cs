using System;
using System.Collections.Generic;
using Game.Domain.Modifiers;
using UnityEngine;

namespace Game.Domain.Battle
{
    [Serializable]
    public sealed class TroopInstance
    {
        public string instanceId = Guid.NewGuid().ToString("N");
        public string troopDefId = string.Empty;

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
                Debug.LogWarning("[TroopInstance] instanceId was empty and has been regenerated.");
            }

            if (attackModifiers == null)
            {
                attackModifiers = new List<NumericModifier>();
                Debug.LogWarning("[TroopInstance] attackModifiers was null and has been auto-initialized.");
            }

            if (attackResultModifiers == null)
            {
                attackResultModifiers = new List<NumericModifier>();
                Debug.LogWarning("[TroopInstance] attackResultModifiers was null and has been auto-initialized.");
            }

            if (tags == null)
            {
                tags = new List<string>();
                Debug.LogWarning("[TroopInstance] tags was null and has been auto-initialized.");
            }
        }
    }
}
