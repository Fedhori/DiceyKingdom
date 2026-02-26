using System;
using System.Collections.Generic;
using Game.Domain.Duel;
using Game.Infrastructure.Data;

namespace Game.Application.Duel
{
    public enum DuelSide
    {
        Player = 0,
        Opponent = 1
    }

    public enum DuelAbilityLocationType
    {
        None = 0,
        Loadout = 1,
        Combat = 2
    }

    public readonly struct DuelAbilityLocation
    {
        public DuelAbilityLocationType locationType { get; }
        public int combatIndex { get; }

        public bool isCombat => locationType == DuelAbilityLocationType.Combat;

        public DuelAbilityLocation(DuelAbilityLocationType locationType, int combatIndex)
        {
            this.locationType = locationType;
            this.combatIndex = combatIndex;
        }
    }

    public readonly struct DuelAutoDeployResult
    {
        public int deployedCount { get; }
        public int skippedCount { get; }

        public DuelAutoDeployResult(int deployedCount, int skippedCount)
        {
            this.deployedCount = deployedCount;
            this.skippedCount = skippedCount;
        }
    }

    public sealed class DuelAbilityPlacementService
    {
        public const int MaxLoadoutAbilityCount = 12;

        public bool TryMoveAbilityToCombat(
            DuelState state,
            string abilityId,
            int targetCombatIndex,
            DuelSide side,
            out string failureMessage)
        {
            failureMessage = string.Empty;

            if (!TryValidateMoveRequest(state, abilityId, targetCombatIndex, out AbilityInstance ability, out failureMessage))
            {
                return false;
            }

            if (!TryFindAbilityLocation(state, abilityId, side, out DuelAbilityLocation sourceLocation, out failureMessage))
            {
                return false;
            }

            CombatState targetCombat = state.combats[targetCombatIndex];
            if (targetCombat == null)
            {
                failureMessage = $"target combat({targetCombatIndex}) is null.";
                return false;
            }

            targetCombat.EnsureInitialized();

            List<string> targetList = GetCombatAbilityIds(targetCombat, side);
            if (!targetList.Contains(abilityId) && !HasSpaceForSide(targetCombat, side))
            {
                failureMessage = $"target combat({targetCombatIndex}) has no available slot for side({side}).";
                return false;
            }

            RemoveFromCurrentLocation(state, abilityId, side, sourceLocation);
            if (!targetList.Contains(abilityId))
            {
                targetList.Add(abilityId);
            }

            return true;
        }

        public bool TryReturnAbilityToLoadout(
            DuelState state,
            string abilityId,
            DuelSide side,
            out string failureMessage)
        {
            failureMessage = string.Empty;
            if (state == null)
            {
                failureMessage = "duel state is null.";
                return false;
            }

            state.EnsureInitialized();

            if (string.IsNullOrWhiteSpace(abilityId))
            {
                failureMessage = "abilityId is empty.";
                return false;
            }

            if (!state.abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) || ability == null)
            {
                failureMessage = $"ability({abilityId}) does not exist.";
                return false;
            }

            if (ability.abilityType != AbilityType.Attack)
            {
                failureMessage = $"only Attack type ability can be returned to loadout (current: {ability.abilityType}).";
                return false;
            }

            if (!TryFindAbilityLocation(state, abilityId, side, out DuelAbilityLocation sourceLocation, out failureMessage))
            {
                return false;
            }

            if (!sourceLocation.isCombat)
            {
                failureMessage = $"ability({abilityId}) is not deployed in combat.";
                return false;
            }

            RemoveFromCurrentLocation(state, abilityId, side, sourceLocation);
            AddToLoadoutIfMissing(state, abilityId, side);
            return true;
        }

