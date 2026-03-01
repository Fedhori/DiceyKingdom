using System;
using Game.Application.Duel;
using Game.Domain.Duel;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Duel
{
    public sealed class DuelScreenDragDropInput
    {
        const float dragGhostAlpha = 0.85f;

        readonly Func<Canvas> resolveCanvas;
        readonly DuelCombatZoneView[] combatZones;
        readonly DuelSessionRunner sessionRunner;
        readonly DuelSelectionState selectionState;
        readonly DuelUiQueryService uiQueryService;
        readonly Func<bool> isFlowRunning;
        readonly Action publishState;

        bool isCardDragActive;
        string dragAbilityInstanceId = string.Empty;
        RectTransform dragGhostRect;
        DuelCombatZoneView hoveredDropZone;

        public bool IsCardDragActive => isCardDragActive;

        public DuelScreenDragDropInput(
            Func<Canvas> resolveCanvas,
            DuelCombatZoneView[] combatZones,
            DuelSessionRunner sessionRunner,
            DuelSelectionState selectionState,
            DuelUiQueryService uiQueryService,
            Func<bool> isFlowRunning,
            Action publishState)
        {
            this.resolveCanvas = resolveCanvas ?? throw new ArgumentNullException(nameof(resolveCanvas));
            this.combatZones = combatZones ?? Array.Empty<DuelCombatZoneView>();
            this.sessionRunner = sessionRunner ?? throw new ArgumentNullException(nameof(sessionRunner));
            this.selectionState = selectionState ?? throw new ArgumentNullException(nameof(selectionState));
            this.uiQueryService = uiQueryService ?? throw new ArgumentNullException(nameof(uiQueryService));
            this.isFlowRunning = isFlowRunning ?? throw new ArgumentNullException(nameof(isFlowRunning));
            this.publishState = publishState ?? throw new ArgumentNullException(nameof(publishState));
        }

        public void HandleCardDragStarted(
            DuelAbilityCardView cardView,
            string abilityInstanceId,
            DuelAbilityCardView.InteractionContext context,
            Vector2 screenPosition,
            Camera eventCamera)
        {
            _ = context;

            if (!CanUseCardInteractions(abilityInstanceId))
            {
                return;
            }

            if (cardView == null)
            {
                return;
            }

            isCardDragActive = true;
            dragAbilityInstanceId = abilityInstanceId;

            CreateDragGhost(cardView, screenPosition, eventCamera);
            UpdateDropZoneHover(screenPosition, eventCamera);
        }

        public void HandleCardDragMoved(
            DuelAbilityCardView cardView,
            string abilityInstanceId,
            DuelAbilityCardView.InteractionContext context,
            Vector2 screenPosition,
            Camera eventCamera)
        {
            _ = cardView;
            _ = context;

            if (!isCardDragActive || !string.Equals(dragAbilityInstanceId, abilityInstanceId, StringComparison.Ordinal))
            {
                return;
            }

            UpdateDragGhostPosition(screenPosition, eventCamera);
            UpdateDropZoneHover(screenPosition, eventCamera);
        }

        public void HandleCardDragEnded(
            DuelAbilityCardView cardView,
            string abilityInstanceId,
            DuelAbilityCardView.InteractionContext context,
            Vector2 screenPosition,
            Camera eventCamera)
        {
            if (!isCardDragActive || !string.Equals(dragAbilityInstanceId, abilityInstanceId, StringComparison.Ordinal))
            {
                return;
            }

            bool shouldPublishState = false;
            bool isDropFailure = false;
            if (CanUseCardInteractions(abilityInstanceId) &&
                TryFindDropCombatIndex(screenPosition, eventCamera, out int targetCombatIndex))
            {
                bool isNoOp = context.isCombat && context.combatIndex == targetCombatIndex;
                if (!isNoOp)
                {
                    if (selectionState.TryMovePlayerAbilityToCombat(
                            sessionRunner.DuelState,
                            sessionRunner.PhaseRunner,
                            abilityInstanceId,
                            targetCombatIndex,
                            out string failureMessage))
                    {
                        sessionRunner.NotifyPlayerAbilityDeployed(abilityInstanceId);
                        shouldPublishState = true;
                    }
                    else
                    {
                        isDropFailure = true;
                        Debug.LogWarning($"[DuelScreenDragDropInput] Drag move rejected: {failureMessage}");
                    }
                }
            }
            else
            {
                isDropFailure = true;
            }

            if (isDropFailure && cardView != null)
            {
                cardView.PlayInvalidDropFeedback();
            }

            ClearDragState();

            if (shouldPublishState)
            {
                publishState.Invoke();
            }
        }

        public void HandleCardRightClicked(
            DuelAbilityCardView cardView,
            string abilityInstanceId,
            DuelAbilityCardView.InteractionContext context)
        {
            _ = cardView;

            if (!context.isCombat || !CanUseCardInteractions(abilityInstanceId))
            {
                return;
            }

            if (!selectionState.TryReturnPlayerAbilityToLoadout(
                    sessionRunner.DuelState,
                    sessionRunner.PhaseRunner,
                    abilityInstanceId,
                    out string failureMessage))
            {
                Debug.LogWarning($"[DuelScreenDragDropInput] Return to loadout rejected: {failureMessage}");
                return;
            }

            sessionRunner.NotifyBoardCompositionChanged();
            publishState.Invoke();
        }

        public void ClearDragState()
        {
            if (hoveredDropZone != null)
            {
                hoveredDropZone.SetDragHover(false);
                hoveredDropZone = null;
            }

            DestroyDragGhost();

            isCardDragActive = false;
            dragAbilityInstanceId = string.Empty;
        }

        bool CanUseCardInteractions(string abilityInstanceId)
        {
            if (isFlowRunning.Invoke() ||
                !sessionRunner.IsInitialized ||
                sessionRunner.DuelState == null ||
                sessionRunner.PhaseRunner == null ||
                sessionRunner.DuelState.isDuelEnded ||
                sessionRunner.PhaseRunner.currentPhase != DuelPhase.PlayerSetup ||
                string.IsNullOrWhiteSpace(abilityInstanceId))
            {
                return false;
            }

            if (!sessionRunner.DuelState.abilitiesById.TryGetValue(abilityInstanceId, out AbilityInstance ability) ||
                ability == null)
            {
                return false;
            }

            return uiQueryService.IsAttackDeployable(sessionRunner.DuelState, abilityInstanceId);
        }

        bool TryFindDropCombatIndex(Vector2 screenPosition, Camera eventCamera, out int combatIndex)
        {
            combatIndex = -1;
            if (combatZones == null)
            {
                return false;
            }

            for (int i = 0; i < combatZones.Length; i++)
            {
                DuelCombatZoneView zone = combatZones[i];
                if (zone == null || !zone.ContainsScreenPoint(screenPosition, eventCamera))
                {
                    continue;
                }

                combatIndex = i;
                return true;
            }

            return false;
        }

        void UpdateDropZoneHover(Vector2 screenPosition, Camera eventCamera)
        {
            DuelCombatZoneView nextHovered = null;
            if (combatZones != null)
            {
                for (int i = 0; i < combatZones.Length; i++)
                {
                    DuelCombatZoneView zone = combatZones[i];
                    if (zone != null && zone.ContainsScreenPoint(screenPosition, eventCamera))
                    {
                        nextHovered = zone;
                        break;
                    }
                }
            }

            if (hoveredDropZone == nextHovered)
            {
                return;
            }

            if (hoveredDropZone != null)
            {
                hoveredDropZone.SetDragHover(false);
            }

            hoveredDropZone = nextHovered;
            if (hoveredDropZone != null)
            {
                hoveredDropZone.SetDragHover(true);
            }
        }

        void CreateDragGhost(DuelAbilityCardView sourceCard, Vector2 screenPosition, Camera eventCamera)
        {
            DestroyDragGhost();

            if (sourceCard == null)
            {
                return;
            }

            Canvas canvas = resolveCanvas.Invoke();
            RectTransform canvasRect = canvas == null ? null : canvas.transform as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            GameObject ghostObject = UnityEngine.Object.Instantiate(sourceCard.gameObject, canvasRect, false);
            ghostObject.name = "CardDragGhost";

            if (ghostObject.TryGetComponent(out DuelAbilityCardView ghostCard))
            {
                ghostCard.enabled = false;
            }

            if (ghostObject.TryGetComponent(out Button ghostButton))
            {
                ghostButton.interactable = false;
                ghostButton.enabled = false;
            }

            Graphic[] graphics = ghostObject.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null)
                {
                    continue;
                }

                Color color = graphic.color;
                color.a *= dragGhostAlpha;
                graphic.color = color;
                graphic.raycastTarget = false;
            }

            dragGhostRect = ghostObject.transform as RectTransform;
            if (dragGhostRect == null)
            {
                if (UnityEngine.Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(ghostObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(ghostObject);
                }

                return;
            }

            if (sourceCard.transform is RectTransform sourceRect)
            {
                dragGhostRect.anchorMin = new Vector2(0.5f, 0.5f);
                dragGhostRect.anchorMax = new Vector2(0.5f, 0.5f);
                dragGhostRect.pivot = new Vector2(0.5f, 0.5f);
                dragGhostRect.sizeDelta = sourceRect.rect.size;
            }

            dragGhostRect.SetAsLastSibling();
            UpdateDragGhostPosition(screenPosition, eventCamera);
        }

        void UpdateDragGhostPosition(Vector2 screenPosition, Camera eventCamera)
        {
            if (dragGhostRect == null)
            {
                return;
            }

            RectTransform canvasRect = dragGhostRect.parent as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            dragGhostRect.anchoredPosition = localPoint;
        }

        void DestroyDragGhost()
        {
            if (dragGhostRect == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(dragGhostRect.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(dragGhostRect.gameObject);
            }

            dragGhostRect = null;
        }
    }
}
