using System;
using System.Collections.Generic;
using System.Linq;
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

            if (database.actionsById == null)
            {
                failureMessage = "actions table is missing.";
                return false;
            }

            if (database.cardsById == null)
            {
                failureMessage = "cards table is missing.";
                return false;
            }

            if (!database.encountersById.TryGetValue(encounterId, out EncounterDef encounterDef) ||
                encounterDef == null)
            {
                failureMessage = $"encounter('{encounterId}') is missing.";
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

            if (database.actionsById == null)
            {
                int skipped = state.opponentIntent == null ? 0 : state.opponentIntent.Count;
                Debug.LogWarning("[DuelSessionBuilder] Opponent deploy skipped: actions table is missing.");
                return new OpponentSetupBuildResult(0, skipped);
            }

            int deployedCount = 0;
            int skippedCount = 0;

            for (int i = 0; i < state.opponentIntent.Count; i++)
            {
                OpponentIntentEntry intent = state.opponentIntent[i];
                if (intent == null)
                {
                    skippedCount += 1;
                    Debug.LogWarning($"[DuelSessionBuilder] opponentIntent[{i}] is null.");
                    continue;
                }

                if (!database.actionsById.TryGetValue(intent.actionDefId, out ActionDef actionDef) || actionDef == null)
                {
                    skippedCount += Mathf.Max(1, intent.count);
                    Debug.LogWarning($"[DuelSessionBuilder] actionDef('{intent.actionDefId}') is missing.");
                    continue;
                }

                if (actionDef.TryGetAbilityType(out AbilityType abilityType) &&
                    abilityType == AbilityType.Skill)
                {
                    skippedCount += Mathf.Max(0, intent.count);
                    continue;
                }

                int requestedCount = Mathf.Max(0, intent.count);
                for (int copyIndex = 0; copyIndex < requestedCount; copyIndex++)
                {
                    if (!TryFindOpponentClashForDeploy(
                            state,
                            intent.clashIndex,
                            out int deployClashIndex))
                    {
                        skippedCount += 1;
                        Debug.LogWarning(
                            $"[DuelSessionBuilder] no available clash slot for actionDef('{intent.actionDefId}').");
                        continue;
                    }

                    ClashState deployClash = state.clashes[deployClashIndex];
                    deployClash.EnsureInitialized();

                    ActionInstance actionInstance = CreateActionInstance(actionDef);
                    state.actionsById[actionInstance.instanceId] = actionInstance;
                    deployClash.opponentActionIds.Add(actionInstance.instanceId);
                    deployedCount += 1;
                }
            }

            return new OpponentSetupBuildResult(deployedCount, skippedCount);
        }

        DuelState CreateInitialDuelState(EncounterDef encounterDef)
        {
            int focusMax = Mathf.Max(0, database.duelConfig.focusMax);
            int startingFocus = Mathf.Clamp(database.playerStart.startingFocus, 0, focusMax);

            var nextState = new DuelState
            {
                turnIndex = 0,
                isDuelEnded = false,
                focus = startingFocus,
                honor = database.playerStart.startingHonor,
                playerHealth = Mathf.Max(1, database.playerStart.startingPlayerHealth),
                opponentHealth = Mathf.Max(1, ResolveEncounterOpponentHealth(encounterDef))
            };

            nextState.actionHolderActionIds.Clear();
            nextState.actionsById.Clear();
            nextState.opponentIntent.Clear();

            InitializeClashSlots(nextState, encounterDef);
            PopulateOpponentIntent(nextState, encounterDef);
            PopulateActionHolderFromPlayerStart(nextState);

            return nextState;
        }

        int ResolveEncounterOpponentHealth(EncounterDef encounterDef)
        {
            if (encounterDef?.enemy != null &&
                encounterDef.enemy.health > 0)
            {
                return encounterDef.enemy.health;
            }

            if (encounterDef != null && encounterDef.opponentHealth > 0)
            {
                return encounterDef.opponentHealth;
            }

            return 1;
        }

        void InitializeClashSlots(DuelState state, EncounterDef encounterDef)
        {
            state.clashes.Clear();

            List<EncounterEnemyClashDef> enemyClashes = ResolveEncounterEnemyClashes(encounterDef);
            List<ClashDef> fallbackOrderedDefs = ResolveFallbackClashes(enemyClashes.Count <= 0);
            int targetCount = enemyClashes.Count > 0
                ? enemyClashes.Count
                : Mathf.Max(1, database.duelConfig.clashCount);

            for (int i = 0; i < targetCount; i++)
            {
                string clashIdFromEnemy = i < enemyClashes.Count
                    ? enemyClashes[i].clashId
                    : string.Empty;

                ClashDef sourceDef = ResolveClashDef(
                    i,
                    clashIdFromEnemy,
                    fallbackOrderedDefs);

                int? resolvedSlotLimit = sourceDef != null
                    ? sourceDef.slotLimit
                    : database.duelConfig.p0Rules.defaultSlotLimit;

                var clashState = new ClashState
                {
                    clashId = sourceDef?.id ?? $"missing_clash_{i}",
                    slotLimit = resolvedSlotLimit,
                    totalAttackBonusPlayer = 0,
                    totalAttackBonusOpponent = 0
                };

                clashState.EnsureInitialized();
                state.clashes.Add(clashState);
            }

            state.EnsureInitialized();
        }

        List<EncounterEnemyClashDef> ResolveEncounterEnemyClashes(EncounterDef encounterDef)
        {
            if (encounterDef?.enemy?.clashes == null ||
                encounterDef.enemy.clashes.Count <= 0)
            {
                return new List<EncounterEnemyClashDef>();
            }

            return encounterDef.enemy.clashes
                .Where(entry => entry != null)
                .ToList();
        }

        List<ClashDef> ResolveFallbackClashes(bool shouldResolve)
        {
            if (!shouldResolve || database.clashesById == null)
            {
                return null;
            }

            return database.clashesById
                .Values
                .OrderBy(def => def.id, StringComparer.Ordinal)
                .ToList();
        }

        ClashDef ResolveClashDef(
            int clashIndex,
            string clashIdFromEnemy,
            List<ClashDef> fallbackOrderedDefs)
        {
            if (!string.IsNullOrWhiteSpace(clashIdFromEnemy) &&
                database.clashesById != null &&
                database.clashesById.TryGetValue(clashIdFromEnemy, out ClashDef foundByEnemyId))
            {
                return foundByEnemyId;
            }

            if (fallbackOrderedDefs != null && clashIndex < fallbackOrderedDefs.Count)
            {
                return fallbackOrderedDefs[clashIndex];
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
                        SummonActionRefDef abilityRef = enemyClash.abilityLoadout[abilityIndex];
                        if (abilityRef == null || abilityRef.count <= 0 || string.IsNullOrWhiteSpace(abilityRef.actionId))
                        {
                            continue;
                        }

                        state.opponentIntent.Add(new OpponentIntentEntry
                        {
                            clashIndex = clashIndex,
                            actionDefId = abilityRef.actionId,
                            count = abilityRef.count
                        });
                    }
                }

                return;
            }

            if (encounterDef?.plans == null)
            {
                return;
            }

            for (int planIndex = 0; planIndex < encounterDef.plans.Count; planIndex++)
            {
                EncounterPlanDef plan = encounterDef.plans[planIndex];
                if (plan == null || plan.actions == null)
                {
                    continue;
                }

                for (int actionIndex = 0; actionIndex < plan.actions.Count; actionIndex++)
                {
                    SummonActionRefDef action = plan.actions[actionIndex];
                    if (action == null || action.count <= 0 || string.IsNullOrWhiteSpace(action.actionId))
                    {
                        continue;
                    }

                    state.opponentIntent.Add(new OpponentIntentEntry
                    {
                        clashIndex = plan.clashIndex,
                        actionDefId = action.actionId,
                        count = action.count
                    });
                }
            }
        }

        void PopulateActionHolderFromPlayerStart(DuelState state)
        {
            if (database.playerStart.startingSquadCardIds == null)
            {
                Debug.LogWarning("[DuelSessionBuilder] startingSquadCardIds is missing.");
                return;
            }

            for (int cardIndex = 0; cardIndex < database.playerStart.startingSquadCardIds.Count; cardIndex++)
            {
                string cardId = database.playerStart.startingSquadCardIds[cardIndex];
                if (string.IsNullOrWhiteSpace(cardId))
                {
                    continue;
                }

                if (!database.cardsById.TryGetValue(cardId, out CardDef squadCard) || squadCard == null)
                {
                    continue;
                }

                if (!string.Equals(squadCard.type, "Squad", StringComparison.Ordinal))
                {
                    continue;
                }

                if (squadCard.duelStart == null || squadCard.duelStart.summonActions == null)
                {
                    continue;
                }

                for (int summonIndex = 0; summonIndex < squadCard.duelStart.summonActions.Count; summonIndex++)
                {
                    SummonActionRefDef summon = squadCard.duelStart.summonActions[summonIndex];
                    if (summon == null || summon.count <= 0 || string.IsNullOrWhiteSpace(summon.actionId))
                    {
                        continue;
                    }

                    if (!database.actionsById.TryGetValue(summon.actionId, out ActionDef actionDef) || actionDef == null)
                    {
                        continue;
                    }

                    for (int i = 0; i < summon.count; i++)
                    {
                        ActionInstance actionInstance = CreateActionInstance(actionDef);
                        state.actionsById[actionInstance.instanceId] = actionInstance;
                        state.actionHolderActionIds.Add(actionInstance.instanceId);
                    }
                }
            }
        }

        static ActionInstance CreateActionInstance(ActionDef actionDef)
        {
            AbilityType abilityType = AbilityType.Attack;
            if (!actionDef.TryGetAbilityType(out abilityType))
            {
                Debug.LogWarning(
                    $"[DuelSessionBuilder] Invalid ability type '{actionDef.type}' on '{actionDef.id}'. Defaulted to Attack.");
                abilityType = AbilityType.Attack;
            }

            int resolvedDamage = Mathf.Max(0, actionDef.ResolveDamage());
            var actionInstance = new ActionInstance
            {
                actionDefId = actionDef.id,
                abilityType = abilityType,
                cooldownTurns = Mathf.Max(0, actionDef.cooldown),
                cooldownRemaining = 0,
                attack = resolvedDamage,
                baseRoll = 0,
                attackResult = 0
            };

            if (actionDef.tags != null && actionDef.tags.Count > 0)
            {
                actionInstance.tags.AddRange(actionDef.tags);
            }

            actionInstance.EnsureInitialized();
            return actionInstance;
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

            return clash.opponentActionIds.Count < clash.slotLimit.Value;
        }
    }
}
