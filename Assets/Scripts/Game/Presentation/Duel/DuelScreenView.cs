using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Game.Application.Duel;
using Game.Domain.Duel;
using Game.Domain.Modifiers;
using Game.Infrastructure.Data;
using Game.Presentation.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Duel
{
    public class DuelScreenView
    {
        static readonly Color defaultCombatStartButtonColor = Colors.Semantic.StateInfo;
        static readonly Color defaultSurrenderButtonColor = Colors.Semantic.StateDanger;
        static readonly Color defaultButtonDisabledColor = Colors.Semantic.ActionSecondaryBgDisabled;

        readonly Image backgroundImage;
        readonly Image topBarImage;
        readonly TMP_Text turnText;
        readonly TMP_Text enemyHealthText;
        readonly TMP_Text playerHealthText;
        readonly Button combatStartButton;
        readonly Button surrenderButton;
        readonly RectTransform enemyLoadoutRow;
        readonly RectTransform playerLoadoutRow;
        readonly RectTransform enemyPassiveRow;
        readonly RectTransform playerPassiveRow;
        readonly DuelCombatZoneView[] combatZones;
        readonly DuelAbilityCardView abilityCardPrefab;
        readonly Func<string, Sprite> resolveAbilityIcon;
        readonly DuelAbilityTextFormatter abilityTextFormatter;

        readonly List<DuelAbilityCardView> reusableCardViews = new();
        readonly List<DuelAbilityCardView> activeCardViews = new();
        readonly Dictionary<string, DuelAbilityCardView> cardViewsByInstanceId =
            new(StringComparer.Ordinal);
        readonly Dictionary<string, DuelAbilityCardView> visibleCardsByInstanceId =
            new(StringComparer.Ordinal);
        DuelRevealState currentRevealState = DuelRevealState.Empty;
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
            RectTransform enemyPassiveRow,
            RectTransform playerPassiveRow,
            DuelCombatZoneView[] combatZones,
            DuelAbilityCardView abilityCardPrefab,
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
            this.enemyPassiveRow = enemyPassiveRow;
            this.playerPassiveRow = playerPassiveRow;
            this.combatZones = combatZones ?? Array.Empty<DuelCombatZoneView>();
            this.abilityCardPrefab = abilityCardPrefab;
            this.resolveAbilityIcon = resolveAbilityIcon;
            abilityTextFormatter = new DuelAbilityTextFormatter(new UnityLocalizedTextResolver());
        }

        public void WireZoneCallbacks(Action<int> onZoneClicked)
        {
            for (int i = 0; i < combatZones.Length; i++)
            {
                DuelCombatZoneView zone = combatZones[i];
                if (zone == null)
                {
                    continue;
                }

                zone.SetCombatIndex(i);
                zone.SetClickHandler(onZoneClicked);
                zone.EnsureRowsAndSlots();
            }
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
            HideDirectChildren(enemyPassiveRow);
            HideDirectChildren(playerPassiveRow);
            ReleaseAllActiveCards();
        }

        public void RenderTopBar(DuelTopBarState state)
        {
            UpdateTopBar(state.turnIndex);
        }

        public void RenderHealth(DuelHealthState state)
        {
            RenderHealth(
                state.playerHealth,
                state.maxPlayerHealth,
                state.opponentHealth,
                state.maxOpponentHealth);
        }

        public void RenderButtons(DuelButtonState state)
        {
            UpdateButtonState(state.canCombatStart, state.canSurrender);
        }

        public void RenderBoard(
            DuelBoardState state,
            Action<string> onPlayerAbilityClicked,
            Action<DuelAbilityCardView, string, DuelAbilityCardView.InteractionContext, Vector2, Camera> onCardDragStart,
            Action<DuelAbilityCardView, string, DuelAbilityCardView.InteractionContext, Vector2, Camera> onCardDragMove,
            Action<DuelAbilityCardView, string, DuelAbilityCardView.InteractionContext, Vector2, Camera> onCardDragEnd,
            Action<DuelAbilityCardView, string, DuelAbilityCardView.InteractionContext> onCardRightClick)
        {
            ReleaseAllActiveCards();

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
            RenderPassiveRows(
                state.duelState,
                state.database);
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

        public void RenderReveal(DuelRevealState revealState)
        {
            currentRevealState = revealState;
            CacheVisibleCardsByInstanceId();
            ApplyRevealState(revealState);
        }

        public bool TryGetVisibleCardView(string abilityId, out DuelAbilityCardView cardView)
        {
            cardView = null;
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            CacheVisibleCardsByInstanceId();
            if (!visibleCardsByInstanceId.TryGetValue(abilityId, out DuelAbilityCardView visibleCard) ||
                visibleCard == null ||
                !visibleCard.gameObject.activeInHierarchy)
            {
                return false;
            }

            cardView = visibleCard;
            return true;
        }

        public bool TryGetVisibleCardScreenCenter(string abilityId, out Vector2 screenCenter)
        {
            screenCenter = Vector2.zero;
            if (!TryGetVisibleCardView(abilityId, out DuelAbilityCardView card))
            {
                return false;
            }

            RectTransform cardRect = card.transform as RectTransform;
            return TryGetRectScreenCenter(cardRect, out screenCenter);
        }

        public bool SetCardVisible(string abilityId, bool isVisible)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            if (!cardViewsByInstanceId.TryGetValue(abilityId, out DuelAbilityCardView card) ||
                card == null)
            {
                if (!TryGetVisibleCardView(abilityId, out card))
                {
                    return false;
                }
            }

            card.gameObject.SetActive(isVisible);
            return true;
        }

        public bool TryGetCombatSlotScreenCenter(
            int combatIndex,
            bool isPlayerSide,
            int slotIndex,
            out Vector2 screenCenter)
        {
            screenCenter = Vector2.zero;

            if (combatZones == null ||
                combatIndex < 0 ||
                combatIndex >= combatZones.Length ||
                slotIndex < 0)
            {
                return false;
            }

            DuelCombatZoneView zone = combatZones[combatIndex];
            if (zone == null)
            {
                return false;
            }

            zone.EnsureRowsAndSlots();
            IReadOnlyList<RectTransform> slots = isPlayerSide
                ? zone.PlayerSlots
                : zone.EnemySlots;
            if (slots == null || slotIndex >= slots.Count)
            {
                return false;
            }

            return TryGetRectScreenCenter(slots[slotIndex], out screenCenter);
        }

        public IEnumerator AnimateRoll(DuelAnimationConfig animationConfig)
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
                    DuelCombatZoneView zone = combatZones[zoneIndex];
                    zone?.SetRollPulse(normalized);
                }

                for (int i = 0; i < activeCardViews.Count; i++)
                {
                    DuelAbilityCardView card = activeCardViews[i];
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
                DuelCombatZoneView zone = combatZones[zoneIndex];
                zone?.RestoreBaseVisual();
            }

            for (int i = 0; i < activeCardViews.Count; i++)
            {
                DuelAbilityCardView card = activeCardViews[i];
                if (card == null || !card.gameObject.activeInHierarchy)
                {
                    continue;
                }

                card.RestoreVisual();
            }
        }

        public IEnumerator AnimateResolve(
            DuelCombatResolveResult resolveResult,
            DuelAnimationConfig animationConfig)
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

                DuelCombatZoneView zone = combatZones[step.combatIndex];
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
            DuelAnimationConfig animationConfig)
        {
            if (animationConfig == null || combatIndex < 0 || combatIndex >= combatZones.Length)
            {
                yield break;
            }

            DuelCombatZoneView zone = combatZones[combatIndex];
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
            Action<DuelAbilityCardView, string, DuelAbilityCardView.InteractionContext, Vector2, Camera> onCardDragStart,
            Action<DuelAbilityCardView, string, DuelAbilityCardView.InteractionContext, Vector2, Camera> onCardDragMove,
            Action<DuelAbilityCardView, string, DuelAbilityCardView.InteractionContext, Vector2, Camera> onCardDragEnd,
            Action<DuelAbilityCardView, string, DuelAbilityCardView.InteractionContext> onCardRightClick)
        {
            if (duelState == null || database == null)
            {
                return;
            }

            List<DuelAbilityCardView.BindData> enemyCards = ExpandOpponentLoadoutCards(
                duelState,
                database,
                abilityType => abilityType != AbilityType.Passive);
            for (int i = 0; i < enemyCards.Count; i++)
            {
                DuelAbilityCardView.BindData bindData = enemyCards[i];
                DuelAbilityCardView card = AcquireCardForAbility(bindData.instanceId, enemyLoadoutRow);
                if (card == null)
                {
                    continue;
                }

                card.Bind(
                    bindData,
                    false,
                    false,
                    null,
                    DuelAbilityCardView.InteractionContext.None,
                    null,
                    null,
                    null,
                    null);
            }

            List<string> playerLoadoutAbilityIds = CollectLoadoutAbilityIdsByType(
                duelState,
                DuelSide.Player,
                abilityType => abilityType != AbilityType.Passive);
            for (int i = 0; i < playerLoadoutAbilityIds.Count; i++)
            {
                string abilityId = playerLoadoutAbilityIds[i];
                if (!TryResolveAbilityAndDef(duelState, database, abilityId, out AbilityInstance ability, out AbilityDef def))
                {
                    continue;
                }

                bool isInteractable = !isFlowRunning &&
                    !duelState.isDuelEnded &&
                    phaseRunner != null &&
                    phaseRunner.currentPhase == DuelPhase.PlayerSetup &&
                    ability.abilityType == AbilityType.Attack &&
                    ability.cooldownRemaining <= 0;
                bool isSelected = string.Equals(selectedAbilityId, abilityId, StringComparison.Ordinal);
                DuelAbilityCardView.BindData bindData = CreateBindData(abilityId, ability, def);
                DuelAbilityCardView card = AcquireCardForAbility(abilityId, playerLoadoutRow);
                if (card == null)
                {
                    continue;
                }

                card.Bind(
                    bindData,
                    isSelected,
                    isInteractable,
                    onPlayerAbilityClicked,
                    DuelAbilityCardView.InteractionContext.Loadout,
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
            Action<DuelAbilityCardView, string, DuelAbilityCardView.InteractionContext, Vector2, Camera> onCardDragStart,
            Action<DuelAbilityCardView, string, DuelAbilityCardView.InteractionContext, Vector2, Camera> onCardDragMove,
            Action<DuelAbilityCardView, string, DuelAbilityCardView.InteractionContext, Vector2, Camera> onCardDragEnd,
            Action<DuelAbilityCardView, string, DuelAbilityCardView.InteractionContext> onCardRightClick)
        {
            if (combatZones == null || combatZones.Length <= 0)
            {
                return;
            }

            for (int zoneIndex = 0; zoneIndex < combatZones.Length; zoneIndex++)
            {
                DuelCombatZoneView zone = combatZones[zoneIndex];
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

        void RenderPassiveRows(
            DuelState duelState,
            GameDatabase database)
        {
            if (duelState == null || database == null)
            {
                return;
            }

            List<DuelAbilityCardView.BindData> enemyPassiveCards = ExpandOpponentLoadoutCards(
                duelState,
                database,
                abilityType => abilityType == AbilityType.Passive);
            for (int i = 0; i < enemyPassiveCards.Count; i++)
            {
                DuelAbilityCardView.BindData bindData = enemyPassiveCards[i];
                DuelAbilityCardView card = AcquireCardForAbility(bindData.instanceId, enemyPassiveRow);
                if (card == null)
                {
                    continue;
                }

                card.Bind(
                    bindData,
                    false,
                    false,
                    null,
                    DuelAbilityCardView.InteractionContext.None,
                    null,
                    null,
                    null,
                    null);
            }

            List<string> playerPassiveAbilityIds = CollectLoadoutAbilityIdsByType(
                duelState,
                DuelSide.Player,
                abilityType => abilityType == AbilityType.Passive);
            for (int i = 0; i < playerPassiveAbilityIds.Count; i++)
            {
                string abilityId = playerPassiveAbilityIds[i];
                if (!TryResolveAbilityAndDef(duelState, database, abilityId, out AbilityInstance ability, out AbilityDef def))
                {
                    continue;
                }

                DuelAbilityCardView.BindData bindData = CreateBindData(abilityId, ability, def);
                DuelAbilityCardView card = AcquireCardForAbility(abilityId, playerPassiveRow);
                if (card == null)
                {
                    continue;
                }

                card.Bind(
                    bindData,
                    false,
                    false,
                    null,
                    DuelAbilityCardView.InteractionContext.None,
                    null,
                    null,
                    null,
                    null);
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
            Action<DuelAbilityCardView, string, DuelAbilityCardView.InteractionContext, Vector2, Camera> onCardDragStart,
            Action<DuelAbilityCardView, string, DuelAbilityCardView.InteractionContext, Vector2, Camera> onCardDragMove,
            Action<DuelAbilityCardView, string, DuelAbilityCardView.InteractionContext, Vector2, Camera> onCardDragEnd,
            Action<DuelAbilityCardView, string, DuelAbilityCardView.InteractionContext> onCardRightClick)
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

            int renderCount = Mathf.Min(abilityCount, slots.Count);
            for (int slotIndex = 0; slotIndex < renderCount; slotIndex++)
            {
                RectTransform slot = slots[slotIndex];
                if (slot == null)
                {
                    continue;
                }

                string abilityId = abilityIds[slotIndex];
                if (!TryResolveAbilityAndDef(duelState, database, abilityId, out AbilityInstance ability, out AbilityDef def))
                {
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
                DuelAbilityCardView.BindData bindData = CreateBindData(abilityId, ability, def);
                DuelAbilityCardView card = AcquireCardForAbility(abilityId, slot);
                if (card == null)
                {
                    continue;
                }

                DuelAbilityCardView.InteractionContext context = isPlayerSide
                    ? DuelAbilityCardView.InteractionContext.Combat(combatIndex)
                    : DuelAbilityCardView.InteractionContext.None;
                card.Bind(
                    bindData,
                    isSelected,
                    isInteractable,
                    onClick,
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

        void CacheVisibleCardsByInstanceId()
        {
            visibleCardsByInstanceId.Clear();
            for (int i = 0; i < activeCardViews.Count; i++)
            {
                DuelAbilityCardView card = activeCardViews[i];
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

        void ApplyRevealState(DuelRevealState revealState)
        {
            ApplyRollOverlayState(revealState);
            if (!revealState.isRunning)
            {
                return;
            }

            ApplyRevealZoneTotals(revealState);
            ApplyRevealPowerBadges(revealState);
        }

        void ApplyRollOverlayState(DuelRevealState revealState)
        {
            foreach (KeyValuePair<string, DuelAbilityCardView> pair in visibleCardsByInstanceId)
            {
                pair.Value?.HideRollOverlay();
            }

            if (!revealState.isRunning)
            {
                return;
            }

            foreach (KeyValuePair<string, DuelAbilityCardView> pair in visibleCardsByInstanceId)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                if (!revealState.TryGetOverlay(pair.Key, out DuelRollOverlayValue overlay) || !overlay.isVisible)
                {
                    continue;
                }

                pair.Value.SetRollOverlayValue(overlay.value, overlay.isFinal);
            }
        }

        void ApplyRevealZoneTotals(DuelRevealState revealState)
        {
            if (combatZones == null)
            {
                return;
            }

            for (int zoneIndex = 0; zoneIndex < combatZones.Length; zoneIndex++)
            {
                DuelCombatZoneView zone = combatZones[zoneIndex];
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

        void ApplyRevealPowerBadges(DuelRevealState revealState)
        {
            foreach (KeyValuePair<string, DuelAbilityCardView> pair in visibleCardsByInstanceId)
            {
                DuelAbilityCardView card = pair.Value;
                if (card == null)
                {
                    continue;
                }

                if (!revealState.TryGetPowerBadge(pair.Key, out int powerValue))
                {
                    continue;
                }

                card.SetPowerBadgeValue(powerValue);
            }
        }

        void ReleaseAllActiveCards()
        {
            for (int i = 0; i < activeCardViews.Count; i++)
            {
                DuelAbilityCardView card = activeCardViews[i];
                if (card == null)
                {
                    continue;
                }

                card.gameObject.SetActive(false);
                card.transform.SetParent(null, false);
                if (!reusableCardViews.Contains(card))
                {
                    reusableCardViews.Add(card);
                }
            }

            activeCardViews.Clear();
            visibleCardsByInstanceId.Clear();
        }

        DuelAbilityCardView AcquireCardForAbility(string abilityId, RectTransform parent)
        {
            if (string.IsNullOrWhiteSpace(abilityId) || parent == null)
            {
                return null;
            }

            DuelAbilityCardView card = null;
            if (cardViewsByInstanceId.TryGetValue(abilityId, out DuelAbilityCardView mappedCard))
            {
                if (mappedCard == null)
                {
                    cardViewsByInstanceId.Remove(abilityId);
                }
                else
                {
                    card = mappedCard;
                    reusableCardViews.Remove(card);
                }
            }

            if (card == null)
            {
                card = TakeReusableCard();
            }

            if (card == null)
            {
                card = CreateCardInstance(parent);
                if (card == null)
                {
                    return null;
                }
            }

            cardViewsByInstanceId[abilityId] = card;
            if (!activeCardViews.Contains(card))
            {
                activeCardViews.Add(card);
            }

            Transform transform = card.transform;
            transform.SetParent(parent, false);
            if (transform is RectTransform cardRect)
            {
                ConfigureCardRectForParent(cardRect, parent);
            }

            card.gameObject.SetActive(true);
            return card;
        }

        DuelAbilityCardView TakeReusableCard()
        {
            for (int i = reusableCardViews.Count - 1; i >= 0; i--)
            {
                DuelAbilityCardView card = reusableCardViews[i];
                reusableCardViews.RemoveAt(i);
                if (card == null)
                {
                    continue;
                }

                return card;
            }

            return null;
        }

        DuelAbilityCardView CreateCardInstance(RectTransform parent)
        {
            if (!UnityEngine.Application.isPlaying)
            {
                return null;
            }

            if (abilityCardPrefab == null)
            {
                Debug.LogError("[DuelScreenView] abilityCardPrefab is null. Cannot create on-demand cards.");
                return null;
            }

            DuelAbilityCardView card = UnityEngine.Object.Instantiate(abilityCardPrefab, parent, false);
            if (card == null)
            {
                return null;
            }

            card.gameObject.SetActive(false);
            return card;
        }

        static void ConfigureCardRectForParent(RectTransform cardRect, RectTransform parent)
        {
            if (cardRect == null || parent == null)
            {
                return;
            }

            cardRect.localScale = Vector3.one;
            if (parent.TryGetComponent(out LayoutGroup _))
            {
                return;
            }

            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
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

        List<DuelAbilityCardView.BindData> ExpandOpponentLoadoutCards(
            DuelState duelState,
            GameDatabase database,
            Func<AbilityType, bool> includePredicate)
        {
            var cards = new List<DuelAbilityCardView.BindData>();

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

                if (includePredicate != null && !includePredicate(ability.abilityType))
                {
                    continue;
                }

                cards.Add(CreateBindData(abilityId, ability, def));
            }

            return cards;
        }

        static List<string> CollectLoadoutAbilityIdsByType(
            DuelState duelState,
            DuelSide side,
            Func<AbilityType, bool> includePredicate)
        {
            var filteredIds = new List<string>();
            if (duelState?.abilitiesById == null)
            {
                return filteredIds;
            }

            List<string> sourceIds = side == DuelSide.Player
                ? duelState.loadoutAbilityIds
                : duelState.opponentLoadoutAbilityIds;
            if (sourceIds == null)
            {
                return filteredIds;
            }

            for (int i = 0; i < sourceIds.Count; i++)
            {
                string abilityId = sourceIds[i];
                if (string.IsNullOrWhiteSpace(abilityId))
                {
                    continue;
                }

                if (!duelState.abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) || ability == null)
                {
                    continue;
                }

                ability.EnsureInitialized();
                if (includePredicate != null && !includePredicate(ability.abilityType))
                {
                    continue;
                }

                filteredIds.Add(abilityId);
            }

            return filteredIds;
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

        DuelAbilityCardView.BindData CreateBindData(
            string abilityId,
            AbilityInstance ability,
            AbilityDef def)
        {
            int displayPower = ResolveDisplayedPower(ability);
            string localizedTitle = abilityTextFormatter.FormatName(def);
            string localizedBody = abilityTextFormatter.FormatDescription(def, ability);
            Sprite iconSprite = resolveAbilityIcon == null
                ? null
                : resolveAbilityIcon.Invoke(def.iconId);

            return new DuelAbilityCardView.BindData(
                abilityId,
                localizedTitle,
                localizedBody,
                ability.abilityType,
                iconSprite,
                displayPower,
                ability.abilityType == AbilityType.Attack,
                ability.cooldownTurns,
                ability.cooldownRemaining);
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

                int displayedPower = ResolveDisplayedPower(ability);
                int value = usePowerResult && ability.powerResult > 0
                    ? ability.powerResult
                    : displayedPower;
                total += Mathf.Max(0, value);
            }

            return total;
        }

        static int ResolveDisplayedPower(AbilityInstance ability)
        {
            if (ability == null)
            {
                return 0;
            }

            ability.EnsureInitialized();
            return NumericModifierCalculator.Apply(
                ability.power,
                ability.powerModifiers,
                minValue: 0,
                logContext: "DuelScreenView.ResolveDisplayedPower");
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

            if (enemyPassiveRow != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(enemyPassiveRow);
            }

            if (playerPassiveRow != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(playerPassiveRow);
            }
        }

        static bool TryGetRectScreenCenter(RectTransform rectTransform, out Vector2 screenCenter)
        {
            screenCenter = Vector2.zero;
            if (rectTransform == null)
            {
                return false;
            }

            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            Camera canvasCamera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;
            Vector3 worldCenter = rectTransform.TransformPoint(rectTransform.rect.center);
            screenCenter = RectTransformUtility.WorldToScreenPoint(canvasCamera, worldCenter);
            return true;
        }
    }
}



