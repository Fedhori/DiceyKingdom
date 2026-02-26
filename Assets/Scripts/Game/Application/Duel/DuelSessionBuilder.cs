using System;
using System.Collections.Generic;
using Game.Domain.Duel;
using Game.Infrastructure.Data;
using UnityEngine;

namespace Game.Application.Duel
{
    public readonly struct OpponentSetupBuildResult
    {
        public int deployedCount { get; }
        public int skippedCount { get; }
        public IReadOnlyList<string> deployedAbilityIds { get; }
        public IReadOnlyList<DuelOpponentDeployStep> steps { get; }

        public OpponentSetupBuildResult(
            int deployedCount,
            int skippedCount,
            IReadOnlyList<string> deployedAbilityIds = null,
            IReadOnlyList<DuelOpponentDeployStep> steps = null)
        {
            this.deployedCount = deployedCount;
            this.skippedCount = skippedCount;
            this.deployedAbilityIds = deployedAbilityIds ?? Array.Empty<string>();
            this.steps = steps ?? Array.Empty<DuelOpponentDeployStep>();
        }
    }

    public sealed class DuelSessionBuilder
    {
        const int fixedCombatCount = 3;
        const int defaultCombatSlotLimitPerSide = 6;

        readonly GameDatabase database;
        readonly System.Random random;
        readonly DuelAbilityPlacementService placementService = new();

        public DuelSessionBuilder(GameDatabase database, System.Random random = null)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            this.random = random ?? new System.Random();
        }

        public bool TryCreateInitialState(
            string enemyId,
            out DuelState state,
            out string failureMessage)
        {
            state = null;
            failureMessage = string.Empty;

            if (database.duelConfig == null)
            {
                failureMessage = "duel.config is missing.";
                return false;
            }

            if (database.playerStart == null)
            {
                failureMessage = "player.start is missing.";
                return false;
            }

            if (database.runConfig == null)
            {
                failureMessage = "run.config is missing.";
                return false;
            }

            if (database.enemiesById == null)
            {
                failureMessage = "enemies table is missing.";
                return false;
            }

            if (database.abilitiesById == null)
            {
                failureMessage = "abilities table is missing.";
                return false;
            }

            if (!database.enemiesById.TryGetValue(enemyId, out EnemyDef enemyDef) ||
                enemyDef == null)
            {
                failureMessage = $"enemy('{enemyId}') is missing.";
                return false;
            }

            int playerLoadoutCount = database.playerStart.startingLoadoutAbilityIds == null
                ? 0
                : database.playerStart.startingLoadoutAbilityIds.Count;
            if (playerLoadoutCount > DuelAbilityPlacementService.MaxLoadoutAbilityCount)
            {
                failureMessage =
                    $"player.start loadout count({playerLoadoutCount}) exceeds max({DuelAbilityPlacementService.MaxLoadoutAbilityCount}).";
                return false;
            }

            int opponentLoadoutCount = CountEnemyLoadoutAbilityInstances(enemyDef);
            if (opponentLoadoutCount > DuelAbilityPlacementService.MaxLoadoutAbilityCount)
            {
                failureMessage =
                    $"enemy('{enemyDef.id}') loadout count({opponentLoadoutCount}) exceeds max({DuelAbilityPlacementService.MaxLoadoutAbilityCount}).";
                return false;
            }

            state = CreateInitialDuelState(enemyDef);
            return true;
        }

        public OpponentSetupBuildResult AutoDeployOpponentCombat(DuelState state)
        {
            if (state == null)
            {
                return new OpponentSetupBuildResult(0, 0);
            }

            state.EnsureInitialized();

            if (database.abilitiesById == null)
            {
                int skipped = state.opponentLoadoutAbilityIds == null ? 0 : state.opponentLoadoutAbilityIds.Count;
                Debug.LogWarning("[DuelSessionBuilder] Opponent deploy skipped: abilities table is missing.");
                return new OpponentSetupBuildResult(0, skipped);
            }

            if (state.combats == null || state.combats.Count <= 0)
            {
                int skipped = state.opponentLoadoutAbilityIds == null ? 0 : state.opponentLoadoutAbilityIds.Count;
                Debug.LogWarning("[DuelSessionBuilder] Opponent deploy skipped: combat slots are missing.");
                return new OpponentSetupBuildResult(0, skipped);
            }

            DuelAutoDeployResult result = placementService.AutoDeployRandomFromLoadout(
                state,
                DuelSide.Opponent,
                random);
            return new OpponentSetupBuildResult(
                result.deployedCount,
                result.skippedCount,
                result.deployedAbilityIds,
                result.steps);
        }

