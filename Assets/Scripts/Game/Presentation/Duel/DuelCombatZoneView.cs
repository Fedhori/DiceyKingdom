using System;
using System.Collections.Generic;
using Game.Domain.Duel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Duel
{
    public class DuelCombatZoneView : MonoBehaviour
    {
        const int slotCountPerSide = 6;

        static readonly Color defaultZoneBackground = Colors.Semantic.SurfaceParchment;
        static readonly Color defaultZoneBorder = Colors.Semantic.BorderParchment;
        static readonly Color defaultDividerColor = Colors.Semantic.DividerParchment;
        static readonly Color defaultEnemyTotalColor = Colors.Semantic.StateDanger;
        static readonly Color defaultPlayerTotalColor = Colors.Semantic.StateInfo;
        static readonly Color rollPulseOverlay = Colors.Semantic.HighlightSheen;
        static readonly Color resolveVictoryTint = Colors.Semantic.StatePositiveTint;
        static readonly Color resolveDefeatTint = Colors.Semantic.StateDangerTint;
        static readonly Color dragHoverTint = Colors.Semantic.HighlightSheen;

        [Header("References")]
        [SerializeField] Button zoneButton;
        [SerializeField] Image zoneBackgroundImage;
        [SerializeField] Outline zoneBorderOutline;
        [SerializeField] Image dividerImage;
        [SerializeField] RectTransform enemySlotsRow;
        [SerializeField] RectTransform playerSlotsRow;
        [SerializeField] TMP_Text enemyTotalText;
        [SerializeField] TMP_Text playerTotalText;

        [Header("Runtime")]
        [SerializeField] int combatIndex;

        readonly List<RectTransform> enemySlots = new();
        readonly List<RectTransform> playerSlots = new();
        Action<int> clickHandler;
        Color zoneBaseColor = defaultZoneBackground;
        bool isDragHovering;

        public int CombatIndex => combatIndex;
        public IReadOnlyList<RectTransform> EnemySlots => enemySlots;
        public IReadOnlyList<RectTransform> PlayerSlots => playerSlots;

        void Awake()
        {
            if (!ValidateRequiredReferences("Awake"))
            {
                enabled = false;
                return;
            }

            EnsureRowsAndSlots();
            ApplyStaticVisuals();

            if (zoneButton != null)
            {
                zoneButton.onClick.RemoveListener(HandleZoneClick);
                zoneButton.onClick.AddListener(HandleZoneClick);
            }
        }

        void OnDestroy()
        {
            if (zoneButton != null)
            {
                zoneButton.onClick.RemoveListener(HandleZoneClick);
            }
        }

        void OnValidate()
        {
            ValidateRequiredReferences("OnValidate");
        }

        bool ValidateRequiredReferences(string stage)
        {
            var missing = new List<string>();
            if (zoneButton == null)
            {
                missing.Add(nameof(zoneButton));
            }

            if (zoneBackgroundImage == null)
            {
                missing.Add(nameof(zoneBackgroundImage));
            }

            if (zoneBorderOutline == null)
            {
                missing.Add(nameof(zoneBorderOutline));
            }

            if (enemySlotsRow == null)
            {
                missing.Add(nameof(enemySlotsRow));
            }

            if (playerSlotsRow == null)
            {
                missing.Add(nameof(playerSlotsRow));
            }

            if (enemyTotalText == null)
            {
                missing.Add(nameof(enemyTotalText));
            }

            if (playerTotalText == null)
            {
                missing.Add(nameof(playerTotalText));
            }

            if (dividerImage == null)
            {
                missing.Add(nameof(dividerImage));
            }

            if (missing.Count == 0)
            {
                return true;
            }

            Debug.LogError(
                $"[DuelCombatZoneView] Missing serialized references at {stage} on '{name}': {string.Join(", ", missing)}",
                this);
            return false;
        }

        public void SetCombatIndex(int index)
        {
            combatIndex = index;
        }

        public void SetClickHandler(Action<int> onClicked)
        {
            clickHandler = onClicked;
        }

        public void SetInteractable(bool isInteractable)
        {
            if (zoneButton != null)
            {
                zoneButton.interactable = isInteractable;
            }
        }

        public void SetTotals(int enemyTotalPower, int playerTotalPower)
        {
            if (enemyTotalText != null)
            {
                enemyTotalText.text = enemyTotalPower.ToString();
                enemyTotalText.color = defaultEnemyTotalColor;
            }

            if (playerTotalText != null)
            {
                playerTotalText.text = playerTotalPower.ToString();
                playerTotalText.color = defaultPlayerTotalColor;
            }
        }

        public bool ContainsScreenPoint(Vector2 screenPoint, Camera eventCamera)
        {
            if (!(transform is RectTransform rectTransform))
            {
                return false;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, eventCamera);
        }

        public void SetDragHover(bool isHovering)
        {
            if (isDragHovering == isHovering)
            {
                return;
            }

            isDragHovering = isHovering;
            ApplyBaseOrHoverVisual();
        }

        public void SetRollPulse(float normalized)
        {
            if (zoneBackgroundImage == null)
            {
                return;
            }

            zoneBackgroundImage.color = Color.Lerp(zoneBaseColor, rollPulseOverlay, normalized);
        }

        public void SetResolveHighlight(DuelOutcome outcome, float normalized)
        {
            if (zoneBackgroundImage == null)
            {
                return;
            }

            Color target = zoneBaseColor;
            switch (outcome)
            {
                case DuelOutcome.Victory:
                    target = resolveVictoryTint;
                    break;
                case DuelOutcome.Defeat:
                    target = resolveDefeatTint;
                    break;
                case DuelOutcome.Draw:
                    target = rollPulseOverlay;
                    break;
            }

            zoneBackgroundImage.color = Color.Lerp(zoneBaseColor, target, normalized);
        }

        public void RestoreBaseVisual()
        {
            ApplyBaseOrHoverVisual();
        }

        public void EnsureRowsAndSlots()
        {
            enemySlots.Clear();
            playerSlots.Clear();

            CollectSlots(enemySlotsRow, enemySlots, slotCountPerSide);
            CollectSlots(playerSlotsRow, playerSlots, slotCountPerSide);

            if (enemySlots.Count != slotCountPerSide || playerSlots.Count != slotCountPerSide)
            {
                UnityEngine.Debug.LogWarning(
                    $"[DuelCombatZoneView] Slot count mismatch at combat({combatIndex}). " +
                    $"enemySlots={enemySlots.Count}, playerSlots={playerSlots.Count}, expected={slotCountPerSide}");
            }
        }

        void ApplyStaticVisuals()
        {
            zoneBaseColor = defaultZoneBackground;

            if (zoneBackgroundImage != null)
            {
                zoneBackgroundImage.color = zoneBaseColor;
            }

            if (zoneBorderOutline != null)
            {
                zoneBorderOutline.effectColor = defaultZoneBorder;
                zoneBorderOutline.effectDistance = new Vector2(1f, -1f);
            }

            if (dividerImage != null)
            {
                dividerImage.color = defaultDividerColor;
            }
        }

        void ApplyBaseOrHoverVisual()
        {
            if (zoneBackgroundImage == null)
            {
                return;
            }

            if (isDragHovering)
            {
                zoneBackgroundImage.color = Color.Lerp(zoneBaseColor, dragHoverTint, 0.35f);
                return;
            }

            zoneBackgroundImage.color = zoneBaseColor;
        }

        static void CollectSlots(RectTransform row, List<RectTransform> buffer, int maxCount)
        {
            buffer.Clear();
            if (row == null)
            {
                return;
            }

            for (int i = 0; i < row.childCount && buffer.Count < maxCount; i++)
            {
                Transform child = row.GetChild(i);
                if (child is RectTransform rect)
                {
                    buffer.Add(rect);
                }
            }
        }

        void HandleZoneClick()
        {
            clickHandler?.Invoke(combatIndex);
        }

    }
}


