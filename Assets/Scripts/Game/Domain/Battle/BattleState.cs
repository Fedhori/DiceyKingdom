using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Domain.Battle
{
    [Serializable]
    public sealed class BattleState
    {
        public const int defaultBattlefieldCount = 3;

        public int turnIndex;
        public int playerMorale;
        public int enemyMorale;
        public int mana;

        public Dictionary<string, int> cooldowns = new();
        public List<BattlefieldState> battlefields = new();
        public Dictionary<string, TroopInstance> troopsById = new();
        public List<string> campTroopIds = new();
        public List<EnemyIntentEntry> enemyIntent = new();

        public BattleState()
        {
            EnsureInitialized();
        }

        public void EnsureInitialized()
        {
            if (cooldowns == null)
            {
                cooldowns = new Dictionary<string, int>();
                Debug.LogWarning("[BattleState] cooldowns was null and has been auto-initialized.");
            }

            if (battlefields == null)
            {
                battlefields = new List<BattlefieldState>();
                Debug.LogWarning("[BattleState] battlefields was null and has been auto-initialized.");
            }

            if (troopsById == null)
            {
                troopsById = new Dictionary<string, TroopInstance>();
                Debug.LogWarning("[BattleState] troopsById was null and has been auto-initialized.");
            }

            if (campTroopIds == null)
            {
                campTroopIds = new List<string>();
                Debug.LogWarning("[BattleState] campTroopIds was null and has been auto-initialized.");
            }

            if (enemyIntent == null)
            {
                enemyIntent = new List<EnemyIntentEntry>();
                Debug.LogWarning("[BattleState] enemyIntent was null and has been auto-initialized.");
            }

            int battlefieldCountBeforePadding = battlefields.Count;
            while (battlefields.Count < defaultBattlefieldCount)
            {
                battlefields.Add(new BattlefieldState());
            }

            if (battlefieldCountBeforePadding > 0 &&
                battlefieldCountBeforePadding < defaultBattlefieldCount)
            {
                Debug.LogWarning(
                    $"[BattleState] battlefields had {battlefieldCountBeforePadding} entries and was padded to {defaultBattlefieldCount}.");
            }

            if (battlefields.Count > defaultBattlefieldCount)
            {
                Debug.LogWarning(
                    $"[BattleState] battlefields had more than {defaultBattlefieldCount} entries and was trimmed.");

                battlefields.RemoveRange(
                    defaultBattlefieldCount,
                    battlefields.Count - defaultBattlefieldCount);
            }

            for (int i = 0; i < battlefields.Count; i++)
            {
                if (battlefields[i] == null)
                {
                    battlefields[i] = new BattlefieldState();
                    Debug.LogWarning($"[BattleState] battlefields[{i}] was null and has been replaced.");
                }

                battlefields[i].EnsureInitialized();
            }
        }
    }

    [Serializable]
    public sealed class EnemyIntentEntry
    {
        public int battlefieldIndex;
        public string troopDefId = string.Empty;
        public int count;
    }
}