        public DuelAutoDeployResult AutoDeployRandomFromLoadout(
            DuelState state,
            DuelSide side,
            System.Random random)
        {
            if (state == null)
            {
                return new DuelAutoDeployResult(0, 0);
            }

            state.EnsureInitialized();

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            List<string> loadout = GetLoadoutAbilityIds(state, side);
            if (loadout == null || loadout.Count <= 0)
            {
                return new DuelAutoDeployResult(0, 0);
            }

            int deployedCount = 0;
            int skippedCount = 0;
            var pendingAbilityIds = new List<string>(loadout);
            for (int i = 0; i < pendingAbilityIds.Count; i++)
            {
                string abilityId = pendingAbilityIds[i];
                if (string.IsNullOrWhiteSpace(abilityId))
                {
                    skippedCount += 1;
                    continue;
                }

                if (!state.abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) || ability == null)
                {
                    skippedCount += 1;
                    continue;
                }

                ability.EnsureInitialized();
                if (ability.abilityType != AbilityType.Attack || ability.cooldownRemaining > 0)
                {
                    skippedCount += 1;
                    continue;
                }

                List<int> candidateCombats = CollectCombatIndicesWithSpace(state.combats, side);
                if (candidateCombats.Count <= 0)
                {
                    skippedCount += 1;
                    continue;
                }

                int randomIndex = random.Next(0, candidateCombats.Count);
                int combatIndex = candidateCombats[randomIndex];
                if (!TryMoveAbilityToCombat(state, abilityId, combatIndex, side, out _))
                {
                    skippedCount += 1;
                    continue;
                }

                deployedCount += 1;
            }

