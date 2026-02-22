using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Debug
{
    public sealed class BattleTroopBlockView : MonoBehaviour
    {
        static readonly Vector2 defaultBlockPreferredSize = new Vector2(360f, 68f);
        static readonly Color defaultPlayerBackgroundColor = new Color32(68, 129, 192, 220);
        static readonly Color defaultEnemyBackgroundColor = new Color32(160, 82, 82, 220);
        static readonly Color defaultPlayerSelectedColor = new Color32(35, 92, 168, 255);
        static readonly Color defaultEnemySelectedColor = new Color32(176, 54, 54, 255);
        static readonly Color defaultPlayerAttackColor = new Color32(255, 233, 165, 255);
        static readonly Color defaultEnemyAttackColor = new Color32(255, 201, 201, 255);

        [SerializeField] Image backgroundImage;
        [SerializeField] Button selectButton;
        [SerializeField] TMP_Text troopDefIdText;
        [SerializeField] TMP_Text effectsText;
        [SerializeField] TMP_Text attackResultText;
        [SerializeField] TMP_Text attackText;
        [SerializeField] bool enforcePrefabSize = true;
        [SerializeField] Vector2 blockPreferredSize = defaultBlockPreferredSize;
        [SerializeField] Color playerBackgroundColor = defaultPlayerBackgroundColor;
        [SerializeField] Color enemyBackgroundColor = defaultEnemyBackgroundColor;
        [SerializeField] Color playerSelectedColor = defaultPlayerSelectedColor;
        [SerializeField] Color enemySelectedColor = defaultEnemySelectedColor;
        [SerializeField] Color playerAttackColor = defaultPlayerAttackColor;
        [SerializeField] Color enemyAttackColor = defaultEnemyAttackColor;

        Action onSelected;

        void Awake()
        {
            ApplyPrefabSizeDefaults();

            if (ApplyDefaultColorsIfInvalid())
            {
                UnityEngine.Debug.LogWarning("[BattleTroopBlockView] Color fields were invalid and reset to defaults.");
            }

            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(HandleClick);
                selectButton.onClick.AddListener(HandleClick);
            }
        }

        void OnDestroy()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(HandleClick);
            }
        }

        void OnValidate()
        {
            ApplyPrefabSizeDefaults();
            ApplyDefaultColorsIfInvalid();

            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            if (selectButton == null)
            {
                selectButton = GetComponent<Button>();
            }

            troopDefIdText = ResolveChildText(troopDefIdText, "TroopDefIdText");
            effectsText = ResolveChildText(effectsText, "EffectsText");
            attackResultText = ResolveChildText(attackResultText, "AttackResultText");
            attackText = ResolveChildText(attackText, "AttackText");
        }

        public void Bind(
            string troopDefId,
            int attack,
            int attackResult,
            string effectsLabel,
            bool isPlayerSide,
            bool isSelected,
            bool isSelectable,
            Action onSelected)
        {
            this.onSelected = onSelected;

            SetText(troopDefIdText, troopDefId);
            SetText(attackText, attack.ToString());
            SetText(attackResultText, $"Result {attackResult}");
            SetText(effectsText, $"Effects: {effectsLabel}");

            if (attackText != null)
            {
                attackText.fontStyle = FontStyles.Bold;
            }

            Color baseColor = isPlayerSide
                ? playerBackgroundColor
                : enemyBackgroundColor;
            Color selectedColor = isPlayerSide
                ? playerSelectedColor
                : enemySelectedColor;

            if (backgroundImage != null)
            {
                backgroundImage.color = isSelected ? selectedColor : baseColor;
                backgroundImage.raycastTarget = isSelectable;
            }

            if (selectButton != null)
            {
                selectButton.interactable = isSelectable;
                selectButton.enabled = isSelectable;
            }

            Color valueColor = isPlayerSide
                ? playerAttackColor
                : enemyAttackColor;
            if (attackText != null)
            {
                attackText.color = valueColor;
            }
        }

        void HandleClick()
        {
            onSelected?.Invoke();
        }

        static void SetText(TMP_Text target, string value)
        {
            if (target == null)
            {
                return;
            }

            target.text = value ?? string.Empty;
        }

        TMP_Text ResolveChildText(TMP_Text currentValue, string childName)
        {
            if (currentValue != null)
            {
                return currentValue;
            }

            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (!string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (child.TryGetComponent(out TMP_Text text))
                {
                    return text;
                }
            }

            return null;
        }

        bool ApplyDefaultColorsIfInvalid()
        {
            bool changed = false;

            changed |= TryRestoreColor(ref playerBackgroundColor, defaultPlayerBackgroundColor);
            changed |= TryRestoreColor(ref enemyBackgroundColor, defaultEnemyBackgroundColor);
            changed |= TryRestoreColor(ref playerSelectedColor, defaultPlayerSelectedColor);
            changed |= TryRestoreColor(ref enemySelectedColor, defaultEnemySelectedColor);
            changed |= TryRestoreColor(ref playerAttackColor, defaultPlayerAttackColor);
            changed |= TryRestoreColor(ref enemyAttackColor, defaultEnemyAttackColor);

            return changed;
        }

        void ApplyPrefabSizeDefaults()
        {
            if (blockPreferredSize.x <= 0f || blockPreferredSize.y <= 0f)
            {
                blockPreferredSize = defaultBlockPreferredSize;
            }

            if (!enforcePrefabSize)
            {
                return;
            }

            if (TryGetComponent(out RectTransform rectTransform))
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = blockPreferredSize;
            }

            if (TryGetComponent(out LayoutElement layoutElement))
            {
                layoutElement.minWidth = blockPreferredSize.x;
                layoutElement.minHeight = blockPreferredSize.y;
                layoutElement.preferredWidth = blockPreferredSize.x;
                layoutElement.preferredHeight = blockPreferredSize.y;
                layoutElement.flexibleWidth = 0f;
                layoutElement.flexibleHeight = 0f;
            }
        }

        static bool TryRestoreColor(ref Color target, Color fallback)
        {
            if (target.a > 0f)
            {
                return false;
            }

            target = fallback;
            return true;
        }
    }
}
