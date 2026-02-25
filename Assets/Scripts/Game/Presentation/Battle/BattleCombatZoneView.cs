using System;
using System.Collections.Generic;
using Game.Domain.Duel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Battle
{
    public sealed class BattleCombatZoneView : MonoBehaviour
    {
        const int slotCountPerSide = 6;

        static readonly Color defaultZoneBackground = Colors.Semantic.SurfaceParchment;
        static readonly Color defaultZoneBorder = Colors.Semantic.BorderParchment;
        static readonly Color defaultDividerColor = Colors.Semantic.DividerParchment;
        static readonly Color defaultTotalPanelBackground = Colors.Semantic.SurfaceSecondary;
        static readonly Color defaultTotalPanelBorder = Colors.Semantic.BorderStrong;
        static readonly Color defaultEnemyTotalColor = Colors.Semantic.StateDanger;
        static readonly Color defaultPlayerTotalColor = Colors.Semantic.StateInfo;
        static readonly Color rollPulseOverlay = Colors.Semantic.HighlightSheen;
        static readonly Color resolveVictoryTint = Colors.Semantic.StatePositiveTint;
        static readonly Color resolveDefeatTint = Colors.Semantic.StateDangerTint;

        [Header("References")]
        [SerializeField] Button zoneButton;
        [SerializeField] Image zoneBackgroundImage;
        [SerializeField] Outline zoneBorderOutline;
        [SerializeField] Image dividerImage;
        [SerializeField] RectTransform enemySlotsRow;
        [SerializeField] RectTransform playerSlotsRow;
        [SerializeField] Image enemyTotalPanelImage;
        [SerializeField] Image playerTotalPanelImage;
        [SerializeField] TMP_Text enemyTotalText;
        [SerializeField] TMP_Text playerTotalText;

        [Header("Runtime")]
        [SerializeField] int combatIndex;

        readonly List<RectTransform> enemySlots = new();
        readonly List<RectTransform> playerSlots = new();
        Action<int> clickHandler;
        Color zoneBaseColor = defaultZoneBackground;

        public int CombatIndex => combatIndex;
        public IReadOnlyList<RectTransform> EnemySlots => enemySlots;
        public IReadOnlyList<RectTransform> PlayerSlots => playerSlots;

        void Awake()
        {
            CacheReferencesIfNeeded();
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
            CacheReferencesIfNeeded();
        }

        void CacheReferencesIfNeeded()
        {
            if (zoneButton == null)
            {
                zoneButton = GetComponent<Button>();
            }

            if (zoneBackgroundImage == null)
            {
                zoneBackgroundImage = GetComponent<Image>();
            }

            if (zoneBorderOutline == null)
            {
                zoneBorderOutline = GetComponent<Outline>();
            }

            if (enemySlotsRow == null)
            {
                enemySlotsRow = ResolveRect("EnemySlotsRow");
            }

            if (playerSlotsRow == null)
            {
                playerSlotsRow = ResolveRect("PlayerSlotsRow");
            }

            if (enemyTotalPanelImage == null)
            {
                enemyTotalPanelImage = ResolveImage("EnemyTotalPanel");
            }

            if (playerTotalPanelImage == null)
            {
                playerTotalPanelImage = ResolveImage("PlayerTotalPanel");
            }

            if (enemyTotalText == null)
            {
                enemyTotalText = ResolveText("EnemyTotalText");
            }

            if (playerTotalText == null)
            {
                playerTotalText = ResolveText("PlayerTotalText");
            }

            if (dividerImage == null)
            {
                dividerImage = ResolveImage("MiddleDivider");
            }
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
            if (zoneBackgroundImage != null)
            {
                zoneBackgroundImage.color = zoneBaseColor;
            }
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
                    $"[BattleCombatZoneView] Slot count mismatch at combat({combatIndex}). " +
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

            ApplyTotalPanelVisual(enemyTotalPanelImage);
            ApplyTotalPanelVisual(playerTotalPanelImage);
        }

        static void ApplyTotalPanelVisual(Image panelImage)
        {
            if (panelImage == null)
            {
                return;
            }

            panelImage.color = defaultTotalPanelBackground;
            if (panelImage.TryGetComponent(out Outline outline))
            {
                outline.effectColor = defaultTotalPanelBorder;
                outline.effectDistance = new Vector2(1f, -1f);
            }
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

        RectTransform ResolveRect(string childName)
        {
            Transform child = transform.Find(childName);
            if (child is RectTransform rect)
            {
                return rect;
            }

            return null;
        }

        Image ResolveImage(string childName)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                return null;
            }

            if (child.TryGetComponent(out Image image))
            {
                return image;
            }

            return null;
        }

        TMP_Text ResolveText(string childName)
        {
            Transform child = transform.Find(childName);
            if (child == null)
            {
                return null;
            }

            if (child.TryGetComponent(out TMP_Text text))
            {
                return text;
            }

            return null;
        }
    }
}
