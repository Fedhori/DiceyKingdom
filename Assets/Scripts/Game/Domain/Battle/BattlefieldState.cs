using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Domain.Battle
{
    [Serializable]
    public sealed class BattlefieldState
    {
        public string battlefieldId = string.Empty;
        public int? slotLimit;

        public List<string> playerTroopIds = new();
        public List<string> enemyTroopIds = new();

        public int totalAttackBonusPlayer;
        public int totalAttackBonusEnemy;

        public void EnsureInitialized()
        {
            if (playerTroopIds == null)
            {
                playerTroopIds = new List<string>();
                Debug.LogWarning("[BattlefieldState] playerTroopIds was null and has been auto-initialized.");
            }

            if (enemyTroopIds == null)
            {
                enemyTroopIds = new List<string>();
                Debug.LogWarning("[BattlefieldState] enemyTroopIds was null and has been auto-initialized.");
            }
        }
    }
}
