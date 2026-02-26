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
    public class DuelScreenView
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
        readonly Func<string, Sprite> resolveAbilityIcon;

        readonly List<BattleAbilityCardView> pooledCardViews = new();
        readonly List<BattleAbilityCardView> enemyLoadoutCardViews = new();
        readonly List<BattleAbilityCardView> playerLoadoutCardViews = new();
        readonly Dictionary<string, BattleAbilityCardView> visibleCardsByInstanceId =
            new(StringComparer.Ordinal);
        BattleRevealState currentRevealState = BattleRevealState.Empty;
        int cachedMaxPlayerHealth = 1;
        int cachedMaxOpponentHealth = 1;

        public DuelScreenView(
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
            Image tooltipBackgroundImage,
            Func<string, Sprite> resolveAbilityIcon)
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
            this.resolveAbilityIcon = resolveAbilityIcon;
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
            HideDirectChildren(enemyLoadoutRow);
            HideDirectChildren(playerLoadoutRow);
            CacheCardPools();
            HideAllCardViews();
            HideTooltip();
        }

        public void RenderTopBar(BattleTopBarState state)
        {
            UpdateTopBar(state.turnIndex);
        }

        public void RenderHealth(BattleHealthState state)
        {
            RenderHealth(
                state.playerHealth,
                state.maxPlayerHealth,
                state.opponentHealth,
                state.maxOpponentHealth);
        }

        public void RenderButtons(BattleButtonState state)
        {
            UpdateButtonState(state.canCombatStart, state.canSurrender);
        }

        public void RenderBoard(
            BattleBoardState state,
            Action<string> onPlayerAbilityClicked,
            Action<BattleAbilityCardView, string, BattleAbilityCardView.InteractionContext, Vector2, Camera> onCardDragStart,
            Action<BattleAbilityCardView, string, BattleAbilityCardView.InteractionContext, Vector2, Camera> onCardDragMove,
            Action<BattleAbilityCardView, string, BattleAbilityCardView.InteractionContext, Vector2, Camera> onCardDragEnd,
            Action<BattleAbilityCardView, string, BattleAbilityCardView.InteractionContext> onCardRightClick)
        {
            CacheCardPools();
            HideAllCardViews();
            HideTooltip();

            RenderLoadoutRows(
                state.duelState,
                state.phaseRunner,
                state.database,
                state.selectedAbilityId,
                state.isFlowRunning,
                onPlayerAbilityClicked,
                onCardDragStart,
                onCardDragMove,
                onCardDragEnd,
                onCardRightClick);
            ForceRebuildLoadoutLayouts();
            RenderCombatZones(
                state.duelState,
                state.phaseRunner,
                state.database,
                state.selectedAbilityId,
                state.isFlowRunning,
                onPlayerAbilityClicked,
                onCardDragStart,
                onCardDragMove,
                onCardDragEnd,
                onCardRightClick);
            CacheVisibleCardsByInstanceId();
            ApplyRevealState(currentRevealState);
        }

        public void RenderReveal(BattleRevealState revealState)
        {
            currentRevealState = revealState;
            CacheVisibleCardsByInstanceId();
            ApplyRevealState(revealState);
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

                yield return AnimateResolveSingleCombat(step.combatIndex, step.outcome, animationConfig);

                if (gap > 0f && stepIndex < resolveResult.steps.Count - 1)
                {
                    yield return new WaitForSecondsRealtime(gap);
                }
            }
        }

        public IEnumerator AnimateResolveSingleCombat(
            int combatIndex,
            DuelOutcome outcome,
            BattleAnimationConfig animationConfig)
        {
            if (animationConfig == null || combatIndex < 0 || combatIndex >= combatZones.Length)
            {
                yield break;
            }

            BattleCombatZoneView zone = combatZones[combatIndex];
            if (zone == null)
            {
                yield break;
            }

            float duration = animationConfig.resolvePerCombatDuration;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float normalized = Mathf.PingPong(elapsed * 4f, 1f);
                zone.SetResolveHighlight(outcome, normalized);

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            zone.RestoreBaseVisual();
        }

        void UpdateTopBar(int turnIndex)
        {
            if (turnText == null)
            {
                return;
            }

            turnText.text = $"Turn: {turnIndex}";
        }

        void RenderHealth(
            int playerHealth,
            int maxPlayerHealth,
            int opponentHealth,
            int maxOpponentHealth)
        {
            cachedMaxPlayerHealth = Mathf.Max(1, maxPlayerHealth);
            cachedMaxOpponentHealth = Mathf.Max(1, maxOpponentHealth);

            if (enemyHealthText != null)
            {
                enemyHealthText.text = BuildHeartText(opponentHealth, cachedMaxOpponentHealth);
            }

            if (playerHealthText != null)
            {
                playerHealthText.text = BuildHeartText(playerHealth, cachedMaxPlayerHealth);
            }
        }

        void RenderLoadoutRows(
            DuelState duelState,
            DuelPhaseRunner phaseRunner,
            GameDatabase database,
            string selectedAbilityId,
            bool isFlowRunning,
            Action<string> onPlayerAbilityClicked,
            Action<BattleAbilityCardView, string, BattleAbilityCardView.InteractionContext, Vector2, Camera> onCardDragStart,
            Action<BattleAbilityCardView, string, BattleAbilityCardView.InteractionContext, Vector2, Camera> onCardDragMove,
            Action<BattleAbilityCardView, string, BattleAbilityCardView.InteractionContext, Vector2, Camera> onCardDragEnd,
            Action<BattleAbilityCardView, string, BattleAbilityCardView.InteractionContext> onCardRightClick)
        {
            if (duelState == null || database == null)
            {
                return;
            }

            List<BattleAbilityCardView.BindData> enemyCards = ExpandOpponentLoadoutCards(duelState, database);
            if (enemyCards.Count > maxLoadoutCardCount)
            {
                UnityEngine.Debug.LogWarning(
                    $"[DuelScreenView] Enemy loadout overflow: cardCount={enemyCards.Count}, max={maxLoadoutCardCount}");
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

                card.Bind(
                    enemyCards[i],
                    false,
                    false,
                    null,
                    ShowTooltip,
                    HideTooltip,
                    BattleAbilityCardView.InteractionContext.None,
                    null,
                    null,
                    null,
                    null);
            }

            List<string> playerLoadoutAbilityIds = duelState.loadoutAbilityIds == null
                ? new List<string>()
                : new List<string>(duelState.loadoutAbilityIds);
            if (playerLoadoutAbilityIds.Count > maxLoadoutCardCount)
            {
                UnityEngine.Debug.LogWarning(
                    $"[DuelScreenView] Player loadout overflow: cardCount={playerLoadoutAbilityIds.Count}, max={maxLoadoutCardCount}");
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
                    ability.abilityType == AbilityType.Attack &&
                    ability.cooldownRemaining <= 0;
                bool isSelected = string.Equals(selectedAbilityId, abilityId, StringComparison.Ordinal);
                BattleAbilityCardView.BindData bindData = CreateBindData(abilityId, ability, def);

                card.Bind(
                    bindData,
                    isSelected,
                    isInteractable,
                    onPlayerAbilityClicked,
                    ShowTooltip,
                    HideTooltip,
                    BattleAbilityCardView.InteractionContext.Loadout,
                    onCardDragStart,
                    onCardDragMove,
                    onCardDragEnd,
                    onCardRightClick);
            }
        }

        void RenderCombatZones(
            DuelState duelState,
            DuelPhaseRunner phaseRunner,
            GameDatabase database,
            string selectedAbilityId,
            bool isFlowRunning,
            Action<string> onPlayerAbilityClicked,
            Action<BattleAbilityCardView, string, BattleAbilityCardView.InteractionContext, Vector2, Camera> onCardDragStart,
            Action<BattleAbilityCardView, string, BattleAbilityCardView.InteractionContext, Vector2, Camera> onCardDragMove,
            Action<BattleAbilityCardView, string, BattleAbilityCardView.InteractionContext, Vector2, Camera> onCardDragEnd,
            Action<BattleAbilityCardView, string, BattleAbilityCardView.InteractionContext> onCardRightClick)
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
                    enemyTotal = ComputeDisplayedTotalPower(
                        combat,
                        duelState.abilitiesById,
                        isPlayerSide: false,
                        phaseRunner);
                    playerTotal = ComputeDisplayedTotalPower(
                        combat,
                        duelState.abilitiesById,
                        isPlayerSide: true,
                        phaseRunner);

                    RenderCombatSideCards(
                        duelState,
                        phaseRunner,
                        database,
                        selectedAbilityId,
                        isFlowRunning,
                        zone.EnemySlots,
                        combat.opponentAbilityIds,
                        isPlayerSide: false,
                        combatIndex: zoneIndex,
                        onClick: null,
                        onCardDragStart,
                        onCardDragMove,
                        onCardDragEnd,
                        onCardRightClick);
                    RenderCombatSideCards(
                        duelState,
                        phaseRunner,
                        database,
                        selectedAbilityId,
                        isFlowRunning,
                        zone.PlayerSlots,
                        combat.playerAbilityIds,
                        isPlayerSide: true,
                        combatIndex: zoneIndex,
                        onClick: onPlayerAbilityClicked,
                        onCardDragStart,
                        onCardDragMove,
                        onCardDragEnd,
                        onCardRightClick);
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
            int combatIndex,
            Action<string> onClick,
            Action<BattleAbilityCardView, string, BattleAbilityCardView.InteractionContext, Vector2, Camera> onCardDragStart,
            Action<BattleAbilityCardView, string, BattleAbilityCardView.InteractionContext, Vector2, Camera> onCardDragMove,
            Action<BattleAbilityCardView, string, BattleAbilityCardView.InteractionContext, Vector2, Camera> onCardDragEnd,
            Action<BattleAbilityCardView, string, BattleAbilityCardView.InteractionContext> onCardRightClick)
        {
            if (slots == null)
            {
                return;
            }

            int abilityCount = abilityIds == null ? 0 : abilityIds.Count;
            if (abilityCount > slots.Count)
            {
                UnityEngine.Debug.LogWarning(
                    $"[DuelScreenView] Slot overflow: abilityCount={abilityCount}, slotCount={slots.Count}");
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
                    ability.abilityType == AbilityType.Attack &&
                    ability.cooldownRemaining <= 0;
                BattleAbilityCardView.BindData bindData = CreateBindData(abilityId, ability, def);

                BattleAbilityCardView.InteractionContext context = isPlayerSide
                    ? BattleAbilityCardView.InteractionContext.Combat(combatIndex)
                    : BattleAbilityCardView.InteractionContext.None;
                card.Bind(
                    bindData,
                    isSelected,
                    isInteractable,
                    onClick,
                    ShowTooltip,
                    HideTooltip,
                    context,
                    onCardDragStart,
                    onCardDragMove,
                    onCardDragEnd,
                    onCardRightClick);
            }
        }

        void UpdateButtonState(bool canCombatStart, bool canSurrender)
        {
            if (combatStartButton != null)
            {
                combatStartButton.interactable = canCombatStart;
                ApplyButtonVisual(
                    combatStartButton,
                    canCombatStart
                        ? defaultCombatStartButtonColor
                        : defaultButtonDisabledColor);
            }

            if (surrenderButton != null)
            {
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

        void CacheVisibleCardsByInstanceId()
        {
            visibleCardsByInstanceId.Clear();
            for (int i = 0; i < pooledCardViews.Count; i++)
            {
                BattleAbilityCardView card = pooledCardViews[i];
                if (card == null || !card.gameObject.activeInHierarchy)
                {
                    continue;
                }

                string abilityId = card.InstanceId;
                if (string.IsNullOrWhiteSpace(abilityId) || visibleCardsByInstanceId.ContainsKey(abilityId))
                {
                    continue;
                }

                visibleCardsByInstanceId.Add(abilityId, card);
            }
        }

        void ApplyRevealState(BattleRevealState revealState)
        {
            ApplyRollOverlayState(revealState);
            if (!revealState.isRunning)
            {
                return;
            }

            ApplyRevealHealthState(revealState);
            ApplyRevealZoneTotals(revealState);
        }

        void ApplyRollOverlayState(BattleRevealState revealState)
        {
            foreach (KeyValuePair<string, BattleAbilityCardView> pair in visibleCardsByInstanceId)
            {
                pair.Value?.HideRollOverlay();
            }

            if (!revealState.isRunning)
            {
                return;
            }

            foreach (KeyValuePair<string, BattleAbilityCardView> pair in visibleCardsByInstanceId)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                if (!revealState.TryGetOverlay(pair.Key, out BattleRollOverlayValue overlay) || !overlay.isVisible)
                {
                    continue;
                }

                pair.Value.SetRollOverlayValue(overlay.value, overlay.isFinal);
            }
        }

        void ApplyRevealHealthState(BattleRevealState revealState)
        {
            if (revealState.displayOpponentHealth >= 0 && enemyHealthText != null)
            {
                enemyHealthText.text = BuildHeartText(revealState.displayOpponentHealth, cachedMaxOpponentHealth);
            }

            if (revealState.displayPlayerHealth >= 0 && playerHealthText != null)
            {
                playerHealthText.text = BuildHeartText(revealState.displayPlayerHealth, cachedMaxPlayerHealth);
            }
        }

        void ApplyRevealZoneTotals(BattleRevealState revealState)
        {
            if (combatZones == null)
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

                if (!revealState.TryGetZoneTotals(zoneIndex, out int opponentTotal, out int playerTotal))
                {
                    continue;
                }

                zone.SetTotals(opponentTotal, playerTotal);
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

        static void HideDirectChildren(RectTransform root)
        {
            if (root == null)
            {
                return;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                child.gameObject.SetActive(false);
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

        List<BattleAbilityCardView.BindData> ExpandOpponentLoadoutCards(
            DuelState duelState,
            GameDatabase database)
        {
            var cards = new List<BattleAbilityCardView.BindData>();

            if (duelState?.opponentLoadoutAbilityIds == null || database?.abilitiesById == null)
            {
                return cards;
            }

            for (int i = 0; i < duelState.opponentLoadoutAbilityIds.Count; i++)
            {
                string abilityId = duelState.opponentLoadoutAbilityIds[i];
                if (string.IsNullOrWhiteSpace(abilityId))
                {
                    continue;
                }

                if (!TryResolveAbilityAndDef(duelState, database, abilityId, out AbilityInstance ability, out AbilityDef def))
                {
                    continue;
                }

                cards.Add(CreateBindData(abilityId, ability, def));
            }

            return cards;
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
                UnityEngine.Debug.LogWarning($"[DuelScreenView] Missing ability instance: {abilityId}");
                return false;
            }

            if (database?.abilitiesById == null ||
                !database.abilitiesById.TryGetValue(ability.abilityDefId, out def) ||
                def == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[DuelScreenView] Missing ability def for instance({abilityId}) defId({ability.abilityDefId}).");
                return false;
            }

            return true;
        }

        BattleAbilityCardView.BindData CreateBindData(
            string abilityId,
            AbilityInstance ability,
            AbilityDef def)
        {
            int displayPower = Mathf.Max(0, ability.power);
            Sprite iconSprite = resolveAbilityIcon == null
                ? null
                : resolveAbilityIcon.Invoke(def.iconId);

            return new BattleAbilityCardView.BindData(
                abilityId,
                def.id,
                BuildAbilityTooltip(def),
                ability.abilityType,
                iconSprite,
                displayPower,
                ability.abilityType == AbilityType.Attack,
                ability.cooldownTurns,
                ability.cooldownRemaining);
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

        static int ComputeDisplayedTotalPower(
            CombatState combat,
            IReadOnlyDictionary<string, AbilityInstance> abilitiesById,
            bool isPlayerSide,
            DuelPhaseRunner phaseRunner)
        {
            if (combat == null || abilitiesById == null)
            {
                return 0;
            }

            combat.EnsureInitialized();

            int total = isPlayerSide
                ? combat.totalPowerBonusPlayer
                : combat.totalPowerBonusOpponent;
            List<string> abilityIds = isPlayerSide
                ? combat.playerAbilityIds
                : combat.opponentAbilityIds;
            bool usePowerResult = phaseRunner != null &&
                (phaseRunner.currentPhase == DuelPhase.Roll || phaseRunner.currentPhase == DuelPhase.Resolve);

            for (int i = 0; i < abilityIds.Count; i++)
            {
                string abilityId = abilityIds[i];
                if (string.IsNullOrWhiteSpace(abilityId))
                {
                    continue;
                }

                if (!abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) || ability == null)
                {
                    continue;
                }

                if (ability.abilityType != AbilityType.Attack)
                {
                    continue;
                }

                int value = usePowerResult && ability.powerResult > 0
                    ? ability.powerResult
                    : ability.power;
                total += Mathf.Max(0, value);
            }

            return total;
        }

        void ForceRebuildLoadoutLayouts()
        {
            if (enemyLoadoutRow != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(enemyLoadoutRow);
            }

            if (playerLoadoutRow != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(playerLoadoutRow);
            }
        }
    }
}
