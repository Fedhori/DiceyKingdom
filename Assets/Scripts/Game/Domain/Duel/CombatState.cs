using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Domain.Duel
{
    [Serializable]
    public sealed class CombatState
    {
        public int? maxPlayerAssignments;

        public List<string> playerAbilityIds = new();
        public List<string> opponentAbilityIds = new();

        public int totalPowerBonusPlayer;
        public int totalPowerBonusOpponent;
        public bool preventOutgoingDamageOnWinPlayer;
        public bool preventOutgoingDamageOnWinOpponent;

        public void EnsureInitialized()
        {
            if (playerAbilityIds == null)
            {
                playerAbilityIds = new List<string>();
                Debug.LogWarning("[CombatState] playerAbilityIds was null and has been auto-initialized.");
            }

            if (opponentAbilityIds == null)
            {
                opponentAbilityIds = new List<string>();
                Debug.LogWarning("[CombatState] opponentAbilityIds was null and has been auto-initialized.");
            }
        }
    }
}
