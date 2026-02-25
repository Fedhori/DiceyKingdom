using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Game.Application.Duel;
using Game.Domain.Duel;
using Game.Infrastructure.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Battle
{
    public sealed class BattleScreenView
    {
        const int expectedCombatCount = 3;
        const int maxLoadoutCardCount = 12;

        static readonly Color defaultCombatStartButtonColor = Colors.Semantic.StateInfo;
        static readonly Color defaultSurrenderButtonColor = Colors.Semantic.StateDanger;
        static readonly Color defaultButtonDisabledColor = Colors.Semantic.ActionSecondaryBgDisabled;
        static readonly Color defaultTooltipBackgroundColor = Colors.Semantic.SurfaceSecondary;
        static readonly Color defaultTooltipTextColor = Colors.Semantic.TextPrimary;

        readonly Image backgroundImage;
        readonly Image topBarImage;
        readonly TMP_Text turnText;
        readonly TMP_Text enemyHealthText;
        readonly TMP_Text playerHealthText;
        readonly Button combatStartButton;
        readonly Button surrenderButton;
        readonly RectTransform enemyLoadoutRow;
        readonly RectTransform playerLoadoutRow;
        readonly BattleCombatZoneView[] combatZones;
        readonly TMP_Text tooltipText;
        readonly Image tooltipBackgroundImage;

        readonly List<BattleAbilityCardView> pooledCardViews = new();
        readonly List<BattleAbilityCardView> enemyLoadoutCardViews = new();
        readonly List<BattleAbilityCardView> playerLoadoutCardViews = new();

        public BattleScreenView(
            Image backgroundImage,
            Image topBarImage,
            TMP_Text turnText,
            TMP_Text enemyHealthText,
            TMP_Text playerHealthText,
            Button combatStartButton,
            Button surrenderButton,
            RectTransform enemyLoadoutRow,
            RectTransform playerLoadoutRow,
            BattleCombatZoneView[] combatZones,
            BattleAbilityCardView _,
            TMP_Text tooltipText,
            Image tooltipBackgroundImage)
        {
            this.backgroundImage = backgroundImage;
            this.topBarImage = topBarImage;
            this.turnText = turnText;
            this.enemyHealthText = enemyHealthText;
            this.playerHealthText = playerHealthText;
            this.combatStartButton = combatStartButton;
            this.surrenderButton = surrenderButton;
            this.enemyLoadoutRow = enemyLoadoutRow;
            this.playerLoadoutRow = playerLoadoutRow;
            this.combatZones = combatZones ?? Array.Empty<BattleCombatZoneView>();
            this.tooltipText = tooltipText;
            this.tooltipBackgroundImage = tooltipBackgroundImage;
        }

        public bool ValidateSceneReferencesForRuntime(out string missingReferences)
        {
            var missing = new List<string>();
            if (backgroundImage == null)
            {
                missing.Add(nameof(backgroundImage));
            }

            if (topBarImage == null)
            {
                missing.Add(nameof(topBarImage));
            }

            if (turnText == null)
            {
                missing.Add(nameof(turnText));
            }

            if (enemyHealthText == null)
            {
                missing.Add(nameof(enemyHealthText));
            }

            if (playerHealthText == null)
            {
                missing.Add(nameof(playerHealthText));
            }

            if (combatStartButton == null)
            {
                missing.Add(nameof(combatStartButton));
            }

            if (surrenderButton == null)
            {
                missing.Add(nameof(surrenderButton));
            }

            if (enemyLoadoutRow == null)
            {
                missing.Add(nameof(enemyLoadoutRow));
            }

            if (playerLoadoutRow == null)
            {
                missing.Add(nameof(playerLoadoutRow));
            }

            if (combatZones == null || combatZones.Length != expectedCombatCount || combatZones.Any(zone => zone == null))
            {
                missing.Add(nameof(combatZones));
            }

            if (tooltipText == null)
            {
                missing.Add(nameof(tooltipText));
            }

            if (tooltipBackgroundImage == null)
            {
                missing.Add(nameof(tooltipBackgroundImage));
            }

            CacheCardPools();
            if (enemyLoadoutCardViews.Count < maxLoadoutCardCount)
            {
                missing.Add($"{nameof(enemyLoadoutRow)}(cards<{maxLoadoutCardCount})");
            }

            if (playerLoadoutCardViews.Count < maxLoadoutCardCount)
            {
                missing.Add($"{nameof(playerLoadoutRow)}(cards<{maxLoadoutCardCount})");
            }

            for (int zoneIndex = 0; zoneIndex < combatZones.Length; zoneIndex++)
            {
                BattleCombatZoneView zone = combatZones[zoneIndex];
                if (zone == null)
                {
                    continue;
                }

                if (!HasCardInEverySlot(zone.EnemySlots))
                {
                    missing.Add($"combatZones[{zoneIndex}].EnemySlots(cards missing)");
                }

                if (!HasCardInEverySlot(zone.PlayerSlots))
                {
                    missing.Add($"combatZones[{zoneIndex}].PlayerSlots(cards missing)");
                }
            }

            missingReferences = string.Join(", ", missing);
            return missing.Count <= 0;
        }

        public void WireZoneCallbacks(Action<int> onZoneClicked)
        {
            for (int i = 0; i < combatZones.Length; i++)
            {
                BattleCombatZoneView zone = combatZones[i];
                if (zone == null)
                {
                    continue;
                }

                zone.SetCombatIndex(i);
                zone.SetClickHandler(onZoneClicked);
                zone.EnsureRowsAndSlots();
            }

            CacheCardPools();
        }

        public void UnwireZoneCallbacks()
        {
            for (int i = 0; i < combatZones.Length; i++)
            {
                if (combatZones[i] == null)
                {
                    continue;
                }

                combatZones[i].SetClickHandler(null);
            }
        }

        public void ApplyStaticVisuals()
        {
            HideTooltip();
        }

        public void Refresh(
            BattleSessionRunner sessionRunner,
            BattleSelectionState selectionState,
            bool isFlowRunning,
            Action<string> onPlayerAbilityClicked)
        {
            CacheCardPools();
            HideAllCardViews();
            HideTooltip();

            DuelState duelState = sessionRunner == null ? null : sessionRunner.DuelState;
            DuelPhaseRunner phaseRunner = sessionRunner == null ? null : sessionRunner.PhaseRunner;
            GameDatabase database = sessionRunner == null ? null : sessionRunner.Database;
            int maxPlayerHealth = sessionRunner == null ? 1 : sessionRunner.MaxPlayerHealth;
            int maxOpponentHealth = sessionRunner == null ? 1 : sessionRunner.MaxOpponentHealth;

            string selectedAbilityId = selectionState == null
                ? string.Empty
                : selectionState.SelectedAbilityId;

            UpdateTopBar(duelState);
            RenderHealth(duelState, maxPlayerHealth, maxOpponentHealth);
            RenderLoadoutRows(
                duelState,
                phaseRunner,
                database,
                selectedAbilityId,
                isFlowRunning,
                onPlayerAbilityClicked);
            RenderCombatZones(
                duelState,
                phaseRunner,
                database,
                selectedAbilityId,
                isFlowRunning,
                onPlayerAbilityClicked);
            UpdateButtonState(duelState, phaseRunner, isFlowRunning);
        }

        public IEnumerator AnimateRoll(BattleAnimationConfig animationConfig)
        {
            float duration = animationConfig == null ? 0f : animationConfig.rollDuration;
            if (duration <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float normalized = Mathf.PingPong(elapsed * 5f, 1f);

                for (int zoneIndex = 0; zoneIndex < combatZones.Length; zoneIndex++)
                {
                    BattleCombatZoneView zone = combatZones[zoneIndex];
                    zone?.SetRollPulse(normalized);
                }

                for (int i = 0; i < pooledCardViews.Count; i++)
                {
                    BattleAbilityCardView card = pooledCardViews[i];
                    if (card == null || !card.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    card.SetRollPulse(normalized);
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            for (int zoneIndex = 0; zoneIndex < combatZones.Length; zoneIndex++)
            {
                BattleCombatZoneView zone = combatZones[zoneIndex];
                zone?.RestoreBaseVisual();
            }

            for (int i = 0; i < pooledCardViews.Count; i++)
            {
                BattleAbilityCardView card = pooledCardViews[i];
                if (card == null || !card.gameObject.activeInHierarchy)
                {
                    continue;
                }

                card.RestoreVisual();
            }
        }

        public IEnumerator AnimateResolve(
            DuelCombatResolveResult resolveResult,
            BattleAnimationConfig animationConfig)
        {
            if (resolveResult == null || animationConfig == null)
            {
                yield break;
            }

            float duration = animationConfig.resolvePerCombatDuration;
            float gap = animationConfig.resolveCombatGap;

            for (int stepIndex = 0; stepIndex < resolveResult.steps.Count; stepIndex++)
            {
                DuelCombatResolveStepResult step = resolveResult.steps[stepIndex];
                if (step.combatIndex < 0 || step.combatIndex >= combatZones.Length)
                {
                    continue;
                }

                BattleCombatZoneView zone = combatZones[step.combatIndex];
                if (zone == null)
                {
                    continue;
                }

                float elapsed = 0f;
                while (elapsed < duration)
                {
                    float normalized = Mathf.PingPong(elapsed * 4f, 1f);
                    zone.SetResolveHighlight(step.outcome, normalized);

                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                zone.RestoreBaseVisual();

                if (gap > 0f && stepIndex < resolveResult.steps.Count - 1)
                {
                    yield return new WaitForSecondsRealtime(gap);
                }
            }
        }

        public IEnumerator AnimateTurnTransition(BattleAnimationConfig animationConfig)
        {
            float duration = animationConfig == null ? 0f : animationConfig.turnTransitionDuration;
            if (topBarImage == null || duration <= 0f)
            {
                if (duration > 0f)
                {
                    yield return new WaitForSecondsRealtime(duration);
                }

                yield break;
            }

            Color baseColor = topBarImage.color;
            Color pulseColor = Colors.Semantic.ActionSecondaryBgHover;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float normalized = Mathf.PingPong(elapsed * 5f, 1f);
                topBarImage.color = Color.Lerp(baseColor, pulseColor, normalized);

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            topBarImage.color = baseColor;
        }

        void UpdateTopBar(DuelState duelState)
        {
            if (turnText == null)
            {
                return;
            }

            int turnIndex = duelState == null ? 0 : duelState.turnIndex;
            turnText.text = $"Turn: {turnIndex}";
        }

        void RenderHealth(DuelState duelState, int maxPlayerHealth, int maxOpponentHealth)
        {
            if (enemyHealthText != null)
            {
                enemyHealthText.text = BuildHeartText(
                    duelState == null ? 0 : duelState.opponentHealth,
                    maxOpponentHealth);
            }

            if (playerHealthText != null)
            {
                playerHealthText.text = BuildHeartText(
                    duelState == null ? 0 : duelState.playerHealth,
                    maxPlayerHealth);
            }
        }

        void RenderLoadoutRows(
            DuelState duelState,
            DuelPhaseRunner phaseRunner,
            GameDatabase database,
            string selectedAbilityId,
            bool isFlowRunning,
            Action<string> onPlayerAbilityClicked)
        {
            if (duelState == null || database == null)
            {
                return;
            }

            List<BattleAbilityCardView.BindData> enemyCards = ExpandOpponentLoadoutCards(duelState, database);
            if (enemyCards.Count > maxLoadoutCardCount)
            {
                UnityEngine.Debug.LogWarning(
                    $"[BattleScreenView] Enemy loadout overflow: cardCount={enemyCards.Count}, max={maxLoadoutCardCount}");
            }

            int enemyVisibleCount = Mathf.Min(maxLoadoutCardCount, enemyCards.Count);
            for (int i = 0; i < enemyLoadoutCardViews.Count; i++)
            {
                BattleAbilityCardView card = enemyLoadoutCardViews[i];
                if (card == null)
                {
                    continue;
                }

                bool visible = i < enemyVisibleCount;
                card.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                card.Bind(enemyCards[i], false, false, null, null, null);
            }

            List<string> playerLoadoutAbilityIds = duelState.loadoutAbilityIds == null
                ? new List<string>()
                : duelState.loadoutAbilityIds
                    .OrderBy(id => ResolveSortPriority(duelState, id))
                    .ThenBy(id => id, StringComparer.Ordinal)
                    .ToList();
            if (playerLoadoutAbilityIds.Count > maxLoadoutCardCount)
            {
                UnityEngine.Debug.LogWarning(
                    $"[BattleScreenView] Player loadout overflow: cardCount={playerLoadoutAbilityIds.Count}, max={maxLoadoutCardCount}");
            }

            int playerVisibleCount = Mathf.Min(maxLoadoutCardCount, playerLoadoutAbilityIds.Count);
            for (int i = 0; i < playerLoadoutCardViews.Count; i++)
            {
                BattleAbilityCardView card = playerLoadoutCardViews[i];
                if (card == null)
                {
                    continue;
                }

                bool visible = i < playerVisibleCount;
                card.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                string abilityId = playerLoadoutAbilityIds[i];
                if (!TryResolveAbilityAndDef(duelState, database, abilityId, out AbilityInstance ability, out AbilityDef def))
                {
                    card.gameObject.SetActive(false);
                    continue;
                }

                bool isInteractable = !isFlowRunning &&
                    !duelState.isDuelEnded &&
                    phaseRunner != null &&
                    phaseRunner.currentPhase == DuelPhase.PlayerSetup &&
                    ability.abilityType == AbilityType.Attack;
                bool isSelected = string.Equals(selectedAbilityId, abilityId, StringComparison.Ordinal);
                BattleAbilityCardView.BindData bindData = CreateBindData(abilityId, ability, def, phaseRunner);

                card.Bind(bindData, isSelected, isInteractable, onPlayerAbilityClicked, ShowTooltip, HideTooltip);
            }
        }

        void RenderCombatZones(
            DuelState duelState,
            DuelPhaseRunner phaseRunner,
            GameDatabase database,
            string selectedAbilityId,
            bool isFlowRunning,
            Action<string> onPlayerAbilityClicked)
        {
            if (combatZones == null || combatZones.Length <= 0)
            {
                return;
            }

            for (int zoneIndex = 0; zoneIndex < combatZones.Length; zoneIndex++)
            {
                BattleCombatZoneView zone = combatZones[zoneIndex];
                if (zone == null)
                {
                    continue;
                }

                zone.EnsureRowsAndSlots();
                zone.SetCombatIndex(zoneIndex);

                int enemyTotal = 0;
                int playerTotal = 0;
                if (duelState != null &&
                    duelState.combats != null &&
                    zoneIndex >= 0 &&
                    zoneIndex < duelState.combats.Count &&
                    duelState.combats[zoneIndex] != null)
                {
                    CombatState combat = duelState.combats[zoneIndex];
                    combat.EnsureInitialized();
                    enemyTotal = DuelSimulator.ComputeTotalPower(combat, duelState.abilitiesById, false);
                    playerTotal = DuelSimulator.ComputeTotalPower(combat, duelState.abilitiesById, true);

                    RenderCombatSideCards(
                        duelState,
                        phaseRunner,
                        database,
                        selectedAbilityId,
                        isFlowRunning,
                        zone.EnemySlots,
                        combat.opponentAbilityIds,
                        isPlayerSide: false,
                        onClick: null);
                    RenderCombatSideCards(
                        duelState,
                        phaseRunner,
                        database,
                        selectedAbilityId,
                        isFlowRunning,
                        zone.PlayerSlots,
                        combat.playerAbilityIds,
                        isPlayerSide: true,
                        onClick: onPlayerAbilityClicked);
                }
                else
                {
                    HideSlotCards(zone.EnemySlots);
                    HideSlotCards(zone.PlayerSlots);
                }

                bool canDeployToZone = !isFlowRunning &&
                    duelState != null &&
                    !duelState.isDuelEnded &&
                    phaseRunner != null &&
                    phaseRunner.currentPhase == DuelPhase.PlayerSetup &&
                    !string.IsNullOrWhiteSpace(selectedAbilityId);

                zone.SetInteractable(canDeployToZone);
                zone.SetTotals(enemyTotal, playerTotal);
            }
        }

        void RenderCombatSideCards(
            DuelState duelState,
            DuelPhaseRunner phaseRunner,
            GameDatabase database,
            string selectedAbilityId,
            bool isFlowRunning,
            IReadOnlyList<RectTransform> slots,
            List<string> abilityIds,
            bool isPlayerSide,
            Action<string> onClick)
        {
            if (slots == null)
            {
                return;
            }

            int abilityCount = abilityIds == null ? 0 : abilityIds.Count;
            if (abilityCount > slots.Count)
            {
                UnityEngine.Debug.LogWarning(
                    $"[BattleScreenView] Slot overflow: abilityCount={abilityCount}, slotCount={slots.Count}");
            }

            for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
            {
                RectTransform slot = slots[slotIndex];
                BattleAbilityCardView card = ResolveCardInSlot(slot);
                if (card == null)
                {
                    continue;
                }

                bool visible = slotIndex < abilityCount;
                card.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                string abilityId = abilityIds[slotIndex];
                if (!TryResolveAbilityAndDef(duelState, database, abilityId, out AbilityInstance ability, out AbilityDef def))
                {
                    card.gameObject.SetActive(false);
                    continue;
                }

                bool isSelected = string.Equals(selectedAbilityId, abilityId, StringComparison.Ordinal);
                bool isInteractable = isPlayerSide &&
                    duelState != null &&
                    !isFlowRunning &&
                    !duelState.isDuelEnded &&
                    phaseRunner != null &&
                    phaseRunner.currentPhase == DuelPhase.PlayerSetup &&
                    ability.abilityType == AbilityType.Attack;
                BattleAbilityCardView.BindData bindData = CreateBindData(abilityId, ability, def, phaseRunner);

                card.Bind(bindData, isSelected, isInteractable, onClick, ShowTooltip, HideTooltip);
            }
        }

        int ResolveSortPriority(DuelState duelState, string abilityId)
        {
            if (duelState == null ||
                !duelState.abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) ||
                ability == null)
            {
                return 999;
            }

            return ability.abilityType == AbilityType.Attack ? 0 : 1;
        }

        void UpdateButtonState(DuelState duelState, DuelPhaseRunner phaseRunner, bool isFlowRunning)
        {
            bool isBusy = isFlowRunning || duelState == null || phaseRunner == null;

            if (combatStartButton != null)
            {
                bool canStart = !isBusy && !duelState.isDuelEnded;
                combatStartButton.interactable = canStart;
                ApplyButtonVisual(
                    combatStartButton,
                    canStart
                        ? defaultCombatStartButtonColor
                        : defaultButtonDisabledColor);
            }

            if (surrenderButton != null)
            {
                bool canSurrender = !isBusy &&
                    !duelState.isDuelEnded &&
                    phaseRunner.currentPhase == DuelPhase.PlayerSetup &&
                    duelState.honor > 0;
                surrenderButton.interactable = canSurrender;
                ApplyButtonVisual(
                    surrenderButton,
                    canSurrender
                        ? defaultSurrenderButtonColor
                        : defaultButtonDisabledColor);
            }
        }

        void ShowTooltip(string message)
        {
            if (tooltipText == null || tooltipBackgroundImage == null)
            {
                return;
            }

            tooltipText.text = string.IsNullOrWhiteSpace(message) ? "-" : message;
            tooltipText.color = defaultTooltipTextColor;
            tooltipText.gameObject.SetActive(true);

            tooltipBackgroundImage.color = defaultTooltipBackgroundColor;
            tooltipBackgroundImage.gameObject.SetActive(true);
        }

        void HideTooltip()
        {
            if (tooltipText != null)
            {
                tooltipText.text = string.Empty;
                tooltipText.gameObject.SetActive(false);
            }

            if (tooltipBackgroundImage != null)
            {
                tooltipBackgroundImage.gameObject.SetActive(false);
            }
        }

        void CacheCardPools()
        {
            enemyLoadoutCardViews.Clear();
            playerLoadoutCardViews.Clear();
            pooledCardViews.Clear();

            CollectCardViews(enemyLoadoutRow, enemyLoadoutCardViews);
            CollectCardViews(playerLoadoutRow, playerLoadoutCardViews);

            AddCardsToPool(enemyLoadoutCardViews);
            AddCardsToPool(playerLoadoutCardViews);

            for (int zoneIndex = 0; zoneIndex < combatZones.Length; zoneIndex++)
            {
                BattleCombatZoneView zone = combatZones[zoneIndex];
                if (zone == null)
                {
                    continue;
                }

                zone.EnsureRowsAndSlots();

                AddSlotCardsToPool(zone.EnemySlots);
                AddSlotCardsToPool(zone.PlayerSlots);
            }
        }

        void HideAllCardViews()
        {
            for (int i = 0; i < pooledCardViews.Count; i++)
            {
                BattleAbilityCardView card = pooledCardViews[i];
                if (card == null)
                {
                    continue;
                }

                card.gameObject.SetActive(false);
            }
        }

        static void CollectCardViews(RectTransform root, List<BattleAbilityCardView> buffer)
        {
            buffer.Clear();
            if (root == null)
            {
                return;
            }

            IEnumerable<BattleAbilityCardView> cards = root
                .GetComponentsInChildren<BattleAbilityCardView>(true)
                .OrderBy(card => card == null || card.transform.parent == null
                    ? int.MaxValue
                    : card.transform.parent.GetSiblingIndex())
                .ThenBy(card => card == null ? int.MaxValue : card.transform.GetSiblingIndex());

            foreach (BattleAbilityCardView card in cards)
            {
                if (card == null)
                {
                    continue;
                }

                buffer.Add(card);
            }
        }

        void AddCardsToPool(IReadOnlyList<BattleAbilityCardView> cards)
        {
            if (cards == null)
            {
                return;
            }

            for (int i = 0; i < cards.Count; i++)
            {
                AddCardToPool(cards[i]);
            }
        }

        void AddSlotCardsToPool(IReadOnlyList<RectTransform> slots)
        {
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                AddCardToPool(ResolveCardInSlot(slots[i]));
            }
        }

        void AddCardToPool(BattleAbilityCardView card)
        {
            if (card == null || pooledCardViews.Contains(card))
            {
                return;
            }

            pooledCardViews.Add(card);
        }

        static void HideSlotCards(IReadOnlyList<RectTransform> slots)
        {
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                BattleAbilityCardView card = ResolveCardInSlot(slots[i]);
                if (card != null)
                {
                    card.gameObject.SetActive(false);
                }
            }
        }

        static bool HasCardInEverySlot(IReadOnlyList<RectTransform> slots)
        {
            if (slots == null)
            {
                return false;
            }

            for (int i = 0; i < slots.Count; i++)
            {
                if (ResolveCardInSlot(slots[i]) == null)
                {
                    return false;
                }
            }

            return true;
        }

        static BattleAbilityCardView ResolveCardInSlot(RectTransform slot)
        {
            if (slot == null)
            {
                return null;
            }

            return slot.GetComponentInChildren<BattleAbilityCardView>(true);
        }

        static string BuildHeartText(int currentHealth, int maxHealth)
        {
            int safeMax = Mathf.Max(1, maxHealth);
            int safeCurrent = Mathf.Clamp(currentHealth, 0, safeMax);

            const string fullHeart = "\u2665";
            const string emptyHeart = "\u2661";

            var builder = new StringBuilder(safeMax * 2);
            for (int i = 0; i < safeCurrent; i++)
            {
                builder.Append(fullHeart);
            }

            for (int i = safeCurrent; i < safeMax; i++)
            {
                builder.Append(emptyHeart);
            }

            return builder.ToString();
        }

        static List<BattleAbilityCardView.BindData> ExpandOpponentLoadoutCards(
            DuelState duelState,
            GameDatabase database)
        {
            var cards = new List<BattleAbilityCardView.BindData>();

            if (duelState?.opponentLoadoutEntries == null || database?.abilitiesById == null)
            {
                return cards;
            }

            for (int i = 0; i < duelState.opponentLoadoutEntries.Count; i++)
            {
                OpponentLoadoutEntry entry = duelState.opponentLoadoutEntries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.abilityDefId))
                {
                    continue;
                }

                if (!database.abilitiesById.TryGetValue(entry.abilityDefId, out AbilityDef def) || def == null)
                {
                    UnityEngine.Debug.LogWarning($"[BattleScreenView] Missing opponent ability def: {entry.abilityDefId}");
                    continue;
                }

                AbilityType abilityType = AbilityType.Attack;
                if (!def.TryGetAbilityType(out abilityType))
                {
                    abilityType = AbilityType.Attack;
                }

                int count = Mathf.Max(0, entry.count);
                for (int copyIndex = 0; copyIndex < count; copyIndex++)
                {
                    string pseudoInstanceId = $"{entry.abilityDefId}#{copyIndex}";
                    cards.Add(new BattleAbilityCardView.BindData(
                        pseudoInstanceId,
                        def.id,
                        BuildAbilityTooltip(def),
                        abilityType,
                        Mathf.Max(0, def.ResolvePower()),
                        abilityType == AbilityType.Attack));
                }
            }

            return cards
                .OrderBy(bindData => bindData.abilityType == AbilityType.Attack ? 0 : 1)
                .ThenBy(bindData => bindData.title, StringComparer.Ordinal)
                .ToList();
        }

        static bool TryResolveAbilityAndDef(
            DuelState duelState,
            GameDatabase database,
            string abilityId,
            out AbilityInstance ability,
            out AbilityDef def)
        {
            ability = null;
            def = null;

            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            if (duelState?.abilitiesById == null ||
                !duelState.abilitiesById.TryGetValue(abilityId, out ability) ||
                ability == null)
            {
                UnityEngine.Debug.LogWarning($"[BattleScreenView] Missing ability instance: {abilityId}");
                return false;
            }

            if (database?.abilitiesById == null ||
                !database.abilitiesById.TryGetValue(ability.abilityDefId, out def) ||
                def == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[BattleScreenView] Missing ability def for instance({abilityId}) defId({ability.abilityDefId}).");
                return false;
            }

            return true;
        }

        static BattleAbilityCardView.BindData CreateBindData(
            string abilityId,
            AbilityInstance ability,
            AbilityDef def,
            DuelPhaseRunner phaseRunner)
        {
            int displayPower = Mathf.Max(0, ability.power);
            if (ability.abilityType == AbilityType.Attack &&
                phaseRunner != null &&
                phaseRunner.currentPhase != DuelPhase.PlayerSetup &&
                ability.powerResult > 0)
            {
                displayPower = ability.powerResult;
            }

            return new BattleAbilityCardView.BindData(
                abilityId,
                def.id,
                BuildAbilityTooltip(def),
                ability.abilityType,
                displayPower,
                ability.abilityType == AbilityType.Attack);
        }

        static string BuildAbilityTooltip(AbilityDef def)
        {
            if (def == null)
            {
                return string.Empty;
            }

            string line1 = def.id;
            string line2 = string.IsNullOrWhiteSpace(def.descLocKey)
                ? string.Empty
                : def.descLocKey;
            return string.IsNullOrWhiteSpace(line2)
                ? line1
                : $"{line1}\n{line2}";
        }

        static void ApplyButtonVisual(Button button, Color backgroundColor)
        {
            if (button == null)
            {
                return;
            }

            if (button.TryGetComponent(out Image image))
            {
                image.color = backgroundColor;
            }
        }
    }
}
