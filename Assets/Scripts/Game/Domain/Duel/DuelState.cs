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
        public int focus;
        public int honor;
        public bool isDuelEnded;

        public List<ClashState> clashes = new();
        public Dictionary<string, ActionInstance> actionsById = new();
        public List<string> actionHolderActionIds = new();
        public List<OpponentIntentEntry> opponentIntent = new();

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

            if (actionsById == null)
            {
                actionsById = new Dictionary<string, ActionInstance>();
                Debug.LogWarning("[DuelState] actionsById was null and has been auto-initialized.");
            }

            if (actionHolderActionIds == null)
            {
                actionHolderActionIds = new List<string>();
                Debug.LogWarning("[DuelState] actionHolderActionIds was null and has been auto-initialized.");
            }

            if (opponentIntent == null)
            {
                opponentIntent = new List<OpponentIntentEntry>();
                Debug.LogWarning("[DuelState] opponentIntent was null and has been auto-initialized.");
            }

            int clashCountBeforePadding = clashes.Count;
            while (clashes.Count < defaultClashCount)
            {
                clashes.Add(new ClashState());
            }

            if (clashCountBeforePadding > 0 &&
                clashCountBeforePadding < defaultClashCount)
            {
                Debug.LogWarning(
                    $"[DuelState] clashes had {clashCountBeforePadding} entries and was padded to {defaultClashCount}.");
            }

            if (clashes.Count > defaultClashCount)
            {
                Debug.LogWarning(
                    $"[DuelState] clashes had more than {defaultClashCount} entries and was trimmed.");

                clashes.RemoveRange(
                    defaultClashCount,
                    clashes.Count - defaultClashCount);
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
    public sealed class OpponentIntentEntry
    {
        public int clashIndex;
        public string actionDefId = string.Empty;
        public int count;
    }
}
