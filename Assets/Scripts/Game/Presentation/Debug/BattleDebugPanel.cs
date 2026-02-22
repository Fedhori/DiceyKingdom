using Game.Application.Battle;
using Game.Domain.Battle;
using Game.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
            AutoBindUiReferencesByName();
            WireDefaultButtonCallbacks();
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
                RejectAction("StartBattle", failureMessage);
                return;
            }

            battleState = nextState;
            phaseRunner = new BattlePhaseRunner(battleState);

            bool started = phaseRunner.StartBattle();
            if (!started)
            {
                RejectAction("StartBattle", phaseRunner.LastFailureReason.ToString());
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

            if (!phaseRunner.isStarted)
            {
                RejectAction("EnemyDeploy", "battle is not started.");
                return;
            }

            if (phaseRunner.currentPhase != BattlePhase.Recall)
            {
                RejectAction(
                    "EnemyDeploy",
                    $"current phase is {phaseRunner.currentPhase}, required phase is {BattlePhase.Recall}.");
                return;
            }

            if (!phaseRunner.AdvanceToNextPhase())
            {
                RejectAction("EnemyDeploy", phaseRunner.LastFailureReason.ToString());
                return;
            }

            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog("EnemyDeploy phase entered.");
        }

        public void PlayerDeploy()
        {
            EnsureContextInitialized();

            if (!phaseRunner.isStarted)
            {
                RejectAction("PlayerDeploy", "battle is not started.");
                return;
            }

            if (phaseRunner.currentPhase != BattlePhase.EnemyDeploy)
            {
                RejectAction(
                    "PlayerDeploy",
                    $"current phase is {phaseRunner.currentPhase}, required phase is {BattlePhase.EnemyDeploy}.");
                return;
            }

            if (!phaseRunner.AdvanceToNextPhase())
            {
                RejectAction("PlayerDeploy", phaseRunner.LastFailureReason.ToString());
                return;
            }

            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog("PlayerDeploy phase entered.");
        }

        public void DeploySelected()
        {
            EnsureContextInitialized();

            if (!TryDeploySelectedTroop(out string failureMessage))
            {
                RejectAction("DeploySelected", failureMessage);
                return;
            }

            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog(
                $"DeploySelected success: troop={selectedTroopId}, battlefield={selectedBattlefieldIndex}");
        }

        public void Roll()
        {
            EnsureContextInitialized();

            if (!TryRollAllDeployedTroops(out int rolledCount, out string failureMessage))
            {
                RejectAction("Roll", failureMessage);
                return;
            }

            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog($"Roll success: rolledTroops={rolledCount}");
        }

        public void Resolve()
        {
            EnsureContextInitialized();

            if (!TryResolveAllBattlefields(out int resolvedCount, out string failureMessage))
            {
                RejectAction("Resolve", failureMessage);
                return;
            }

            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog($"Resolve success: resolvedBattlefields={resolvedCount}");
        }

        public void Retreat()
        {
            EnsureContextInitialized();

            if (!TryRetreatBattle(out string failureMessage))
            {
                RejectAction("Retreat", failureMessage);
                return;
            }

            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog("Retreat success: battle ended.");
        }

        public void SelectTroop(string troopId)
        {
            EnsureContextInitialized();

            if (string.IsNullOrWhiteSpace(troopId))
            {
                selectedTroopId = string.Empty;
                RejectAction("SelectTroop", "troopId is empty.");
                return;
            }

            if (!battleState.troopsById.ContainsKey(troopId))
            {
                RejectAction("SelectTroop", $"troopId({troopId}) does not exist.");
                return;
            }

            if (!battleState.campTroopIds.Contains(troopId))
            {
                RejectAction("SelectTroop", $"troopId({troopId}) is not in camp.");
                return;
            }

            selectedTroopId = troopId;
            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog($"Troop selected: {selectedTroopId}");
        }

        public void SelectBattlefield(int battlefieldIndex)
        {
            EnsureContextInitialized();

            if (battlefieldIndex < 0 || battlefieldIndex >= battleState.battlefields.Count)
            {
                RejectAction(
                    "SelectBattlefield",
                    $"battlefieldIndex({battlefieldIndex}) is out of range.");
                return;
            }

            selectedBattlefieldIndex = battlefieldIndex;
            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog($"Battlefield selected: {selectedBattlefieldIndex}");
        }

        public void SelectFirstCampTroop()
        {
            EnsureContextInitialized();

            if (!phaseRunner.isStarted)
            {
                RejectAction("SelectFirstCampTroop", "battle is not started.");
                return;
            }

            if (battleState.campTroopIds == null || battleState.campTroopIds.Count <= 0)
            {
                RejectAction("SelectFirstCampTroop", "no troop exists in camp.");
                return;
            }

            SelectTroop(battleState.campTroopIds[0]);
        }

        public void SelectBattlefield0()
        {
            SelectBattlefield(0);
        }

        public void SelectBattlefield1()
        {
            SelectBattlefield(1);
        }

        public void SelectBattlefield2()
        {
            SelectBattlefield(2);
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
            GameDatabase currentDatabase = GameDataRuntime.CurrentDatabase;

            SetText(phaseText, BattleDebugPanelFormatter.FormatPhase(phaseRunner));
            SetText(turnText, BattleDebugPanelFormatter.FormatTurn(battleState));
            SetText(manaText, BattleDebugPanelFormatter.FormatMana(battleState));
            SetText(stabilityText, BattleDebugPanelFormatter.FormatStability(battleState));
            SetText(playerMoraleText, BattleDebugPanelFormatter.FormatPlayerMorale(battleState));
            SetText(enemyMoraleText, BattleDebugPanelFormatter.FormatEnemyMorale(battleState));
            SetText(selectedTroopText, BattleDebugPanelFormatter.FormatSelectedTroop(battleState, selectedTroopId));
            SetText(
                selectedBattlefieldText,
                BattleDebugPanelFormatter.FormatSelectedBattlefield(battleState, selectedBattlefieldIndex));

            SetText(battlefield0Text, BattleDebugPanelFormatter.FormatBattlefield(battleState, 0, currentDatabase));
            SetText(battlefield1Text, BattleDebugPanelFormatter.FormatBattlefield(battleState, 1, currentDatabase));
            SetText(battlefield2Text, BattleDebugPanelFormatter.FormatBattlefield(battleState, 2, currentDatabase));
        }

        void RefreshStubButtons()
        {
            EnsureContextInitialized();

            bool isStarted = phaseRunner.isStarted;
            bool isEnded = battleState.isBattleEnded;
            BattlePhase currentPhase = phaseRunner.currentPhase;

            bool hasSelectedTroop = !string.IsNullOrWhiteSpace(selectedTroopId) &&
                                    battleState.campTroopIds.Contains(selectedTroopId);
            bool hasSelectedBattlefield = selectedBattlefieldIndex >= 0 &&
                                          selectedBattlefieldIndex < battleState.battlefields.Count;

            bool canProgress = isStarted && !isEnded;

            SetButtonInteractable(startBattleButton, !isStarted || isEnded);
            SetButtonInteractable(enemyDeployButton, canProgress && currentPhase == BattlePhase.Recall);
            SetButtonInteractable(playerDeployButton, canProgress && currentPhase == BattlePhase.EnemyDeploy);
            SetButtonInteractable(
                deploySelectedButton,
                canProgress &&
                currentPhase == BattlePhase.PlayerDeploy &&
                hasSelectedTroop &&
                hasSelectedBattlefield);
            SetButtonInteractable(
                rollButton,
                canProgress &&
                (currentPhase == BattlePhase.PlayerDeploy || currentPhase == BattlePhase.Roll));
            SetButtonInteractable(
                resolveButton,
                canProgress &&
                (currentPhase == BattlePhase.Tactics || currentPhase == BattlePhase.Resolve));
            SetButtonInteractable(
                retreatButton,
                canProgress &&
                currentPhase == BattlePhase.PlayerDeploy &&
                battleState.stability > 0);
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

        void AutoBindUiReferencesByName()
        {
            phaseText = ResolveBinding(phaseText, "PhaseText");
            turnText = ResolveBinding(turnText, "TurnText");
            manaText = ResolveBinding(manaText, "ManaText");
            stabilityText = ResolveBinding(stabilityText, "StabilityText");
            playerMoraleText = ResolveBinding(playerMoraleText, "PlayerMoraleText");
            enemyMoraleText = ResolveBinding(enemyMoraleText, "EnemyMoraleText");
            selectedTroopText = ResolveBinding(selectedTroopText, "SelectedTroopText");
            selectedBattlefieldText = ResolveBinding(selectedBattlefieldText, "SelectedBattlefieldText");

            battlefield0Text = ResolveBinding(battlefield0Text, "Battlefield0Text");
            battlefield1Text = ResolveBinding(battlefield1Text, "Battlefield1Text");
            battlefield2Text = ResolveBinding(battlefield2Text, "Battlefield2Text");

            startBattleButton = ResolveBinding(startBattleButton, "StartBattleButton");
            enemyDeployButton = ResolveBinding(enemyDeployButton, "EnemyDeployButton");
            playerDeployButton = ResolveBinding(playerDeployButton, "PlayerDeployButton");
            deploySelectedButton = ResolveBinding(deploySelectedButton, "DeploySelectedButton");
            rollButton = ResolveBinding(rollButton, "RollButton");
            resolveButton = ResolveBinding(resolveButton, "ResolveButton");
            retreatButton = ResolveBinding(retreatButton, "RetreatButton");

            logText = ResolveBinding(logText, "LogText");
        }

        void WireDefaultButtonCallbacks()
        {
            WireButton(startBattleButton, StartBattle);
            WireButton(enemyDeployButton, EnemyDeploy);
            WireButton(playerDeployButton, PlayerDeploy);
            WireButton(deploySelectedButton, DeploySelected);
            WireButton(rollButton, Roll);
            WireButton(resolveButton, Resolve);
            WireButton(retreatButton, Retreat);

            WireOptionalButton("SelectFirstCampTroopButton", SelectFirstCampTroop);
            WireOptionalButton("SelectBattlefield0Button", SelectBattlefield0);
            WireOptionalButton("SelectBattlefield1Button", SelectBattlefield1);
            WireOptionalButton("SelectBattlefield2Button", SelectBattlefield2);
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
                        WarnAndLog($"StartBattle warning: troopDef('{summon.troopId}') is missing.");
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

            WarnAndLog("StartBattle warning: no Squad troops found, using one copy per troopDef.");

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
                WarnAndLog("Enemy auto deploy skipped: battleState is null.");
                return;
            }

            if (sourceDatabase == null)
            {
                WarnAndLog("Enemy auto deploy skipped: sourceDatabase is null.");
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
                    WarnAndLog($"Enemy auto deploy warning: enemyIntent[{i}] is null.");
                    continue;
                }

                if (intent.battlefieldIndex < 0 || intent.battlefieldIndex >= nextState.battlefields.Count)
                {
                    skippedCount += Mathf.Max(1, intent.count);
                    WarnAndLog(
                        $"Enemy auto deploy warning: battlefieldIndex({intent.battlefieldIndex}) is out of range.");
                    continue;
                }

                if (!sourceDatabase.troopsById.TryGetValue(intent.troopDefId, out TroopDef troopDef) || troopDef == null)
                {
                    skippedCount += Mathf.Max(1, intent.count);
                    WarnAndLog($"Enemy auto deploy warning: troopDef('{intent.troopDefId}') is missing.");
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
                        WarnAndLog(
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

        bool TryDeploySelectedTroop(out string failureMessage)
        {
            failureMessage = string.Empty;

            if (!phaseRunner.isStarted)
            {
                failureMessage = "battle is not started.";
                return false;
            }

            if (phaseRunner.currentPhase != BattlePhase.PlayerDeploy)
            {
                failureMessage =
                    $"current phase is {phaseRunner.currentPhase}, required phase is {BattlePhase.PlayerDeploy}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(selectedTroopId))
            {
                failureMessage = "selectedTroopId is empty.";
                return false;
            }

            if (selectedBattlefieldIndex < 0 || selectedBattlefieldIndex >= battleState.battlefields.Count)
            {
                failureMessage = $"selectedBattlefieldIndex({selectedBattlefieldIndex}) is out of range.";
                return false;
            }

            if (!battleState.troopsById.TryGetValue(selectedTroopId, out TroopInstance troopInstance) || troopInstance == null)
            {
                failureMessage = $"selected troop({selectedTroopId}) does not exist.";
                return false;
            }

            if (!battleState.campTroopIds.Remove(selectedTroopId))
            {
                failureMessage = $"selected troop({selectedTroopId}) is not in camp.";
                return false;
            }

            BattlefieldState targetField = battleState.battlefields[selectedBattlefieldIndex];
            targetField.EnsureInitialized();

            if (targetField.slotLimit.HasValue &&
                targetField.playerTroopIds.Count >= targetField.slotLimit.Value)
            {
                battleState.campTroopIds.Add(selectedTroopId);
                failureMessage = $"target battlefield({selectedBattlefieldIndex}) slotLimit exceeded.";
                return false;
            }

            targetField.playerTroopIds.Add(selectedTroopId);
            return true;
        }

        bool TryRollAllDeployedTroops(out int rolledCount, out string failureMessage)
        {
            rolledCount = 0;
            failureMessage = string.Empty;

            if (!phaseRunner.isStarted)
            {
                failureMessage = "battle is not started.";
                return false;
            }

            if (battleState.isBattleEnded)
            {
                failureMessage = "battle already ended.";
                return false;
            }

            if (phaseRunner.currentPhase == BattlePhase.PlayerDeploy)
            {
                if (!phaseRunner.AdvanceToNextPhase())
                {
                    failureMessage = $"failed to enter Roll phase ({phaseRunner.LastFailureReason}).";
                    return false;
                }

                AppendLog("Roll info: advanced from PlayerDeploy to Roll.");
            }

            if (phaseRunner.currentPhase != BattlePhase.Roll)
            {
                failureMessage = $"current phase is {phaseRunner.currentPhase}, required phase is {BattlePhase.Roll}.";
                return false;
            }

            var deployedTroopIds = new HashSet<string>(StringComparer.Ordinal);
            for (int battlefieldIndex = 0; battlefieldIndex < battleState.battlefields.Count; battlefieldIndex++)
            {
                BattlefieldState battlefield = battleState.battlefields[battlefieldIndex];
                if (battlefield == null)
                {
                    WarnAndLog($"Roll warning: battlefields[{battlefieldIndex}] is null.");
                    continue;
                }

                battlefield.EnsureInitialized();
                CollectTroopIds(deployedTroopIds, battlefield.playerTroopIds, $"playerTroopIds[{battlefieldIndex}]");
                CollectTroopIds(deployedTroopIds, battlefield.enemyTroopIds, $"enemyTroopIds[{battlefieldIndex}]");
            }

            if (deployedTroopIds.Count == 0)
            {
                failureMessage = "no deployed troops to roll.";
                return false;
            }

            foreach (string troopId in deployedTroopIds)
            {
                if (!battleState.troopsById.TryGetValue(troopId, out TroopInstance troop) || troop == null)
                {
                    WarnAndLog($"Roll warning: troopId({troopId}) does not exist.");
                    continue;
                }

                BattleSimulator.RollTroop(troop);
                rolledCount += 1;
            }

            if (rolledCount <= 0)
            {
                failureMessage = "all deployed troops were invalid.";
                return false;
            }

            if (!phaseRunner.AdvanceToNextPhase())
            {
                WarnAndLog($"Roll warning: failed to move to next phase ({phaseRunner.LastFailureReason}).");
            }

            return true;
        }

        bool TryResolveAllBattlefields(out int resolvedCount, out string failureMessage)
        {
            resolvedCount = 0;
            failureMessage = string.Empty;

            if (!phaseRunner.isStarted)
            {
                failureMessage = "battle is not started.";
                return false;
            }

            if (battleState.isBattleEnded)
            {
                failureMessage = "battle already ended.";
                return false;
            }

            if (phaseRunner.currentPhase == BattlePhase.Tactics)
            {
                if (!phaseRunner.AdvanceToNextPhase())
                {
                    failureMessage = $"failed to enter Resolve phase ({phaseRunner.LastFailureReason}).";
                    return false;
                }

                AppendLog("Resolve info: Tactics phase skipped (no tactics UI in T6).");
            }

            if (phaseRunner.currentPhase != BattlePhase.Resolve)
            {
                failureMessage = $"current phase is {phaseRunner.currentPhase}, required phase is {BattlePhase.Resolve}.";
                return false;
            }

            for (int battlefieldIndex = 0; battlefieldIndex < battleState.battlefields.Count; battlefieldIndex++)
            {
                bool resolved = BattleSimulator.ResolveBattlefield(
                    battleState,
                    battlefieldIndex,
                    out BattleOutcome outcome,
                    out int playerTotalAttack,
                    out int enemyTotalAttack);

                if (!resolved)
                {
                    if (resolvedCount == 0)
                    {
                        failureMessage = $"resolve failed at battlefieldIndex({battlefieldIndex}).";
                        return false;
                    }

                    WarnAndLog($"Resolve warning: stopped at battlefieldIndex({battlefieldIndex}).");
                    break;
                }

                resolvedCount += 1;
                AppendLog(
                    $"Resolve[{battlefieldIndex}] outcome={outcome} totalAttack(P:{playerTotalAttack},E:{enemyTotalAttack}) morale(P:{battleState.playerMorale},E:{battleState.enemyMorale})");

                if (battleState.isBattleEnded)
                {
                    AppendLog($"Resolve stopped early: battle ended at battlefieldIndex({battlefieldIndex}).");
                    break;
                }
            }

            if (resolvedCount <= 0)
            {
                failureMessage = "no battlefields were resolved.";
                return false;
            }

            if (!battleState.isBattleEnded && !phaseRunner.AdvanceToNextPhase())
            {
                WarnAndLog($"Resolve warning: failed to move to next phase ({phaseRunner.LastFailureReason}).");
            }

            return true;
        }

        bool TryRetreatBattle(out string failureMessage)
        {
            failureMessage = string.Empty;

            if (!phaseRunner.isStarted)
            {
                failureMessage = "battle is not started.";
                return false;
            }

            if (battleState.isBattleEnded)
            {
                failureMessage = "battle already ended.";
                return false;
            }

            bool retreated = phaseRunner.TryRetreat();
            if (!retreated)
            {
                failureMessage = phaseRunner.LastFailureReason.ToString();
                return false;
            }

            selectedTroopId = string.Empty;
            selectedBattlefieldIndex = -1;
            return true;
        }

        void CollectTroopIds(HashSet<string> buffer, List<string> troopIds, string sourceLabel)
        {
            if (troopIds == null)
            {
                WarnAndLog($"Roll warning: {sourceLabel} is null.");
                return;
            }

            for (int i = 0; i < troopIds.Count; i++)
            {
                string troopId = troopIds[i];
                if (string.IsNullOrWhiteSpace(troopId))
                {
                    WarnAndLog($"Roll warning: empty troopId at {sourceLabel}[{i}].");
                    continue;
                }

                buffer.Add(troopId);
            }
        }

        void RejectAction(string actionName, string reason)
        {
            WarnAndLog($"{actionName} rejected: {reason}");
            RefreshStubTexts();
            RefreshStubButtons();
        }

        void WarnAndLog(string message)
        {
            UnityEngine.Debug.LogWarning($"[BattleDebugPanel] {message}");
            AppendLog(message);
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

        TComponent ResolveBinding<TComponent>(TComponent currentValue, string childName)
            where TComponent : Component
        {
            if (currentValue != null)
            {
                return currentValue;
            }

            TComponent found = FindChildComponentByName<TComponent>(childName);
            if (found == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[BattleDebugPanel] Auto bind failed: {typeof(TComponent).Name} '{childName}' was not found.");
            }

            return found;
        }

        TComponent FindChildComponentByName<TComponent>(string childName)
            where TComponent : Component
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (!string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (child.TryGetComponent(out TComponent component))
                {
                    return component;
                }
            }

            return null;
        }

        void WireOptionalButton(string buttonName, UnityAction callback)
        {
            Button button = FindChildComponentByName<Button>(buttonName);
            WireButton(button, callback);
        }

        static void WireButton(Button button, UnityAction callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(callback);
            button.onClick.AddListener(callback);
        }
    }
}
