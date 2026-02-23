using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Presentation.Debug
{
    public sealed class DuelActionBlockView : MonoBehaviour
    {
        static readonly Color defaultPlayerBackgroundColor = new Color32(68, 129, 192, 220);
        static readonly Color defaultOpponentBackgroundColor = new Color32(160, 82, 82, 220);
        static readonly Color defaultPlayerSelectedColor = new Color32(35, 92, 168, 255);
        static readonly Color defaultOpponentSelectedColor = new Color32(176, 54, 54, 255);
        static readonly Color defaultPlayerAttackColor = new Color32(255, 233, 165, 255);
        static readonly Color defaultOpponentAttackColor = new Color32(255, 201, 201, 255);

        [SerializeField] Image backgroundImage;
        [SerializeField] Button selectButton;
        [FormerlySerializedAs("troopDefIdText")]
        [SerializeField] TMP_Text actionDefIdText;
        [SerializeField] TMP_Text effectsText;
        [SerializeField] TMP_Text attackResultText;
        [SerializeField] TMP_Text attackText;
        [SerializeField] Color playerBackgroundColor = defaultPlayerBackgroundColor;
        [FormerlySerializedAs("enemyBackgroundColor")]
        [SerializeField] Color opponentBackgroundColor = defaultOpponentBackgroundColor;
        [SerializeField] Color playerSelectedColor = defaultPlayerSelectedColor;
        [FormerlySerializedAs("enemySelectedColor")]
        [SerializeField] Color opponentSelectedColor = defaultOpponentSelectedColor;
        [SerializeField] Color playerAttackColor = defaultPlayerAttackColor;
        [FormerlySerializedAs("enemyAttackColor")]
        [SerializeField] Color opponentAttackColor = defaultOpponentAttackColor;

        Action onSelected;

        void Awake()
        {
            if (ApplyDefaultColorsIfInvalid())
            {
                UnityEngine.Debug.LogWarning("[DuelActionBlockView] Color fields were invalid and reset to defaults.");
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
            ApplyDefaultColorsIfInvalid();

            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            if (selectButton == null)
            {
                selectButton = GetComponent<Button>();
            }

            actionDefIdText = ClashResolveChildText(actionDefIdText, "ActionDefIdText", "TroopDefIdText");
            effectsText = ClashResolveChildText(effectsText, "EffectsText");
            attackResultText = ClashResolveChildText(attackResultText, "AttackResultText");
            attackText = ClashResolveChildText(attackText, "AttackText");
        }

        public void Bind(
            string actionDefId,
            int attack,
            int attackResult,
            string effectsLabel,
            bool isPlayerSide,
            bool isSelected,
            bool isSelectable,
            Action onSelected)
        {
            this.onSelected = onSelected;

            SetText(actionDefIdText, actionDefId);
            SetText(attackText, attack.ToString());
            SetText(attackResultText, $"Result {attackResult}");
            SetText(effectsText, $"Effects: {effectsLabel}");

            if (attackText != null)
            {
                attackText.fontStyle = FontStyles.Bold;
            }

            Color baseColor = isPlayerSide
                ? playerBackgroundColor
                : opponentBackgroundColor;
            Color selectedColor = isPlayerSide
                ? playerSelectedColor
                : opponentSelectedColor;

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
                : opponentAttackColor;
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

        TMP_Text ClashResolveChildText(TMP_Text currentValue, params string[] childNames)
        {
            if (currentValue != null)
            {
                return currentValue;
            }

            if (childNames == null || childNames.Length <= 0)
            {
                return null;
            }

            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                bool isMatch = false;
                for (int nameIndex = 0; nameIndex < childNames.Length; nameIndex++)
                {
                    if (!string.Equals(child.name, childNames[nameIndex], StringComparison.Ordinal))
                    {
                        continue;
                    }

                    isMatch = true;
                    break;
                }

                if (!isMatch)
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
            changed |= TryRestoreColor(ref opponentBackgroundColor, defaultOpponentBackgroundColor);
            changed |= TryRestoreColor(ref playerSelectedColor, defaultPlayerSelectedColor);
            changed |= TryRestoreColor(ref opponentSelectedColor, defaultOpponentSelectedColor);
            changed |= TryRestoreColor(ref playerAttackColor, defaultPlayerAttackColor);
            changed |= TryRestoreColor(ref opponentAttackColor, defaultOpponentAttackColor);

            return changed;
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