        public OpponentSetupBuildResult BuildOpponentDeployPlan(DuelState state)
        {
            if (state == null)
            {
                return new OpponentSetupBuildResult(0, 0);
            }

            state.EnsureInitialized();

            if (database.abilitiesById == null)
            {
                int skipped = state.opponentLoadoutAbilityIds == null ? 0 : state.opponentLoadoutAbilityIds.Count;
                Debug.LogWarning("[DuelSessionBuilder] Opponent deploy plan skipped: abilities table is missing.");
                return new OpponentSetupBuildResult(0, skipped);
            }

            if (state.combats == null || state.combats.Count <= 0)
            {
                int skipped = state.opponentLoadoutAbilityIds == null ? 0 : state.opponentLoadoutAbilityIds.Count;
                Debug.LogWarning("[DuelSessionBuilder] Opponent deploy plan skipped: combat slots are missing.");
                return new OpponentSetupBuildResult(0, skipped);
            }

            DuelAutoDeployResult plan = placementService.PlanAutoDeployRandomFromLoadout(
                state,
                DuelSide.Opponent,
                random);
            return new OpponentSetupBuildResult(
                plan.deployedCount,
                plan.skippedCount,
                plan.deployedAbilityIds,
                plan.steps);
        }

        public bool TryApplyOpponentDeployStep(
            DuelState state,
            DuelOpponentDeployStep step,
            out string failureMessage)
        {
            return placementService.TryApplyDeployStep(
                state,
                DuelSide.Opponent,
                step,
                out failureMessage);
        }

        DuelState CreateInitialDuelState(EnemyDef enemyDef)
        {
            int? defaultSlotLimit = defaultCombatSlotLimitPerSide;
            if (database.duelConfig != null &&
                database.duelConfig.p0Rules != null &&
                database.duelConfig.p0Rules.defaultSlotLimit.HasValue &&
                database.duelConfig.p0Rules.defaultSlotLimit.Value > 0)
            {
                defaultSlotLimit = database.duelConfig.p0Rules.defaultSlotLimit.Value;
            }

            var nextState = new DuelState
            {
                turnIndex = 0,
                isDuelEnded = false,
                honor = database.playerStart.startingHonor,
                playerHealth = Mathf.Max(1, database.playerStart.startingPlayerHealth),
                opponentHealth = Mathf.Max(1, enemyDef.health),
                maxPlayerHealth = Mathf.Max(1, database.playerStart.startingPlayerHealth),
                maxOpponentHealth = Mathf.Max(1, enemyDef.health),
                enemyId = enemyDef.id
            };

            nextState.loadoutAbilityIds.Clear();
            nextState.opponentLoadoutAbilityIds.Clear();
            nextState.abilitiesById.Clear();
            nextState.combats.Clear();

            BuildCombatSlots(nextState, defaultSlotLimit);
            PopulateOpponentLoadoutFromEnemyDef(nextState, enemyDef);
            PopulateLoadoutFromPlayerStart(nextState);

            return nextState;
        }

        static void BuildCombatSlots(DuelState state, int? defaultSlotLimit)
        {
            state.combats.Clear();

            for (int combatIndex = 0; combatIndex < fixedCombatCount; combatIndex++)
            {
                var combatState = new CombatState
                {
                    maxPlayerAssignments = defaultSlotLimit,
                    maxOpponentAssignments = defaultSlotLimit
                };
                combatState.EnsureInitialized();
                state.combats.Add(combatState);
            }
        }

