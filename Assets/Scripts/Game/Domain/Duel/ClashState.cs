using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Domain.Duel
{
    [Serializable]
    public sealed class ClashState
    {
        public string clashId = string.Empty;
        public int? slotLimit;

        public List<string> playerActionIds = new();
        public List<string> opponentActionIds = new();

        public int totalAttackBonusPlayer;
        public int totalAttackBonusOpponent;

        public void EnsureInitialized()
        {
            if (playerActionIds == null)
            {
                playerActionIds = new List<string>();
                Debug.LogWarning("[ClashState] playerActionIds was null and has been auto-initialized.");
            }

            if (opponentActionIds == null)
            {
                opponentActionIds = new List<string>();
                Debug.LogWarning("[ClashState] opponentActionIds was null and has been auto-initialized.");
            }
        }
    }
}