            return new DuelAutoDeployResult(deployedCount, skippedCount);
        }

        public int ReturnAllDeployedAbilitiesToLoadout(DuelState state)
        {
            if (state == null)
            {
                return 0;
            }

            state.EnsureInitialized();

            int movedCount = 0;
            if (state.combats == null)
            {
                return 0;
            }

            for (int combatIndex = 0; combatIndex < state.combats.Count; combatIndex++)
            {
                CombatState combat = state.combats[combatIndex];
                if (combat == null)
                {
                    continue;
                }

                combat.EnsureInitialized();
                movedCount += ReturnSideToLoadout(state, combat.playerAbilityIds, DuelSide.Player);
                movedCount += ReturnSideToLoadout(state, combat.opponentAbilityIds, DuelSide.Opponent);
            }

            return movedCount;
        }

        public bool TryFindAbilityLocation(
            DuelState state,
            string abilityId,
            DuelSide side,
            out DuelAbilityLocation location,
            out string failureMessage)
        {
            location = new DuelAbilityLocation(DuelAbilityLocationType.None, -1);
            failureMessage = string.Empty;

            if (state == null)
            {
                failureMessage = "duel state is null.";
                return false;
            }

            state.EnsureInitialized();

            if (string.IsNullOrWhiteSpace(abilityId))
            {
                failureMessage = "abilityId is empty.";
                return false;
            }

            int hitCount = 0;
            List<string> loadout = GetLoadoutAbilityIds(state, side);
            if (loadout != null && loadout.Contains(abilityId))
            {
                hitCount += 1;
                location = new DuelAbilityLocation(DuelAbilityLocationType.Loadout, -1);
            }

            if (state.combats != null)
            {
                for (int combatIndex = 0; combatIndex < state.combats.Count; combatIndex++)
                {
                    CombatState combat = state.combats[combatIndex];
                    if (combat == null)
                    {
                        continue;
                    }

                    combat.EnsureInitialized();
                    List<string> sideAbilityIds = GetCombatAbilityIds(combat, side);
                    if (!sideAbilityIds.Contains(abilityId))
                    {
                        continue;
                    }

                    hitCount += 1;
                    location = new DuelAbilityLocation(DuelAbilityLocationType.Combat, combatIndex);
                }
            }

            if (hitCount == 0)
            {
                failureMessage = $"ability({abilityId}) is not found on side({side}) controllable zones.";
                return false;
            }

            if (hitCount > 1)
            {
                failureMessage = $"ability({abilityId}) is duplicated across side({side}) locations.";
                return false;
            }

            return true;
        }

        static bool TryValidateMoveRequest(
            DuelState state,
            string abilityId,
            int targetCombatIndex,
            out AbilityInstance ability,
            out string failureMessage)
        {
            ability = null;
            failureMessage = string.Empty;

            if (state == null)
            {
                failureMessage = "duel state is null.";
                return false;
            }

            state.EnsureInitialized();

            if (string.IsNullOrWhiteSpace(abilityId))
            {
                failureMessage = "abilityId is empty.";
                return false;
            }

            if (targetCombatIndex < 0 ||
                state.combats == null ||
                targetCombatIndex >= state.combats.Count)
            {
                failureMessage = $"target combat({targetCombatIndex}) is out of range.";
                return false;
            }

            if (!state.abilitiesById.TryGetValue(abilityId, out ability) || ability == null)
            {
                failureMessage = $"ability({abilityId}) does not exist.";
                return false;
            }

            ability.EnsureInitialized();
            if (ability.abilityType != AbilityType.Attack)
            {
                failureMessage = $"only Attack type ability can be moved to combat (current: {ability.abilityType}).";
                return false;
            }

            if (ability.cooldownRemaining > 0)
            {
                failureMessage = $"ability({abilityId}) is on cooldown ({ability.cooldownRemaining}).";
                return false;
            }

            return true;
        }

        static void RemoveFromCurrentLocation(
            DuelState state,
            string abilityId,
            DuelSide side,
            DuelAbilityLocation location)
        {
            if (location.locationType == DuelAbilityLocationType.Loadout)
            {
                List<string> loadout = GetLoadoutAbilityIds(state, side);
                loadout?.Remove(abilityId);
                return;
            }

            if (location.locationType != DuelAbilityLocationType.Combat ||
                location.combatIndex < 0 ||
                state.combats == null ||
                location.combatIndex >= state.combats.Count)
            {
                return;
            }

            CombatState combat = state.combats[location.combatIndex];
            if (combat == null)
            {
                return;
            }

            combat.EnsureInitialized();
            List<string> sideAbilityIds = GetCombatAbilityIds(combat, side);
            sideAbilityIds.Remove(abilityId);
        }

        static List<int> CollectCombatIndicesWithSpace(
            IReadOnlyList<CombatState> combats,
            DuelSide side)
        {
            var indices = new List<int>();
            if (combats == null)
            {
                return indices;
            }

            for (int combatIndex = 0; combatIndex < combats.Count; combatIndex++)
            {
                CombatState combat = combats[combatIndex];
                if (combat == null)
                {
                    continue;
                }

                combat.EnsureInitialized();
                if (HasSpaceForSide(combat, side))
                {
                    indices.Add(combatIndex);
                }
            }

            return indices;
        }

        static int ReturnSideToLoadout(
            DuelState state,
            List<string> deployedAbilityIds,
            DuelSide side)
        {
            if (deployedAbilityIds == null || deployedAbilityIds.Count <= 0)
            {
                return 0;
            }

            int movedCount = 0;
            List<string> loadout = GetLoadoutAbilityIds(state, side);
            for (int i = 0; i < deployedAbilityIds.Count; i++)
            {
                string abilityId = deployedAbilityIds[i];
                if (string.IsNullOrWhiteSpace(abilityId))
                {
                    continue;
                }

                if (!loadout.Contains(abilityId))
                {
                    loadout.Add(abilityId);
                }

                movedCount += 1;
            }

            deployedAbilityIds.Clear();
            return movedCount;
        }

        static void AddToLoadoutIfMissing(DuelState state, string abilityId, DuelSide side)
        {
            List<string> loadout = GetLoadoutAbilityIds(state, side);
            if (loadout == null || loadout.Contains(abilityId))
            {
                return;
            }

            loadout.Add(abilityId);
        }

        static List<string> GetLoadoutAbilityIds(DuelState state, DuelSide side)
        {
            if (state == null)
            {
                return null;
            }

            return side == DuelSide.Player
                ? state.loadoutAbilityIds
                : state.opponentLoadoutAbilityIds;
        }

        static List<string> GetCombatAbilityIds(CombatState combat, DuelSide side)
        {
            return side == DuelSide.Player
                ? combat.playerAbilityIds
                : combat.opponentAbilityIds;
        }

        static bool HasSpaceForSide(CombatState combat, DuelSide side)
        {
            if (combat == null)
            {
                return false;
            }

            List<string> abilityIds = GetCombatAbilityIds(combat, side);
            int? maxAssignments = side == DuelSide.Player
                ? combat.maxPlayerAssignments
                : combat.maxOpponentAssignments;
            return !maxAssignments.HasValue ||
                   maxAssignments.Value <= 0 ||
                   abilityIds.Count < maxAssignments.Value;
        }
    }
}
