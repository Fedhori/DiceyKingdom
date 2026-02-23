using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Domain.Duel
{
    [Serializable]
    public sealed class DuelState
    {
        public const int defaultClashCount = 3;

        public int turnIndex;
        public int playerHealth;
        public int opponentHealth;
        public int honor;
        public bool isDuelEnded;

        public List<ClashState> clashes = new();
        public Dictionary<string, AbilityInstance> abilitiesById = new();
        public List<string> abilityHolderAbilityIds = new();
        public List<IntentEntry> intent = new();

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

            if (abilityHolderAbilityIds == null)
            {
                abilityHolderAbilityIds = new List<string>();
                Debug.LogWarning("[DuelState] abilityHolderAbilityIds was null and has been auto-initialized.");
            }

            if (intent == null)
            {
                intent = new List<IntentEntry>();
                Debug.LogWarning("[DuelState] intent was null and has been auto-initialized.");
            }

            if (clashes.Count == 0)
            {
                for (int i = 0; i < defaultClashCount; i++)
                {
                    clashes.Add(new ClashState());
                }
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
    public sealed class IntentEntry
    {
        public int clashIndex;
        public string abilityDefId = string.Empty;
        public int count;
    }
}
