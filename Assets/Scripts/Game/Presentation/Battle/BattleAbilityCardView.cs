using System;
using Game.Infrastructure.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Presentation.Battle
{
    public sealed class BattleAbilityCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Serializable]
        public readonly struct BindData
        {
            public string instanceId { get; }
            public string title { get; }
            public string tooltipText { get; }
            public AbilityType abilityType { get; }
            public int power { get; }
            public bool showPower { get; }

            public BindData(
                string instanceId,
                string title,
                string tooltipText,
                AbilityType abilityType,
                int power,
                bool showPower)
            {
                this.instanceId = instanceId ?? string.Empty;
                this.title = title ?? string.Empty;
                this.tooltipText = tooltipText ?? string.Empty;
                this.abilityType = abilityType;
                this.power = power;
                this.showPower = showPower;
            }
        }

        static readonly Color defaultCardBackground = Colors.Semantic.SurfaceParchment;
        static readonly Color defaultCardBackgroundSelected = Colors.Primitive.Bone200;
        static readonly Color defaultIconBackground = Colors.Semantic.SurfaceParchmentMuted;
        static readonly Color defaultPowerBadgeBackground = Colors.Semantic.SurfaceSecondary;
        static readonly Color defaultPowerText = Colors.Semantic.TextPrimary;
        static readonly Color defaultTitleText = Colors.Semantic.TextOnLightPrimary;
        static readonly Color defaultAttackBorder = Colors.Semantic.StateDanger;
        static readonly Color defaultSkillBorder = Colors.Semantic.StateInfo;
        static readonly Color defaultDisabledOverlay = Colors.Semantic.DisabledTint;

        [Header("References")]
        [SerializeField] Button clickButton;
        [SerializeField] Image cardBackgroundImage;
        [SerializeField] Image iconBackgroundImage;
        [SerializeField] Image iconImage;
        [SerializeField] Image powerBadgeImage;
        [SerializeField] TMP_Text powerBadgeText;
        [SerializeField] TMP_Text titleText;
        [SerializeField] Outline borderOutline;
        [SerializeField] Image disabledOverlayImage;

        Action<string> clickHandler;
        Action<string> tooltipEnterHandler;
        Action tooltipExitHandler;

        string instanceId = string.Empty;
        string tooltipText = string.Empty;
        bool isHoverable;

        public void Bind(
            BindData bindData,
            bool isSelected,
            bool isInteractable,
            Action<string> onClick,
            Action<string> onTooltipEnter,
            Action onTooltipExit)
        {
            CacheReferencesIfNeeded();

            instanceId = bindData.instanceId;
            tooltipText = bindData.tooltipText;
            clickHandler = onClick;
            tooltipEnterHandler = onTooltipEnter;
            tooltipExitHandler = onTooltipExit;
            isHoverable = !string.IsNullOrWhiteSpace(tooltipText);

            if (titleText != null)
            {
                titleText.text = bindData.title;
                titleText.color = defaultTitleText;
            }

            if (cardBackgroundImage != null)
            {
                cardBackgroundImage.color = isSelected
                    ? defaultCardBackgroundSelected
                    : defaultCardBackground;
            }

            if (iconBackgroundImage != null)
            {
                iconBackgroundImage.color = defaultIconBackground;
            }

            if (iconImage != null)
            {
                iconImage.color = defaultIconBackground;
            }

            Color borderColor = ResolveBorderColor(bindData.abilityType);
            if (borderOutline != null)
            {
                borderOutline.effectColor = borderColor;
            }

            if (powerBadgeImage != null)
            {
                powerBadgeImage.gameObject.SetActive(bindData.showPower);
                powerBadgeImage.color = defaultPowerBadgeBackground;
            }

            if (powerBadgeText != null)
            {
                powerBadgeText.gameObject.SetActive(bindData.showPower);
                powerBadgeText.text = bindData.showPower ? bindData.power.ToString() : string.Empty;
                powerBadgeText.color = defaultPowerText;
            }

            if (clickButton != null)
            {
                clickButton.onClick.RemoveListener(HandleClick);
                clickButton.onClick.AddListener(HandleClick);
                clickButton.interactable = isInteractable;
            }

            if (disabledOverlayImage != null)
            {
                disabledOverlayImage.gameObject.SetActive(!isInteractable);
                disabledOverlayImage.color = defaultDisabledOverlay;
            }
        }

        public void SetRollPulse(float normalized)
        {
            if (cardBackgroundImage == null)
            {
                return;
            }

            float alpha = Mathf.Lerp(0.85f, 1.0f, normalized);
            Color color = cardBackgroundImage.color;
            color.a = alpha;
            cardBackgroundImage.color = color;
        }

        public void RestoreVisual()
        {
            if (cardBackgroundImage == null)
            {
                return;
            }

            Color color = cardBackgroundImage.color;
            color.a = 1.0f;
            cardBackgroundImage.color = color;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isHoverable)
            {
                return;
            }

            tooltipEnterHandler?.Invoke(tooltipText);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltipExitHandler?.Invoke();
        }

        void OnDestroy()
        {
            if (clickButton != null)
            {
                clickButton.onClick.RemoveListener(HandleClick);
            }
        }

        void OnValidate()
        {
            CacheReferencesIfNeeded();
        }

        void CacheReferencesIfNeeded()
        {
            if (clickButton == null)
            {
                clickButton = GetComponent<Button>();
            }

            if (cardBackgroundImage == null)
            {
                cardBackgroundImage = GetComponent<Image>();
            }

            if (borderOutline == null)
            {
                borderOutline = GetComponent<Outline>();
            }

            if (iconBackgroundImage == null)
            {
                iconBackgroundImage = ResolveImage("IconBackground");
            }

            if (iconImage == null)
            {
                iconImage = ResolveImage("IconImage");
            }

            if (powerBadgeImage == null)
            {
                powerBadgeImage = ResolveImage("PowerBadge");
            }

            if (powerBadgeText == null)
            {
                powerBadgeText = ResolveText("PowerText");
            }

            if (titleText == null)
            {
                titleText = ResolveText("TitleText");
            }

            if (disabledOverlayImage == null)
            {
                disabledOverlayImage = ResolveImage("DisabledOverlay");
            }
        }

        void HandleClick()
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return;
            }

            clickHandler?.Invoke(instanceId);
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

        static Color ResolveBorderColor(AbilityType abilityType)
        {
            switch (abilityType)
            {
                case AbilityType.Attack:
                    return defaultAttackBorder;
                case AbilityType.Skill:
                    return defaultSkillBorder;
                default:
                    return defaultAttackBorder;
            }
        }
    }
}
