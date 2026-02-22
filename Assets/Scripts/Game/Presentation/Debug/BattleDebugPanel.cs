using Game.Application.Battle;
using Game.Domain.Battle;
using Game.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Debug
{
    public sealed class BattleDebugPanel : MonoBehaviour
    {
        const string debugEncounterId = "enc_debug_01";

        [Header("Status")]
        [SerializeField] TMP_Text phaseText;
        [SerializeField] TMP_Text turnText;
        [SerializeField] TMP_Text manaText;
        [SerializeField] TMP_Text stabilityText;
        [SerializeField] TMP_Text playerMoraleText;
        [SerializeField] TMP_Text enemyMoraleText;
        [SerializeField] TMP_Text selectedTroopText;
        [SerializeField] TMP_Text selectedBattlefieldText;

        [Header("Battlefields")]
        [SerializeField] TMP_Text battlefield0Text;
        [SerializeField] TMP_Text battlefield1Text;
        [SerializeField] TMP_Text battlefield2Text;

        [Header("Actions")]
        [SerializeField] Button startBattleButton;
        [SerializeField] Button enemyDeployButton;
        [SerializeField] Button playerDeployButton;
        [SerializeField] Button deploySelectedButton;
        [SerializeField] Button rollButton;
        [SerializeField] Button resolveButton;
        [SerializeField] Button retreatButton;

        [Header("Debug")]
        [SerializeField] TMP_Text logText;

        BattleState battleState;
        BattlePhaseRunner phaseRunner;
        string selectedTroopId = string.Empty;
        int selectedBattlefieldIndex = -1;

        public BattleState BattleState => battleState;
        public BattlePhaseRunner PhaseRunner => phaseRunner;

        void Awake()
        {
            ResetContext();
            RefreshStubTexts();
            RefreshStubButtons();
        }

        public void StartBattle()
        {
            if (!TryCreateBattleContextFromData(
                    out BattleState nextState,
                    out GameDatabase sourceDatabase,
                    out string failureMessage))
            {
                AppendLog($"StartBattle failed: {failureMessage}");
                return;
            }

            battleState = nextState;
            phaseRunner = new BattlePhaseRunner(battleState);

            bool started = phaseRunner.StartBattle();
            if (!started)
            {
                AppendLog($"StartBattle rejected: {phaseRunner.LastFailureReason}");
                return;
            }

            AutoDeployEnemyIntent(battleState, sourceDatabase);

            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog($"StartBattle success: encounter={debugEncounterId}");
        }

        public void EnemyDeploy()
        {
            EnsureContextInitialized();
            AppendLog("EnemyDeploy clicked.");
        }

        public void PlayerDeploy()
        {
            EnsureContextInitialized();
            AppendLog("PlayerDeploy clicked.");
        }

        public void DeploySelected()
        {
            EnsureContextInitialized();
            AppendLog("DeploySelected clicked.");
        }

        public void Roll()
        {
            EnsureContextInitialized();
            AppendLog("Roll clicked.");
        }

        public void Resolve()
        {
            EnsureContextInitialized();
            AppendLog("Resolve clicked.");
        }

        public void Retreat()
        {
            EnsureContextInitialized();
            AppendLog("Retreat clicked.");
        }

        public void SelectTroop(string troopId)
        {
            EnsureContextInitialized();
            selectedTroopId = troopId ?? string.Empty;
            RefreshStubTexts();
        }

        public void SelectBattlefield(int battlefieldIndex)
        {
            EnsureContextInitialized();
            selectedBattlefieldIndex = battlefieldIndex;
            RefreshStubTexts();
        }

        public void ResetBattleContext()
        {
            ResetContext();
            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog("Battle context reset.");
        }

        void RefreshStubTexts()
        {
            EnsureContextInitialized();

            string phaseLabel = phaseRunner == null ? "(none)" : phaseRunner.currentPhase.ToString();

            SetText(phaseText, $"Phase: {phaseLabel}");
            SetText(turnText, $"Turn: {battleState.turnIndex}");
            SetText(manaText, $"Mana: {battleState.mana}");
            SetText(stabilityText, $"Stability: {battleState.stability}");
            SetText(playerMoraleText, $"Player Morale: {battleState.playerMorale}");
            SetText(enemyMoraleText, $"Enemy Morale: {battleState.enemyMorale}");
            SetText(selectedTroopText, $"Selected Troop: {selectedTroopId}");
            SetText(selectedBattlefieldText, $"Selected Battlefield: {selectedBattlefieldIndex}");

            SetText(battlefield0Text, BuildBattlefieldStubLine(0));
            SetText(battlefield1Text, BuildBattlefieldStubLine(1));
            SetText(battlefield2Text, BuildBattlefieldStubLine(2));
        }

        void RefreshStubButtons()
        {
            SetButtonInteractable(startBattleButton, true);
            SetButtonInteractable(enemyDeployButton, true);
            SetButtonInteractable(playerDeployButton, true);
            SetButtonInteractable(deploySelectedButton, true);
            SetButtonInteractable(rollButton, true);
            SetButtonInteractable(resolveButton, true);
            SetButtonInteractable(retreatButton, true);
        }

        void ResetContext()
        {
            battleState = new BattleState();
            phaseRunner = new BattlePhaseRunner(battleState);
            selectedTroopId = string.Empty;
            selectedBattlefieldIndex = -1;
        }

        void EnsureContextInitialized()
        {
            if (battleState == null || phaseRunner == null)
            {
                ResetContext();
                AppendLog("Context auto-initialized.");
            }
        }

        string BuildBattlefieldStubLine(int index)
        {
            if (index < 0 || index >= battleState.battlefields.Count)
            {
                return $"Battlefield {index}: (missing)";
            }

            BattlefieldState battlefield = battleState.battlefields[index];
            battlefield.EnsureInitialized();

            return $"Battlefield {index} | P:{battlefield.playerTroopIds.Count} E:{battlefield.enemyTroopIds.Count}";
        }

        bool TryCreateBattleContextFromData(
            out BattleState nextState,
            out GameDatabase sourceDatabase,
            out string failureMessage)
        {
            nextState = null;
            sourceDatabase = null;
            failureMessage = string.Empty;

            GameDatabase database = GameDataRuntime.CurrentDatabase;
            if (database == null)
            {
                failureMessage = "GameDataRuntime.CurrentDatabase is null.";
                return false;
            }

            if (database.battleConfig == null)
            {
                failureMessage = "battle_config is missing.";
                return false;
            }

            if (database.runConfig == null)
            {
                failureMessage = "run_config is missing.";
                return false;
            }

            if (!database.encountersById.TryGetValue(debugEncounterId, out EncounterDef encounterDef) ||
                encounterDef == null)
            {
                failureMessage = $"encounter('{debugEncounterId}') is missing.";
                return false;
            }

            sourceDatabase = database;
            nextState = CreateInitialBattleState(database, encounterDef);
            return true;
        }

        BattleState CreateInitialBattleState(GameDatabase database, EncounterDef encounterDef)
        {
            var nextState = new BattleState
            {
                turnIndex = 0,
                isBattleEnded = false,
                mana = database.battleConfig.manaMax,
                stability = database.runConfig.startingStability,
                playerMorale = Mathf.Max(1, encounterDef.enemyMorale),
                enemyMorale = Mathf.Max(1, encounterDef.enemyMorale)
            };

            nextState.cooldowns.Clear();
            nextState.campTroopIds.Clear();
            nextState.troopsById.Clear();
            nextState.enemyIntent.Clear();

            InitializeBattlefieldSlots(nextState, database);
            PopulateEnemyIntent(nextState, encounterDef);
            PopulateCampFromSquadCards(nextState, database);

            return nextState;
        }

        void InitializeBattlefieldSlots(BattleState nextState, GameDatabase database)
        {
            nextState.battlefields.Clear();

            List<BattlefieldDef> orderedDefs = database.battlefieldsById
                .Values
                .OrderBy(def => def.id, StringComparer.Ordinal)
                .ToList();

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
                nextState.battlefields.Add(field);
            }

            nextState.EnsureInitialized();
        }

        void PopulateEnemyIntent(BattleState nextState, EncounterDef encounterDef)
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

                    nextState.enemyIntent.Add(new EnemyIntentEntry
                    {
                        battlefieldIndex = plan.battlefieldIndex,
                        troopDefId = troop.troopId,
                        count = troop.count
                    });
                }
            }
        }

        void PopulateCampFromSquadCards(BattleState nextState, GameDatabase database)
        {
            List<CardDef> squadCards = database.cardsById
                .Values
                .Where(card => card != null && string.Equals(card.type, "Squad", StringComparison.Ordinal))
                .OrderBy(card => card.id, StringComparer.Ordinal)
                .ToList();

            int addedTroopCount = 0;
            for (int cardIndex = 0; cardIndex < squadCards.Count; cardIndex++)
            {
                CardDef squadCard = squadCards[cardIndex];
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
                        AppendLog($"StartBattle warning: troopDef('{summon.troopId}') is missing.");
                        continue;
                    }

                    for (int i = 0; i < summon.count; i++)
                    {
                        TroopInstance troopInstance = CreateTroopInstance(troopDef);
                        nextState.troopsById[troopInstance.instanceId] = troopInstance;
                        nextState.campTroopIds.Add(troopInstance.instanceId);
                        addedTroopCount += 1;
                    }
                }
            }

            if (addedTroopCount > 0)
            {
                return;
            }

            AppendLog("StartBattle warning: no Squad troops found, using one copy per troopDef.");

            foreach (TroopDef troopDef in database.troopsById.Values.OrderBy(def => def.id, StringComparer.Ordinal))
            {
                TroopInstance troopInstance = CreateTroopInstance(troopDef);
                nextState.troopsById[troopInstance.instanceId] = troopInstance;
                nextState.campTroopIds.Add(troopInstance.instanceId);
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

        void AutoDeployEnemyIntent(BattleState nextState, GameDatabase sourceDatabase)
        {
            if (nextState == null)
            {
                AppendLog("Enemy auto deploy skipped: battleState is null.");
                return;
            }

            if (sourceDatabase == null)
            {
                AppendLog("Enemy auto deploy skipped: sourceDatabase is null.");
                return;
            }

            int deployedCount = 0;
            int skippedCount = 0;

            for (int i = 0; i < nextState.enemyIntent.Count; i++)
            {
                EnemyIntentEntry intent = nextState.enemyIntent[i];
                if (intent == null)
                {
                    skippedCount += 1;
                    AppendLog($"Enemy auto deploy warning: enemyIntent[{i}] is null.");
                    continue;
                }

                if (intent.battlefieldIndex < 0 || intent.battlefieldIndex >= nextState.battlefields.Count)
                {
                    skippedCount += Mathf.Max(1, intent.count);
                    AppendLog(
                        $"Enemy auto deploy warning: battlefieldIndex({intent.battlefieldIndex}) is out of range.");
                    continue;
                }

                if (!sourceDatabase.troopsById.TryGetValue(intent.troopDefId, out TroopDef troopDef) || troopDef == null)
                {
                    skippedCount += Mathf.Max(1, intent.count);
                    AppendLog($"Enemy auto deploy warning: troopDef('{intent.troopDefId}') is missing.");
                    continue;
                }

                BattlefieldState battlefield = nextState.battlefields[intent.battlefieldIndex];
                battlefield.EnsureInitialized();

                int requestedCount = Mathf.Max(0, intent.count);
                for (int copyIndex = 0; copyIndex < requestedCount; copyIndex++)
                {
                    if (battlefield.slotLimit.HasValue &&
                        battlefield.enemyTroopIds.Count >= battlefield.slotLimit.Value)
                    {
                        skippedCount += 1;
                        AppendLog(
                            $"Enemy auto deploy warning: slotLimit exceeded at battlefield({intent.battlefieldIndex}).");
                        continue;
                    }

                    TroopInstance troopInstance = CreateTroopInstance(troopDef);
                    nextState.troopsById[troopInstance.instanceId] = troopInstance;
                    battlefield.enemyTroopIds.Add(troopInstance.instanceId);
                    deployedCount += 1;
                }
            }

            AppendLog(
                $"Enemy auto deploy complete: deployed={deployedCount}, skipped={skippedCount}");
        }

        void AppendLog(string message)
        {
            if (logText != null)
            {
                if (string.IsNullOrEmpty(logText.text))
                {
                    logText.text = message;
                }
                else
                {
                    logText.text = $"{logText.text}\n{message}";
                }
            }

            UnityEngine.Debug.Log($"[BattleDebugPanel] {message}");
        }

        static void SetText(TMP_Text target, string value)
        {
            if (target == null)
            {
                return;
            }

            target.text = value;
        }

        static void SetButtonInteractable(Button button, bool isInteractable)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = isInteractable;
        }
    }
}
