using System;
using Game.Application.Duel;
using Game.Domain.Duel;

namespace Game.Presentation.Duel
{
    public class DuelSelectionState
    {
        readonly DuelAbilityPlacementService placementService = new();

        public string SelectedAbilityInstanceId { get; private set; } = string.Empty;
        public int SelectedCombatIndex { get; private set; } = -1;

        public void ClearAll()
        {
            SelectedAbilityInstanceId = string.Empty;
            SelectedCombatIndex = -1;
        }

        public void ClearAbility()
        {
            SelectedAbilityInstanceId = string.Empty;
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

        public bool TrySelectAbility(DuelState duelState, string abilityInstanceId, out string failureMessage)
        {
            failureMessage = string.Empty;

            if (duelState == null)
            {
                failureMessage = "duel state is null.";
                return false;
            }

            duelState.EnsureInitialized();

            if (string.IsNullOrWhiteSpace(abilityInstanceId))
            {
                failureMessage = "abilityInstanceId is empty.";
                return false;
            }

            if (!duelState.abilitiesById.ContainsKey(abilityInstanceId))
            {
                failureMessage = $"abilityInstanceId({abilityInstanceId}) does not exist.";
                return false;
            }

            if (!placementService.TryFindAbilityLocation(
                    duelState,
                    abilityInstanceId,
                    DuelSide.Player,
                    out DuelAbilityLocation location,
                    out failureMessage))
            {
                return false;
            }

            SelectedAbilityInstanceId = abilityInstanceId;
            if (location.isCombat)
            {
                SelectedCombatIndex = location.combatIndex;
            }

            return true;
        }

        public bool TryToggleAttackSelection(
            DuelState duelState,
            DuelPhaseRunner phaseRunner,
            DuelUiQueryService uiQueryService,
            string abilityInstanceId,
            out string failureMessage)
        {
            failureMessage = string.Empty;

            if (duelState == null || phaseRunner == null || uiQueryService == null)
            {
                return false;
            }

            if (phaseRunner.currentPhase != DuelPhase.PlayerSetup)
            {
                return false;
            }

            duelState.EnsureInitialized();

            if (string.IsNullOrWhiteSpace(abilityInstanceId))
            {
                return false;
            }

            if (!uiQueryService.IsAttackDeployable(duelState, abilityInstanceId))
            {
                return false;
            }

            if (!placementService.TryFindAbilityLocation(
                    duelState,
                    abilityInstanceId,
                    DuelSide.Player,
                    out DuelAbilityLocation location,
                    out _))
            {
                return false;
            }

            SelectedAbilityInstanceId = string.Equals(SelectedAbilityInstanceId, abilityInstanceId, StringComparison.Ordinal)
                ? string.Empty
                : abilityInstanceId;

            if (!string.IsNullOrWhiteSpace(SelectedAbilityInstanceId) &&
                location.isCombat)
            {
                SelectedCombatIndex = location.combatIndex;
            }

            return true;
        }

        public bool TryMovePlayerAbilityToCombat(
            DuelState duelState,
            DuelPhaseRunner phaseRunner,
            string abilityInstanceId,
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
                abilityInstanceId,
                targetCombatIndex,
                DuelSide.Player,
                out failureMessage))
            {
                return false;
            }

            SelectedAbilityInstanceId = abilityInstanceId;
            SelectedCombatIndex = targetCombatIndex;
            return true;
        }

        public bool TryReturnPlayerAbilityToLoadout(
            DuelState duelState,
            DuelPhaseRunner phaseRunner,
            string abilityInstanceId,
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
                    abilityInstanceId,
                    DuelSide.Player,
                    out failureMessage))
            {
                return false;
            }

            SelectedAbilityInstanceId = string.Empty;
            SelectedCombatIndex = -1;
            return true;
        }
    }
}