        void PopulateOpponentLoadoutFromEnemyDef(DuelState state, EnemyDef enemyDef)
        {
            state.opponentLoadoutAbilityIds.Clear();

            if (enemyDef == null || enemyDef.abilityLoadout == null)
            {
                return;
            }

            for (int loadoutIndex = 0; loadoutIndex < enemyDef.abilityLoadout.Count; loadoutIndex++)
            {
                SummonAbilityRefDef abilityRef = enemyDef.abilityLoadout[loadoutIndex];
                if (abilityRef == null || abilityRef.count <= 0 || string.IsNullOrWhiteSpace(abilityRef.abilityId))
                {
                    continue;
                }

                if (!database.abilitiesById.TryGetValue(abilityRef.abilityId, out AbilityDef abilityDef) || abilityDef == null)
                {
                    Debug.LogWarning(
                        $"[DuelSessionBuilder] enemy('{enemyDef.id}') abilityLoadout[{loadoutIndex}] '{abilityRef.abilityId}' is missing.");
                    continue;
                }

                int requestedCount = Mathf.Max(0, abilityRef.count);
                for (int copyIndex = 0; copyIndex < requestedCount; copyIndex++)
                {
                    AbilityInstance abilityInstance = CreateAbilityInstance(abilityRef.abilityId, abilityDef);
                    state.abilitiesById[abilityInstance.instanceId] = abilityInstance;
                    state.opponentLoadoutAbilityIds.Add(abilityInstance.instanceId);
                }
            }
        }

        void PopulateLoadoutFromPlayerStart(DuelState state)
        {
            List<string> startingAbilityIds = database.playerStart.startingLoadoutAbilityIds;
            if (startingAbilityIds == null)
            {
                Debug.LogWarning("[DuelSessionBuilder] startingLoadoutAbilityIds is missing.");
                return;
            }

            for (int abilityIndex = 0; abilityIndex < startingAbilityIds.Count; abilityIndex++)
            {
                string abilityDefId = startingAbilityIds[abilityIndex];
                if (string.IsNullOrWhiteSpace(abilityDefId))
                {
                    continue;
                }

                if (!database.abilitiesById.TryGetValue(abilityDefId, out AbilityDef abilityDef) || abilityDef == null)
                {
                    Debug.LogWarning(
                        $"[DuelSessionBuilder] startingLoadoutAbilityIds[{abilityIndex}] '{abilityDefId}' is missing.");
                    continue;
                }

                if (!abilityDef.isPlayerObtainable)
                {
                    Debug.LogError(
                        $"[DuelSessionBuilder] startingLoadoutAbilityIds[{abilityIndex}] '{abilityDefId}' must be isPlayerObtainable=true.");
                    continue;
                }

                AbilityInstance abilityInstance = CreateAbilityInstance(abilityDefId, abilityDef);
                state.abilitiesById[abilityInstance.instanceId] = abilityInstance;
                state.loadoutAbilityIds.Add(abilityInstance.instanceId);
            }
        }

        static AbilityInstance CreateAbilityInstance(string abilityDefId, AbilityDef abilityDef)
        {
            AbilityType abilityType = AbilityType.Attack;
            if (!abilityDef.TryGetAbilityType(out abilityType))
            {
                Debug.LogWarning(
                    $"[DuelSessionBuilder] Invalid ability type '{abilityDef.type}' on '{abilityDef.id}'. Defaulted to Attack.");
                abilityType = AbilityType.Attack;
            }

            int resolvedPower = Mathf.Max(0, abilityDef.ResolvePower());
            int cooldownTurns = abilityDef.ResolveCooldownTurns(abilityType);
            int minCooldown = AbilityDef.GetMinimumCooldownTurns(abilityType);
            if (cooldownTurns < minCooldown)
            {
                string message =
                    $"[DuelSessionBuilder] Invalid cooldown({cooldownTurns}) for '{abilityDefId}' type({abilityType}).";
                Debug.LogError(message);
                throw new InvalidOperationException(message);
            }

            var abilityInstance = new AbilityInstance
            {
                abilityDefId = abilityDefId,
                abilityType = abilityType,
                cooldownTurns = cooldownTurns,
                cooldownRemaining = 0,
                power = resolvedPower,
                baseRoll = 0,
                powerResult = 0
            };

            abilityInstance.EnsureInitialized();
            return abilityInstance;
        }

        static int CountEnemyLoadoutAbilityInstances(EnemyDef enemyDef)
        {
            if (enemyDef == null || enemyDef.abilityLoadout == null)
            {
                return 0;
            }

            int total = 0;
            for (int i = 0; i < enemyDef.abilityLoadout.Count; i++)
            {
                SummonAbilityRefDef entry = enemyDef.abilityLoadout[i];
                if (entry == null || entry.count <= 0)
                {
                    continue;
                }

                total += entry.count;
            }

            return total;
        }
    }
}
