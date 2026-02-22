using System;
using System.Collections.Generic;
using System.Linq;
using Game.Domain.Battle;
using Game.Infrastructure.Data;
using UnityEngine;

namespace Game.Application.Battle
{
    public readonly struct EnemyDeployBuildResult
    {
        public int deployedCount { get; }
        public int skippedCount { get; }

        public EnemyDeployBuildResult(int deployedCount, int skippedCount)
        {
            this.deployedCount = deployedCount;
            this.skippedCount = skippedCount;
        }
    }

    public sealed class BattleSessionBuilder
    {
        readonly GameDatabase database;

        public BattleSessionBuilder(GameDatabase database)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
        }

        public bool TryCreateInitialState(
            string encounterId,
            out BattleState state,
            out string failureMessage)
        {
            state = null;
            failureMessage = string.Empty;

            if (database.battleConfig == null)
            {
                failureMessage = "battle_config is missing.";
                return false;
            }

            if (database.playerStart == null)
            {
                failureMessage = "player_start is missing.";
                return false;
            }

            if (database.runConfig == null)
            {
                failureMessage = "run_config is missing.";
                return false;
            }

            if (database.encountersById == null)
            {
                failureMessage = "encounters table is missing.";
                return false;
            }

            if (database.battlefieldsById == null)
            {
                failureMessage = "battlefields table is missing.";
                return false;
            }

            if (database.troopsById == null)
            {
                failureMessage = "troops table is missing.";
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

            state = CreateInitialBattleState(encounterDef);
            return true;
        }

        public EnemyDeployBuildResult AutoDeployEnemyIntent(BattleState state)
        {
            if (state == null)
            {
                return new EnemyDeployBuildResult(0, 0);
            }

            state.EnsureInitialized();

            if (database.troopsById == null)
            {
                int skipped = state.enemyIntent == null ? 0 : state.enemyIntent.Count;
                Debug.LogWarning("[BattleSessionBuilder] Enemy deploy skipped: troops table is missing.");
                return new EnemyDeployBuildResult(0, skipped);
            }

            int deployedCount = 0;
            int skippedCount = 0;

            for (int i = 0; i < state.enemyIntent.Count; i++)
            {
                EnemyIntentEntry intent = state.enemyIntent[i];
                if (intent == null)
                {
                    skippedCount += 1;
                    Debug.LogWarning($"[BattleSessionBuilder] enemyIntent[{i}] is null.");
                    continue;
                }

                if (!database.troopsById.TryGetValue(intent.troopDefId, out TroopDef troopDef) || troopDef == null)
                {
                    skippedCount += Mathf.Max(1, intent.count);
                    Debug.LogWarning($"[BattleSessionBuilder] troopDef('{intent.troopDefId}') is missing.");
                    continue;
                }

                int requestedCount = Mathf.Max(0, intent.count);
                for (int copyIndex = 0; copyIndex < requestedCount; copyIndex++)
                {
                    if (!TryFindEnemyBattlefieldForDeploy(
                            state,
                            intent.battlefieldIndex,
                            out int deployBattlefieldIndex))
                    {
                        skippedCount += 1;
                        Debug.LogWarning(
                            $"[BattleSessionBuilder] no available battlefield slot for troopDef('{intent.troopDefId}').");
                        continue;
                    }

                    BattlefieldState deployBattlefield = state.battlefields[deployBattlefieldIndex];
                    deployBattlefield.EnsureInitialized();

                    TroopInstance troopInstance = CreateTroopInstance(troopDef);
                    state.troopsById[troopInstance.instanceId] = troopInstance;
                    deployBattlefield.enemyTroopIds.Add(troopInstance.instanceId);
                    deployedCount += 1;
                }
            }

            return new EnemyDeployBuildResult(deployedCount, skippedCount);
        }

        BattleState CreateInitialBattleState(EncounterDef encounterDef)
        {
            int manaMax = Mathf.Max(0, database.battleConfig.manaMax);
            int startingMana = Mathf.Clamp(database.playerStart.startingMana, 0, manaMax);

            var nextState = new BattleState
            {
                turnIndex = 0,
                isBattleEnded = false,
                mana = startingMana,
                stability = database.playerStart.startingStability,
                playerMorale = Mathf.Max(1, database.playerStart.startingPlayerMorale),
                enemyMorale = Mathf.Max(1, encounterDef.enemyMorale)
            };

            nextState.cooldowns.Clear();
            nextState.campTroopIds.Clear();
            nextState.troopsById.Clear();
            nextState.enemyIntent.Clear();

            InitializeBattlefieldSlots(nextState);
            PopulateEnemyIntent(nextState, encounterDef);
            PopulateCampFromPlayerStart(nextState);

            return nextState;
        }

        void InitializeBattlefieldSlots(BattleState state)
        {
            state.battlefields.Clear();

            var orderedDefs = new List<BattlefieldDef>();
            if (database.battlefieldsById != null)
            {
                orderedDefs = database.battlefieldsById
                    .Values
                    .OrderBy(def => def.id, StringComparer.Ordinal)
                    .ToList();
            }

            int targetCount = Mathf.Max(1, database.battleConfig.battlefieldCount);
            for (int i = 0; i < targetCount; i++)
            {
                BattlefieldDef sourceDef = i < orderedDefs.Count ? orderedDefs[i] : null;
                int? resolvedSlotLimit = sourceDef != null
                    ? sourceDef.slotLimit
                    : database.battleConfig.p0Rules.defaultSlotLimit;

                var field = new BattlefieldState
                {
                    battlefieldId = sourceDef?.id ?? $"missing_battlefield_{i}",
                    slotLimit = resolvedSlotLimit,
                    totalAttackBonusPlayer = 0,
                    totalAttackBonusEnemy = 0
                };

                field.EnsureInitialized();
                state.battlefields.Add(field);
            }

            state.EnsureInitialized();
        }

        void PopulateEnemyIntent(BattleState state, EncounterDef encounterDef)
        {
            if (encounterDef.plans == null)
            {
                return;
            }

            for (int planIndex = 0; planIndex < encounterDef.plans.Count; planIndex++)
            {
                EncounterPlanDef plan = encounterDef.plans[planIndex];
                if (plan == null || plan.troops == null)
                {
                    continue;
                }

                for (int troopIndex = 0; troopIndex < plan.troops.Count; troopIndex++)
                {
                    SummonTroopRefDef troop = plan.troops[troopIndex];
                    if (troop == null || troop.count <= 0 || string.IsNullOrWhiteSpace(troop.troopId))
                    {
                        continue;
                    }

                    state.enemyIntent.Add(new EnemyIntentEntry
                    {
                        battlefieldIndex = plan.battlefieldIndex,
                        troopDefId = troop.troopId,
                        count = troop.count
                    });
                }
            }
        }

        void PopulateCampFromPlayerStart(BattleState state)
        {
            if (database.playerStart.startingSquadCardIds == null)
            {
                Debug.LogWarning("[BattleSessionBuilder] startingSquadCardIds is missing.");
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

                if (squadCard.battleStart == null || squadCard.battleStart.summonTroops == null)
                {
                    continue;
                }

                for (int summonIndex = 0; summonIndex < squadCard.battleStart.summonTroops.Count; summonIndex++)
                {
                    SummonTroopRefDef summon = squadCard.battleStart.summonTroops[summonIndex];
                    if (summon == null || summon.count <= 0 || string.IsNullOrWhiteSpace(summon.troopId))
                    {
                        continue;
                    }

                    if (!database.troopsById.TryGetValue(summon.troopId, out TroopDef troopDef) || troopDef == null)
                    {
                        continue;
                    }

                    for (int i = 0; i < summon.count; i++)
                    {
                        TroopInstance troopInstance = CreateTroopInstance(troopDef);
                        state.troopsById[troopInstance.instanceId] = troopInstance;
                        state.campTroopIds.Add(troopInstance.instanceId);
                    }
                }
            }
        }

        static TroopInstance CreateTroopInstance(TroopDef troopDef)
        {
            var troopInstance = new TroopInstance
            {
                troopDefId = troopDef.id,
                attack = Mathf.Max(1, troopDef.attack),
                baseRoll = 0,
                attackResult = 0
            };

            if (troopDef.tags != null && troopDef.tags.Count > 0)
            {
                troopInstance.tags.AddRange(troopDef.tags);
            }

            troopInstance.EnsureInitialized();
            return troopInstance;
        }

        static bool TryFindEnemyBattlefieldForDeploy(
            BattleState state,
            int preferredBattlefieldIndex,
            out int resolvedBattlefieldIndex)
        {
            resolvedBattlefieldIndex = -1;

            if (CanDeployEnemyToBattlefield(state, preferredBattlefieldIndex))
            {
                resolvedBattlefieldIndex = preferredBattlefieldIndex;
                return true;
            }

            for (int battlefieldIndex = 0; battlefieldIndex < state.battlefields.Count; battlefieldIndex++)
            {
                if (battlefieldIndex == preferredBattlefieldIndex)
                {
                    continue;
                }

                if (!CanDeployEnemyToBattlefield(state, battlefieldIndex))
                {
                    continue;
                }

                resolvedBattlefieldIndex = battlefieldIndex;
                return true;
            }

            return false;
        }

        static bool CanDeployEnemyToBattlefield(BattleState state, int battlefieldIndex)
        {
            if (state == null || state.battlefields == null)
            {
                return false;
            }

            if (battlefieldIndex < 0 || battlefieldIndex >= state.battlefields.Count)
            {
                return false;
            }

            BattlefieldState battlefield = state.battlefields[battlefieldIndex];
            if (battlefield == null)
            {
                return false;
            }

            battlefield.EnsureInitialized();
            if (!battlefield.slotLimit.HasValue)
            {
                return true;
            }

            return battlefield.enemyTroopIds.Count < battlefield.slotLimit.Value;
        }
    }
}
