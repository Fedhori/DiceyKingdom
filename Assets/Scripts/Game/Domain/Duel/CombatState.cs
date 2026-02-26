using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Domain.Duel
{
    [Serializable]
    public sealed class CombatState
    {
        public int? maxPlayerAssignments;
        public int? maxOpponentAssignments;

        public List<string> playerAbilityIds = new();
        public List<string> opponentAbilityIds = new();

        public int totalPowerBonusPlayer;
        public int totalPowerBonusOpponent;
        public bool preventOutgoingDamageOnWinPlayer;
        public bool preventOutgoingDamageOnWinOpponent;

        public void EnsureInitialized()
        {
            var errors = new List<string>();
            if (playerAbilityIds == null)
            {
                errors.Add("playerAbilityIds is null.");
            }

            if (opponentAbilityIds == null)
            {
                errors.Add("opponentAbilityIds is null.");
            }

            if (errors.Count == 0)
            {
                return;
            }

            string message = $"[CombatState] Invalid state: {string.Join(" ", errors)}";
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }
    }
}
