using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Domain.Duel
{
    [Serializable]
    public sealed class DuelState
    {
        public int turnIndex;
        public int playerHealth;
        public int opponentHealth;
        public int honor;
        public bool isDuelEnded;
        public string enemyId = string.Empty;

        public List<CombatState> combats = new();
        public Dictionary<string, AbilityInstance> abilitiesById = new();
        public List<string> loadoutAbilityIds = new();
        public List<OpponentLoadoutEntry> opponentLoadoutEntries = new();

        public DuelState()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            var errors = new List<string>();
            if (combats == null)
            {
                errors.Add("combats is null.");
            }

            if (abilitiesById == null)
            {
                errors.Add("abilitiesById is null.");
            }

            if (loadoutAbilityIds == null)
            {
                errors.Add("loadoutAbilityIds is null.");
            }

            if (opponentLoadoutEntries == null)
            {
                errors.Add("opponentLoadoutEntries is null.");
            }

            if (errors.Count > 0)
            {
                string message = $"[DuelState] Invalid state: {string.Join(" ", errors)}";
                Debug.LogError(message);
                throw new InvalidOperationException(message);
            }

            for (int i = 0; i < combats.Count; i++)
            {
                if (combats[i] == null)
                {
                    string message = $"[DuelState] Invalid state: combats[{i}] is null.";
                    Debug.LogError(message);
                    throw new InvalidOperationException(message);
                }

                combats[i].EnsureInitialized();
            }
        }
    }

    [Serializable]
    public sealed class OpponentLoadoutEntry
    {
        public string abilityDefId = string.Empty;
        public int count;
    }
}
