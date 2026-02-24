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
            if (combats == null)
            {
                combats = new List<CombatState>();
                Debug.LogWarning("[DuelState] combats was null and has been auto-initialized.");
            }

            if (abilitiesById == null)
            {
                abilitiesById = new Dictionary<string, AbilityInstance>();
                Debug.LogWarning("[DuelState] abilitiesById was null and has been auto-initialized.");
            }

            if (loadoutAbilityIds == null)
            {
                loadoutAbilityIds = new List<string>();
                Debug.LogWarning("[DuelState] loadoutAbilityIds was null and has been auto-initialized.");
            }

            if (opponentLoadoutEntries == null)
            {
                opponentLoadoutEntries = new List<OpponentLoadoutEntry>();
                Debug.LogWarning("[DuelState] opponentLoadoutEntries was null and has been auto-initialized.");
            }

            for (int i = 0; i < combats.Count; i++)
            {
                if (combats[i] == null)
                {
                    combats[i] = new CombatState();
                    Debug.LogWarning($"[DuelState] combats[{i}] was null and has been replaced.");
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
