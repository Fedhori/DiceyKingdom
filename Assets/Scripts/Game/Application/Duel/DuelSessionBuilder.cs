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

            if (encounterDef.enemy.patterns == null || encounterDef.enemy.patterns.Count <= 0)
            {
                failureMessage = $"encounter('{encounterId}') enemy.patterns is missing or empty.";
                return false;
            }

            EncounterEnemyPatternDef startPattern = ResolvePattern(
                encounterDef.enemy.patterns,
                encounterDef.enemy.startPatternId);
            if (startPattern == null)
            {
                failureMessage = $"encounter('{encounterId}') startPatternId('{encounterDef.enemy.startPatternId}') is invalid.";
                return false;
            }

            state = CreateInitialDuelState(encounterDef, startPattern);
            return true;
        }

        public OpponentSetupBuildResult AutoDeployOpponentClash(DuelState state)
        {
            if (state == null)
            {
                return new OpponentSetupBuildResult(0, 0);
            }

            state.EnsureInitialized();

            if (database.abilitiesById == null)
            {
                int skipped = state.opponentClashLoadoutEntries == null ? 0 : state.opponentClashLoadoutEntries.Count;
                Debug.LogWarning("[DuelSessionBuilder] Opponent deploy skipped: abilities table is missing.");
                return new OpponentSetupBuildResult(0, skipped);
            }

            RemoveCurrentOpponentAbilityInstances(state);

            int deployedCount = 0;
            int skippedCount = 0;

            for (int i = 0; i < state.opponentClashLoadoutEntries.Count; i++)
            {
                OpponentClashLoadoutEntry loadoutEntry = state.opponentClashLoadoutEntries[i];
                if (loadoutEntry == null)
                {
                    skippedCount += 1;
                    Debug.LogWarning($"[DuelSessionBuilder] opponentClashLoadoutEntries[{i}] is null.");
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

                if (loadoutEntry.clashIndex < 0 || loadoutEntry.clashIndex >= state.clashes.Count)
                {
                    skippedCount += Mathf.Max(1, loadoutEntry.count);
                    Debug.LogWarning(
                        $"[DuelSessionBuilder] clashIndex({loadoutEntry.clashIndex}) is out of range for abilityDef('{loadoutEntry.abilityDefId}').");
                    continue;
                }

                ClashState deployClash = state.clashes[loadoutEntry.clashIndex];
                if (deployClash == null)
                {
                    skippedCount += Mathf.Max(1, loadoutEntry.count);
                    Debug.LogWarning(
                        $"[DuelSessionBuilder] clashes[{loadoutEntry.clashIndex}] is null for abilityDef('{loadoutEntry.abilityDefId}').");
                    continue;
                }

                deployClash.EnsureInitialized();
                int requestedCount = Mathf.Max(0, loadoutEntry.count);
                for (int copyIndex = 0; copyIndex < requestedCount; copyIndex++)
                {
                    AbilityInstance abilityInstance = CreateAbilityInstance(abilityDef);
                    state.abilitiesById[abilityInstance.instanceId] = abilityInstance;
                    deployClash.opponentAbilityIds.Add(abilityInstance.instanceId);
                    deployedCount += 1;
                }
            }

            return new OpponentSetupBuildResult(deployedCount, skippedCount);
        }

        DuelState CreateInitialDuelState(EncounterDef encounterDef, EncounterEnemyPatternDef startPattern)
        {
            var nextState = new DuelState
            {
                turnIndex = 0,
                isDuelEnded = false,
                honor = database.playerStart.startingHonor,
                playerHealth = Mathf.Max(1, database.playerStart.startingPlayerHealth),
                opponentHealth = Mathf.Max(1, encounterDef.enemy.health),
                encounterId = encounterDef.id,
                currentPatternId = startPattern.patternId
            };

            nextState.loadoutAbilityIds.Clear();
            nextState.abilitiesById.Clear();
            nextState.opponentClashLoadoutEntries.Clear();
            nextState.clashes.Clear();

            BuildClashSlotsFromPattern(nextState, startPattern);
            PopulateLoadoutFromPlayerStart(nextState);

            return nextState;
        }

        static void BuildClashSlotsFromPattern(DuelState state, EncounterEnemyPatternDef pattern)
        {
            state.clashes.Clear();
            state.opponentClashLoadoutEntries.Clear();

            if (pattern == null || pattern.clashes == null)
            {
                return;
            }

            for (int clashIndex = 0; clashIndex < pattern.clashes.Count; clashIndex++)
            {
                EncounterEnemyClashDef clashDef = pattern.clashes[clashIndex];
                if (clashDef == null)
                {
                    state.clashes.Add(new ClashState());
                    continue;
                }

                var clashState = new ClashState
                {
                    clashId = clashDef.clashId,
                    maxPlayerAssignments = clashDef.maxPlayerAssignments
                };
                clashState.EnsureInitialized();
                state.clashes.Add(clashState);

                if (clashDef.abilityLoadout == null)
                {
                    continue;
                }

                for (int loadoutIndex = 0; loadoutIndex < clashDef.abilityLoadout.Count; loadoutIndex++)
                {
                    SummonAbilityRefDef abilityRef = clashDef.abilityLoadout[loadoutIndex];
                    if (abilityRef == null || abilityRef.count <= 0 || string.IsNullOrWhiteSpace(abilityRef.abilityId))
                    {
                        continue;
                    }

                    state.opponentClashLoadoutEntries.Add(new OpponentClashLoadoutEntry
                    {
                        clashIndex = clashIndex,
                        abilityDefId = abilityRef.abilityId,
                        count = abilityRef.count
                    });
                }
            }
        }

        static EncounterEnemyPatternDef ResolvePattern(
            IReadOnlyList<EncounterEnemyPatternDef> patterns,
            string patternId)
        {
            if (patterns == null || string.IsNullOrWhiteSpace(patternId))
            {
                return null;
            }

            for (int i = 0; i < patterns.Count; i++)
            {
                EncounterEnemyPatternDef pattern = patterns[i];
                if (pattern == null)
                {
                    continue;
                }

                if (string.Equals(pattern.patternId, patternId, StringComparison.Ordinal))
                {
                    return pattern;
                }
            }

            return null;
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
            if (state.clashes == null || state.abilitiesById == null)
            {
                return;
            }

            for (int clashIndex = 0; clashIndex < state.clashes.Count; clashIndex++)
            {
                ClashState clash = state.clashes[clashIndex];
                if (clash == null)
                {
                    continue;
                }

                clash.EnsureInitialized();
                for (int i = 0; i < clash.opponentAbilityIds.Count; i++)
                {
                    string abilityId = clash.opponentAbilityIds[i];
                    if (string.IsNullOrWhiteSpace(abilityId))
                    {
                        continue;
                    }

                    state.abilitiesById.Remove(abilityId);
                }

                clash.opponentAbilityIds.Clear();
            }
        }
    }
}
