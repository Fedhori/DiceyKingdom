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
        readonly GameDatabase database;

        public DuelSessionBuilder(GameDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public bool TryCreateInitialState(
            string encounterId,
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

            if (database.encountersById == null)
            {
                failureMessage = "encounters table is missing.";
                return false;
            }

            if (database.clashesById == null)
            {
                failureMessage = "clashes table is missing.";
                return false;
            }

            if (database.abilitiesById == null)
            {
                failureMessage = "abilities table is missing.";
                return false;
            }

            if (!database.encountersById.TryGetValue(encounterId, out EncounterDef encounterDef) ||
                encounterDef == null)
            {
                failureMessage = $"encounter('{encounterId}') is missing.";
                return false;
            }

            if (encounterDef.enemy == null)
            {
                failureMessage = $"encounter('{encounterId}') enemy is missing.";
                return false;
            }

            if (encounterDef.enemy.clashes == null || encounterDef.enemy.clashes.Count <= 0)
            {
                failureMessage = $"encounter('{encounterId}') enemy.clashes is missing or empty.";
                return false;
            }

            state = CreateInitialDuelState(encounterDef);
            return true;
        }

        public OpponentSetupBuildResult AutoDeployOpponentIntent(DuelState state)
        {
            if (state == null)
            {
                return new OpponentSetupBuildResult(0, 0);
            }

            state.EnsureInitialized();

            if (database.abilitiesById == null)
            {
                int skipped = state.intent == null ? 0 : state.intent.Count;
                Debug.LogWarning("[DuelSessionBuilder] Opponent deploy skipped: abilities table is missing.");
                return new OpponentSetupBuildResult(0, skipped);
            }

            int deployedCount = 0;
            int skippedCount = 0;

            for (int i = 0; i < state.intent.Count; i++)
            {
                IntentEntry intent = state.intent[i];
                if (intent == null)
                {
                    skippedCount += 1;
                    Debug.LogWarning($"[DuelSessionBuilder] intent[{i}] is null.");
                    continue;
                }

                if (!database.abilitiesById.TryGetValue(intent.abilityDefId, out AbilityDef abilityDef) ||
                    abilityDef == null)
                {
                    skippedCount += Mathf.Max(1, intent.count);
                    Debug.LogWarning($"[DuelSessionBuilder] abilityDef('{intent.abilityDefId}') is missing.");
                    continue;
                }

                if (!abilityDef.TryGetAbilityType(out AbilityType abilityType))
                {
                    skippedCount += Mathf.Max(1, intent.count);
                    Debug.LogWarning(
                        $"[DuelSessionBuilder] Invalid ability type '{abilityDef.type}' on '{abilityDef.id}'.");
                    continue;
                }

                if (abilityType == AbilityType.Skill)
                {
                    skippedCount += Mathf.Max(0, intent.count);
                    continue;
                }

                int requestedCount = Mathf.Max(0, intent.count);
                for (int copyIndex = 0; copyIndex < requestedCount; copyIndex++)
                {
                    if (!TryFindOpponentClashForDeploy(state, intent.clashIndex, out int deployClashIndex))
                    {
                        skippedCount += 1;
                        Debug.LogWarning(
                            $"[DuelSessionBuilder] no available clash slot for abilityDef('{intent.abilityDefId}').");
                        continue;
                    }

                    ClashState deployClash = state.clashes[deployClashIndex];
                    deployClash.EnsureInitialized();

                    AbilityInstance abilityInstance = CreateAbilityInstance(abilityDef);
                    state.abilitiesById[abilityInstance.instanceId] = abilityInstance;
                    deployClash.opponentAbilityIds.Add(abilityInstance.instanceId);
                    deployedCount += 1;
                }
            }

            return new OpponentSetupBuildResult(deployedCount, skippedCount);
        }

        DuelState CreateInitialDuelState(EncounterDef encounterDef)
        {
            var nextState = new DuelState
            {
                turnIndex = 0,
                isDuelEnded = false,
                honor = database.playerStart.startingHonor,
                playerHealth = Mathf.Max(1, database.playerStart.startingPlayerHealth),
                opponentHealth = Mathf.Max(1, encounterDef.enemy.health)
            };

            nextState.bagAbilityIds.Clear();
            nextState.abilitiesById.Clear();
            nextState.intent.Clear();

            InitializeClashSlots(nextState, encounterDef);
            PopulateOpponentIntent(nextState, encounterDef);
            PopulateBagFromPlayerStart(nextState);

            return nextState;
        }

        void InitializeClashSlots(DuelState state, EncounterDef encounterDef)
        {
            state.clashes.Clear();

            List<EncounterEnemyClashDef> enemyClashes = ResolveEncounterEnemyClashes(encounterDef);
            int targetCount = enemyClashes.Count;

            for (int i = 0; i < targetCount; i++)
            {
                string clashIdFromEnemy = enemyClashes[i].clashId;

                ClashDef sourceDef = ResolveClashDef(clashIdFromEnemy);
                int? resolvedSlotLimit = sourceDef != null
                    ? sourceDef.slotLimit
                    : database.duelConfig.p0Rules.defaultSlotLimit;

                var clashState = new ClashState
                {
                    clashId = sourceDef?.id ?? clashIdFromEnemy,
                    slotLimit = resolvedSlotLimit,
                    totalPowerBonusPlayer = 0,
                    totalPowerBonusOpponent = 0
                };

                if (sourceDef == null)
                {
                    Debug.LogWarning(
                        $"[DuelSessionBuilder] clashDef('{clashIdFromEnemy}') is missing. defaultSlotLimit will be used.");
                }

                clashState.EnsureInitialized();
                state.clashes.Add(clashState);
            }
        }

        List<EncounterEnemyClashDef> ResolveEncounterEnemyClashes(EncounterDef encounterDef)
        {
            if (encounterDef?.enemy?.clashes == null ||
                encounterDef.enemy.clashes.Count <= 0)
            {
                return new List<EncounterEnemyClashDef>();
            }

            var result = new List<EncounterEnemyClashDef>(encounterDef.enemy.clashes.Count);
            for (int i = 0; i < encounterDef.enemy.clashes.Count; i++)
            {
                EncounterEnemyClashDef clash = encounterDef.enemy.clashes[i];
                if (clash == null)
                {
                    Debug.LogWarning($"[DuelSessionBuilder] encounter enemy.clashes[{i}] is null and has been skipped.");
                    continue;
                }

                result.Add(clash);
            }

            return result;
        }

        ClashDef ResolveClashDef(string clashIdFromEnemy)
        {
            if (string.IsNullOrWhiteSpace(clashIdFromEnemy))
            {
                return null;
            }

            if (database.clashesById != null &&
                database.clashesById.TryGetValue(clashIdFromEnemy, out ClashDef foundByEnemyId))
            {
                return foundByEnemyId;
            }

            return null;
        }

        void PopulateOpponentIntent(DuelState state, EncounterDef encounterDef)
        {
            if (encounterDef?.enemy?.clashes != null &&
                encounterDef.enemy.clashes.Count > 0)
            {
                for (int clashIndex = 0; clashIndex < encounterDef.enemy.clashes.Count; clashIndex++)
                {
                    EncounterEnemyClashDef enemyClash = encounterDef.enemy.clashes[clashIndex];
                    if (enemyClash == null || enemyClash.abilityLoadout == null)
                    {
                        continue;
                    }

                    for (int abilityIndex = 0; abilityIndex < enemyClash.abilityLoadout.Count; abilityIndex++)
                    {
                        SummonAbilityRefDef abilityRef = enemyClash.abilityLoadout[abilityIndex];
                        if (abilityRef == null || abilityRef.count <= 0 || string.IsNullOrWhiteSpace(abilityRef.abilityId))
                        {
                            continue;
                        }

                        state.intent.Add(new IntentEntry
                        {
                            clashIndex = clashIndex,
                            abilityDefId = abilityRef.abilityId,
                            count = abilityRef.count
                        });
                    }
                }

                return;
            }
        }

        void PopulateBagFromPlayerStart(DuelState state)
        {
            List<string> startingAbilityIds = database.playerStart.startingBagAbilityIds;
            if (startingAbilityIds == null)
            {
                Debug.LogWarning("[DuelSessionBuilder] startingBagAbilityIds is missing.");
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
                        $"[DuelSessionBuilder] startingBagAbilityIds[{abilityIndex}] '{abilityDefId}' is missing.");
                    continue;
                }

                AbilityInstance abilityInstance = CreateAbilityInstance(abilityDef);
                state.abilitiesById[abilityInstance.instanceId] = abilityInstance;
                state.bagAbilityIds.Add(abilityInstance.instanceId);
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

        static bool TryFindOpponentClashForDeploy(
            DuelState state,
            int preferredClashIndex,
            out int resolvedClashIndex)
        {
            resolvedClashIndex = -1;

            if (CanDeployOpponentToClash(state, preferredClashIndex))
            {
                resolvedClashIndex = preferredClashIndex;
                return true;
            }

            for (int clashIndex = 0; clashIndex < state.clashes.Count; clashIndex++)
            {
                if (clashIndex == preferredClashIndex)
                {
                    continue;
                }

                if (!CanDeployOpponentToClash(state, clashIndex))
                {
                    continue;
                }

                resolvedClashIndex = clashIndex;
                return true;
            }

            return false;
        }

        static bool CanDeployOpponentToClash(DuelState state, int clashIndex)
        {
            if (state == null || state.clashes == null)
            {
                return false;
            }

            if (clashIndex < 0 || clashIndex >= state.clashes.Count)
            {
                return false;
            }

            ClashState clash = state.clashes[clashIndex];
            if (clash == null)
            {
                return false;
            }

            clash.EnsureInitialized();
            if (!clash.slotLimit.HasValue)
            {
                return true;
            }

            return clash.opponentAbilityIds.Count < clash.slotLimit.Value;
        }
    }
}

