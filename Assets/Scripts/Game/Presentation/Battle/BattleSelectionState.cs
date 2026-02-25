using System;
using Game.Application.Duel;
using Game.Domain.Duel;
using Game.Infrastructure.Data;

namespace Game.Presentation.Battle
{
    public sealed class BattleSelectionState
    {
        enum AbilityLocationType
        {
            None = 0,
            Loadout = 1,
            Combat = 2
        }

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

            if (!TryFindPlayerAbilityLocation(duelState, abilityId, out AbilityLocationType locationType, out int combatIndex))
            {
                failureMessage = $"abilityId({abilityId}) is not in player controllable zones.";
                return false;
            }

            SelectedAbilityId = abilityId;
            if (locationType == AbilityLocationType.Combat)
            {
                SelectedCombatIndex = combatIndex;
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

            if (!TryFindPlayerAbilityLocation(duelState, abilityId, out AbilityLocationType locationType, out int combatIndex))
            {
                return false;
            }

            SelectedAbilityId = string.Equals(SelectedAbilityId, abilityId, StringComparison.Ordinal)
                ? string.Empty
                : abilityId;

            if (!string.IsNullOrWhiteSpace(SelectedAbilityId) &&
                locationType == AbilityLocationType.Combat)
            {
                SelectedCombatIndex = combatIndex;
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

            return TryMovePlayerAbilityToCombatInternal(
                duelState,
                abilityId,
                targetCombatIndex,
                out failureMessage);
        }

        bool TryMovePlayerAbilityToCombatInternal(
            DuelState duelState,
            string abilityId,
            int targetCombatIndex,
            out string failureMessage)
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

            if (targetCombatIndex < 0 ||
                duelState.combats == null ||
                targetCombatIndex >= duelState.combats.Count)
            {
                failureMessage = $"target combat({targetCombatIndex}) is out of range.";
                return false;
            }

            if (!duelState.abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) || ability == null)
            {
                failureMessage = $"ability({abilityId}) does not exist.";
                return false;
            }

            if (ability.abilityType != AbilityType.Attack)
            {
                failureMessage = $"only Attack type ability can be deployed to combat (current: {ability.abilityType}).";
                return false;
            }

            if (!TryFindPlayerAbilityLocation(duelState, abilityId, out AbilityLocationType sourceType, out int sourceCombatIndex))
            {
                failureMessage = $"ability({abilityId}) is not in player controllable zones.";
                return false;
            }

            CombatState targetCombat = duelState.combats[targetCombatIndex];
            if (targetCombat == null)
            {
                failureMessage = $"combat({targetCombatIndex}) is null.";
                return false;
            }

            targetCombat.EnsureInitialized();
            if (!targetCombat.playerAbilityIds.Contains(abilityId) &&
                targetCombat.maxPlayerAssignments.HasValue &&
                targetCombat.maxPlayerAssignments.Value > 0 &&
                targetCombat.playerAbilityIds.Count >= targetCombat.maxPlayerAssignments.Value)
            {
                failureMessage = $"target combat({targetCombatIndex}) maxPlayerAssignments exceeded.";
                return false;
            }

            if (sourceType == AbilityLocationType.Loadout)
            {
                duelState.loadoutAbilityIds.Remove(abilityId);
            }
            else
            {
                CombatState sourceCombat = duelState.combats[sourceCombatIndex];
                sourceCombat?.playerAbilityIds.Remove(abilityId);
            }

            if (!targetCombat.playerAbilityIds.Contains(abilityId))
            {
                targetCombat.playerAbilityIds.Add(abilityId);
            }

            SelectedAbilityId = abilityId;
            SelectedCombatIndex = targetCombatIndex;
            return true;
        }

        static bool TryFindPlayerAbilityLocation(
            DuelState duelState,
            string abilityId,
            out AbilityLocationType locationType,
            out int combatIndex)
        {
            locationType = AbilityLocationType.None;
            combatIndex = -1;

            if (duelState == null || string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            if (duelState.loadoutAbilityIds != null && duelState.loadoutAbilityIds.Contains(abilityId))
            {
                locationType = AbilityLocationType.Loadout;
                return true;
            }

            if (duelState.combats == null)
            {
                return false;
            }

            for (int i = 0; i < duelState.combats.Count; i++)
            {
                CombatState combat = duelState.combats[i];
                if (combat == null)
                {
                    continue;
                }

                combat.EnsureInitialized();
                if (!combat.playerAbilityIds.Contains(abilityId))
                {
                    continue;
                }

                locationType = AbilityLocationType.Combat;
                combatIndex = i;
                return true;
            }

            return false;
        }
    }
}
