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
        public string encounterId = string.Empty;
        public string currentPatternId = string.Empty;

        public List<ClashState> clashes = new();
        public Dictionary<string, AbilityInstance> abilitiesById = new();
        public List<string> loadoutAbilityIds = new();
        public List<OpponentClashLoadoutEntry> opponentClashLoadoutEntries = new();

        public DuelState()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (clashes == null)
            {
                clashes = new List<ClashState>();
                Debug.LogWarning("[DuelState] clashes was null and has been auto-initialized.");
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

            if (opponentClashLoadoutEntries == null)
            {
                opponentClashLoadoutEntries = new List<OpponentClashLoadoutEntry>();
                Debug.LogWarning("[DuelState] opponentClashLoadoutEntries was null and has been auto-initialized.");
            }

            for (int i = 0; i < clashes.Count; i++)
            {
                if (clashes[i] == null)
                {
                    clashes[i] = new ClashState();
                    Debug.LogWarning($"[DuelState] clashes[{i}] was null and has been replaced.");
                }

                clashes[i].EnsureInitialized();
            }
        }
    }

    [Serializable]
    public sealed class OpponentClashLoadoutEntry
    {
        public int clashIndex;
        public string abilityDefId = string.Empty;
        public int count;
    }
}
