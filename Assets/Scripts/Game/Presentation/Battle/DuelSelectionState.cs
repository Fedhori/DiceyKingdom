using System;
using Game.Application.Duel;
using Game.Domain.Duel;
using Game.Infrastructure.Data;

namespace Game.Presentation.Battle
{
    public class DuelSelectionState
    {
        readonly DuelAbilityPlacementService placementService = new();

        public string SelectedAbilityId { get; private set; } = string.Empty;
        public int SelectedCombatIndex { get; private set; } = -1;

        public void ClearAll()
        {
            SelectedAbilityId = string.Empty;
            SelectedCombatIndex = -1;
        }

        public void ClearAbility()
        {
            SelectedAbilityId = string.Empty;
        }

        public bool TrySetSelectedCombat(DuelState duelState, int combatIndex, out string failureMessage)
        {
            failureMessage = string.Empty;

            if (duelState == null)
            {
                failureMessage = "duel state is null.";
                return false;
            }

            duelState.EnsureInitialized();
            if (combatIndex < 0 || duelState.combats == null || combatIndex >= duelState.combats.Count)
            {
                failureMessage = $"combatIndex({combatIndex}) is out of range.";
                return false;
            }

            SelectedCombatIndex = combatIndex;
            return true;
        }

        public bool TrySelectAbility(DuelState duelState, string abilityId, out string failureMessage)
        {
            failureMessage = string.Empty;

            if (duelState == null)
            {
                failureMessage = "duel state is null.";
                return false;
            }

            duelState.EnsureInitialized();

            if (string.IsNullOrWhiteSpace(abilityId))
            {
                failureMessage = "abilityId is empty.";
                return false;
            }

            if (!duelState.abilitiesById.ContainsKey(abilityId))
            {
                failureMessage = $"abilityId({abilityId}) does not exist.";
                return false;
            }

            if (!placementService.TryFindAbilityLocation(
                    duelState,
                    abilityId,
                    DuelSide.Player,
                    out DuelAbilityLocation location,
                    out failureMessage))
            {
                return false;
            }

            SelectedAbilityId = abilityId;
            if (location.isCombat)
            {
                SelectedCombatIndex = location.combatIndex;
            }

            return true;
        }

        public bool TryToggleAttackSelection(
            DuelState duelState,
            DuelPhaseRunner phaseRunner,
            string abilityId,
            out string failureMessage)
        {
            failureMessage = string.Empty;

            if (duelState == null || phaseRunner == null)
            {
                return false;
            }

            if (phaseRunner.currentPhase != DuelPhase.PlayerSetup)
            {
                return false;
            }

            duelState.EnsureInitialized();

            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            if (!duelState.abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) || ability == null)
            {
                return false;
            }

            if (ability.abilityType != AbilityType.Attack)
            {
                return false;
            }

            if (ability.cooldownRemaining > 0)
            {
                return false;
            }

            if (!placementService.TryFindAbilityLocation(
                    duelState,
                    abilityId,
                    DuelSide.Player,
                    out DuelAbilityLocation location,
                    out _))
            {
                return false;
            }

            SelectedAbilityId = string.Equals(SelectedAbilityId, abilityId, StringComparison.Ordinal)
                ? string.Empty
                : abilityId;

            if (!string.IsNullOrWhiteSpace(SelectedAbilityId) &&
                location.isCombat)
            {
                SelectedCombatIndex = location.combatIndex;
            }

            return true;
        }

        public bool TryMovePlayerAbilityToCombat(
            DuelState duelState,
            DuelPhaseRunner phaseRunner,
            string abilityId,
            int targetCombatIndex,
            out string failureMessage)
        {
            failureMessage = string.Empty;

            if (phaseRunner == null)
            {
                failureMessage = "phase runner is null.";
                return false;
            }

            if (phaseRunner.currentPhase != DuelPhase.PlayerSetup)
            {
                failureMessage = $"current phase is {phaseRunner.currentPhase}, required phase is {DuelPhase.PlayerSetup}.";
                return false;
            }

            if (!placementService.TryMoveAbilityToCombat(
                duelState,
                abilityId,
                targetCombatIndex,
                DuelSide.Player,
                out failureMessage))
            {
                return false;
            }

            SelectedAbilityId = abilityId;
            SelectedCombatIndex = targetCombatIndex;
            return true;
        }

        public bool TryReturnPlayerAbilityToLoadout(
            DuelState duelState,
            DuelPhaseRunner phaseRunner,
            string abilityId,
            out string failureMessage)
        {
            failureMessage = string.Empty;

            if (phaseRunner == null)
            {
                failureMessage = "phase runner is null.";
                return false;
            }

            if (phaseRunner.currentPhase != DuelPhase.PlayerSetup)
            {
                failureMessage = $"current phase is {phaseRunner.currentPhase}, required phase is {DuelPhase.PlayerSetup}.";
                return false;
            }

            if (!placementService.TryReturnAbilityToLoadout(
                    duelState,
                    abilityId,
                    DuelSide.Player,
                    out failureMessage))
            {
                return false;
            }

            SelectedAbilityId = string.Empty;
            SelectedCombatIndex = -1;
            return true;
        }
    }
}
