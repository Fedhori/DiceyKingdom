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

        public OpponentSetupBuildResult(int deployedCount, int skippedCount)
        {
            this.deployedCount = deployedCount;
            this.skippedCount = skippedCount;
        }
    }

    public sealed class DuelSessionBuilder
    {
        const int fixedCombatCount = 3;

        readonly GameDatabase database;
        readonly System.Random random;

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
                int skipped = state.opponentLoadoutEntries == null ? 0 : state.opponentLoadoutEntries.Count;
                Debug.LogWarning("[DuelSessionBuilder] Opponent deploy skipped: abilities table is missing.");
                return new OpponentSetupBuildResult(0, skipped);
            }

            if (state.combats == null || state.combats.Count <= 0)
            {
                int skipped = state.opponentLoadoutEntries == null ? 0 : state.opponentLoadoutEntries.Count;
                Debug.LogWarning("[DuelSessionBuilder] Opponent deploy skipped: combat slots are missing.");
                return new OpponentSetupBuildResult(0, skipped);
            }

            RemoveCurrentOpponentAbilityInstances(state);

            int deployedCount = 0;
            int skippedCount = 0;

            for (int i = 0; i < state.opponentLoadoutEntries.Count; i++)
            {
                OpponentLoadoutEntry loadoutEntry = state.opponentLoadoutEntries[i];
                if (loadoutEntry == null)
                {
                    skippedCount += 1;
                    Debug.LogWarning($"[DuelSessionBuilder] opponentLoadoutEntries[{i}] is null.");
                    continue;
                }

                if (!database.abilitiesById.TryGetValue(loadoutEntry.abilityDefId, out AbilityDef abilityDef) ||
                    abilityDef == null)
                {
                    skippedCount += Mathf.Max(1, loadoutEntry.count);
                    Debug.LogWarning($"[DuelSessionBuilder] abilityDef('{loadoutEntry.abilityDefId}') is missing.");
                    continue;
                }

                if (!abilityDef.TryGetAbilityType(out AbilityType abilityType))
                {
                    skippedCount += Mathf.Max(1, loadoutEntry.count);
                    Debug.LogWarning(
                        $"[DuelSessionBuilder] Invalid ability type '{abilityDef.type}' on '{abilityDef.id}'.");
                    continue;
                }

                if (abilityType == AbilityType.Skill)
                {
                    skippedCount += Mathf.Max(0, loadoutEntry.count);
                    continue;
                }

                int requestedCount = Mathf.Max(0, loadoutEntry.count);
                for (int copyIndex = 0; copyIndex < requestedCount; copyIndex++)
                {
                    AbilityInstance abilityInstance = CreateAbilityInstance(abilityDef);
                    state.abilitiesById[abilityInstance.instanceId] = abilityInstance;

                    int combatIndex = random.Next(0, state.combats.Count);
                    CombatState deployCombat = state.combats[combatIndex];
                    if (deployCombat == null)
                    {
                        skippedCount += 1;
                        Debug.LogWarning($"[DuelSessionBuilder] combats[{combatIndex}] is null.");
                        continue;
                    }

                    deployCombat.EnsureInitialized();
                    deployCombat.opponentAbilityIds.Add(abilityInstance.instanceId);
                    deployedCount += 1;
                }
            }

            return new OpponentSetupBuildResult(deployedCount, skippedCount);
        }

        DuelState CreateInitialDuelState(EnemyDef enemyDef)
        {
            int? defaultSlotLimit = null;
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
                enemyId = enemyDef.id
            };

            nextState.loadoutAbilityIds.Clear();
            nextState.abilitiesById.Clear();
            nextState.opponentLoadoutEntries.Clear();
            nextState.combats.Clear();

            BuildCombatSlots(nextState, defaultSlotLimit);
            BuildOpponentLoadoutEntries(nextState, enemyDef);
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
                    maxPlayerAssignments = defaultSlotLimit
                };
                combatState.EnsureInitialized();
                state.combats.Add(combatState);
            }
        }

        static void BuildOpponentLoadoutEntries(DuelState state, EnemyDef enemyDef)
        {
            state.opponentLoadoutEntries.Clear();

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

                state.opponentLoadoutEntries.Add(new OpponentLoadoutEntry
                {
                    abilityDefId = abilityRef.abilityId,
                    count = abilityRef.count
                });
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

                AbilityInstance abilityInstance = CreateAbilityInstance(abilityDef);
                state.abilitiesById[abilityInstance.instanceId] = abilityInstance;
                state.loadoutAbilityIds.Add(abilityInstance.instanceId);
            }
        }

        static AbilityInstance CreateAbilityInstance(AbilityDef abilityDef)
        {
            AbilityType abilityType = AbilityType.Attack;
            if (!abilityDef.TryGetAbilityType(out abilityType))
            {
                Debug.LogWarning(
                    $"[DuelSessionBuilder] Invalid ability type '{abilityDef.type}' on '{abilityDef.id}'. Defaulted to Attack.");
                abilityType = AbilityType.Attack;
            }

            int resolvedPower = Mathf.Max(0, abilityDef.ResolvePower());
            var abilityInstance = new AbilityInstance
            {
                abilityDefId = abilityDef.id,
                abilityType = abilityType,
                cooldownTurns = Mathf.Max(0, abilityDef.cooldown),
                cooldownRemaining = 0,
                power = resolvedPower,
                baseRoll = 0,
                powerResult = 0
            };

            if (abilityDef.tags != null && abilityDef.tags.Count > 0)
            {
                abilityInstance.tags.AddRange(abilityDef.tags);
            }

            abilityInstance.EnsureInitialized();
            return abilityInstance;
        }

        static void RemoveCurrentOpponentAbilityInstances(DuelState state)
        {
            if (state.combats == null || state.abilitiesById == null)
            {
                return;
            }

            for (int combatIndex = 0; combatIndex < state.combats.Count; combatIndex++)
            {
                CombatState combat = state.combats[combatIndex];
                if (combat == null)
                {
                    continue;
                }

                combat.EnsureInitialized();
                for (int i = 0; i < combat.opponentAbilityIds.Count; i++)
                {
                    string abilityId = combat.opponentAbilityIds[i];
                    if (string.IsNullOrWhiteSpace(abilityId))
                    {
                        continue;
                    }

                    state.abilitiesById.Remove(abilityId);
                }

                combat.opponentAbilityIds.Clear();
            }
        }
    }
}
