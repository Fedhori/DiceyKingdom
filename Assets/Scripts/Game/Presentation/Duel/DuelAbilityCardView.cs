using System;
using System.Collections;
using System.Collections.Generic;
using Game.Infrastructure.Data;
using Game.UI.Tooltip;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Presentation.Duel
{
    public class DuelAbilityCardView :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        public enum InteractionArea
        {
            None = 0,
            Loadout = 1,
            Combat = 2
        }

        [Serializable]
        public readonly struct InteractionContext
        {
            public static InteractionContext None => new(InteractionArea.None, -1);
            public static InteractionContext Loadout => new(InteractionArea.Loadout, -1);
            public static InteractionContext Combat(int combatIndex)
            {
                return new InteractionContext(InteractionArea.Combat, combatIndex);
            }

            public InteractionArea area { get; }
            public int combatIndex { get; }
            public bool isCombat => area == InteractionArea.Combat;

            public InteractionContext(InteractionArea area, int combatIndex)
            {
                this.area = area;
                this.combatIndex = combatIndex;
            }
        }

        [Serializable]
        public readonly struct BindData
        {
            public string instanceId { get; }
            public string tooltipTitle { get; }
            public string tooltipBody { get; }
            public AbilityType abilityType { get; }
            public Sprite iconSprite { get; }
            public int power { get; }
            public bool showPower { get; }
            public int cooldownTurns { get; }
            public int cooldownRemaining { get; }

            public BindData(
                string instanceId,
                string tooltipTitle,
                string tooltipBody,
                AbilityType abilityType,
                Sprite iconSprite,
                int power,
                bool showPower,
                int cooldownTurns,
                int cooldownRemaining)
            {
                this.instanceId = instanceId ?? string.Empty;
                this.tooltipTitle = tooltipTitle ?? string.Empty;
                this.tooltipBody = tooltipBody ?? string.Empty;
                this.abilityType = abilityType;
                this.iconSprite = iconSprite;
                this.power = power;
                this.showPower = showPower;
                this.cooldownTurns = Mathf.Max(0, cooldownTurns);
                this.cooldownRemaining = Mathf.Max(0, cooldownRemaining);
            }
        }

        static readonly Color defaultCardBackground = Colors.Semantic.SurfaceParchment;
        static readonly Color defaultCardBackgroundSelected = Colors.Primitive.Bone200;
        static readonly Color defaultIconTint = Colors.Primitive.Bone050;
        static readonly Color defaultPowerBadgeBackground = Colors.Semantic.SurfaceSecondary;
        static readonly Color defaultPowerText = Colors.Semantic.TextPrimary;
        static readonly Color defaultAttackBorder = Colors.Semantic.StateDanger;
        static readonly Color defaultSkillBorder = Colors.Semantic.StateInfo;
        static readonly Color defaultPassiveBorder = Colors.Semantic.StateWarning;
        static readonly Color defaultDisabledOverlay = Colors.Semantic.DisabledTint;
        const float dragVisualAlpha = 0.5f;
        const float invalidFeedbackDuration = 0.12f;
        const float invalidFeedbackScaleUp = 1.05f;

        [Header("References")]
        [SerializeField] Button clickButton;
        [SerializeField] Image cardBackgroundImage;
        [SerializeField] Image iconImage;
        [SerializeField] Image powerBadgeImage;
        [SerializeField] TMP_Text powerBadgeText;
        [SerializeField] TMP_Text rollOverlayText;
        [SerializeField] TMP_Text cooldownCornerText;
        [SerializeField] Image cooldownOverlayImage;
        [SerializeField] TMP_Text cooldownOverlayText;
        [SerializeField] Outline borderOutline;
        [SerializeField] Image disabledOverlayImage;
        [SerializeField] AbilityCardTooltipProvider tooltipProvider;
        [SerializeField] TooltipTarget tooltipTarget;

        Action<string> clickHandler;
        Action<DuelAbilityCardView, string, InteractionContext, Vector2, Camera> dragStartHandler;
        Action<DuelAbilityCardView, string, InteractionContext, Vector2, Camera> dragMoveHandler;
        Action<DuelAbilityCardView, string, InteractionContext, Vector2, Camera> dragEndHandler;
        Action<DuelAbilityCardView, string, InteractionContext> rightClickHandler;

        string instanceId = string.Empty;
        bool isInteractable;
        bool isDragging;
        bool isLeftPointerDown;
        InteractionContext interactionContext = InteractionContext.None;
        Color currentBackgroundColor = defaultCardBackground;
        Coroutine invalidFeedbackRoutine;

        public string InstanceId => instanceId;

        void Awake()
        {
            if (!ValidateRequiredReferences("Awake"))
            {
                enabled = false;
                return;
            }

            HideRollOverlay();
        }

        void OnValidate()
        {
            ValidateRequiredReferences("OnValidate");
            if (rollOverlayText != null && !UnityEngine.Application.isPlaying)
            {
                rollOverlayText.gameObject.SetActive(false);
            }

            if (!UnityEngine.Application.isPlaying)
            {
                SetCooldownOverlayVisible(false, 0);
                SetCooldownCornerValue(0);
            }
        }

        public void Bind(
            BindData bindData,
            bool isSelected,
            bool isInteractable,
            Action<string> onClick,
            InteractionContext context,
            Action<DuelAbilityCardView, string, InteractionContext, Vector2, Camera> onDragStart,
            Action<DuelAbilityCardView, string, InteractionContext, Vector2, Camera> onDragMove,
            Action<DuelAbilityCardView, string, InteractionContext, Vector2, Camera> onDragEnd,
            Action<DuelAbilityCardView, string, InteractionContext> onRightClick)
        {
            instanceId = bindData.instanceId;
            this.isInteractable = isInteractable;
            interactionContext = context;
            clickHandler = onClick;
            dragStartHandler = onDragStart;
            dragMoveHandler = onDragMove;
            dragEndHandler = onDragEnd;
            rightClickHandler = onRightClick;
            if (tooltipProvider != null)
            {
                tooltipProvider.SetContent(bindData.tooltipTitle, bindData.tooltipBody);
            }

            currentBackgroundColor = isSelected
                ? defaultCardBackgroundSelected
                : defaultCardBackground;
            ApplyBackgroundColorForCurrentState();

            if (iconImage != null)
            {
                iconImage.sprite = bindData.iconSprite;
                iconImage.color = defaultIconTint;
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

            SetCooldownCornerValue(bindData.cooldownTurns);
            SetCooldownOverlayVisible(bindData.cooldownRemaining > 0, bindData.cooldownRemaining);

            if (clickButton != null)
            {
                clickButton.onClick.RemoveListener(HandleClick);
                clickButton.onClick.AddListener(HandleClick);
                clickButton.interactable = isInteractable && !isDragging;
            }

            if (disabledOverlayImage != null)
            {
                disabledOverlayImage.gameObject.SetActive(!isInteractable);
                disabledOverlayImage.color = defaultDisabledOverlay;
            }

            HideRollOverlay();
        }

        public void PlayInvalidDropFeedback()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            if (invalidFeedbackRoutine != null)
            {
                StopCoroutine(invalidFeedbackRoutine);
            }

            invalidFeedbackRoutine = StartCoroutine(PlayInvalidDropFeedbackRoutine());
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
        }

        public void OnPointerExit(PointerEventData eventData)
        {
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                if (!isInteractable ||
                    !interactionContext.isCombat ||
                    string.IsNullOrWhiteSpace(instanceId))
                {
                    return;
                }

                rightClickHandler?.Invoke(this, instanceId, interactionContext);
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                isLeftPointerDown = true;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanStartDrag(eventData))
            {
                return;
            }

            isDragging = true;
            ApplyBackgroundColorForCurrentState();

            if (clickButton != null)
            {
                clickButton.interactable = false;
            }

            dragStartHandler?.Invoke(this, instanceId, interactionContext, eventData.position, eventData.pressEventCamera);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData != null && eventData.button == PointerEventData.InputButton.Left)
            {
                isLeftPointerDown = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || eventData == null)
            {
                return;
            }

            dragMoveHandler?.Invoke(this, instanceId, interactionContext, eventData.position, eventData.pressEventCamera);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            bool shouldDispatch = isDragging && eventData != null;

            isDragging = false;
            isLeftPointerDown = false;
            ApplyBackgroundColorForCurrentState();

            if (clickButton != null)
            {
                clickButton.interactable = isInteractable;
            }

            if (!shouldDispatch)
            {
                return;
            }

            dragEndHandler?.Invoke(this, instanceId, interactionContext, eventData.position, eventData.pressEventCamera);
        }

        void OnDestroy()
        {
            if (invalidFeedbackRoutine != null)
            {
                StopCoroutine(invalidFeedbackRoutine);
                invalidFeedbackRoutine = null;
            }

            if (clickButton != null)
            {
                clickButton.onClick.RemoveListener(HandleClick);
            }
        }

        public void SetRollOverlayValue(int value, bool isFinal)
        {
            if (rollOverlayText == null)
            {
                return;
            }

            _ = isFinal;
            rollOverlayText.text = value.ToString();
            rollOverlayText.gameObject.SetActive(true);
        }

        public void HideRollOverlay()
        {
            if (rollOverlayText == null)
            {
                return;
            }

            rollOverlayText.text = string.Empty;
            rollOverlayText.gameObject.SetActive(false);
        }

        public void SetPowerBadgeValue(int powerValue)
        {
            if (powerBadgeText == null || powerBadgeImage == null || !powerBadgeImage.gameObject.activeSelf)
            {
                return;
            }

            powerBadgeText.text = Mathf.Max(0, powerValue).ToString();
        }

        void HandleClick()
        {
            if (isDragging)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return;
            }

            clickHandler?.Invoke(instanceId);
        }

        bool CanStartDrag(PointerEventData eventData)
        {
            if (eventData == null ||
                eventData.button != PointerEventData.InputButton.Left ||
                !isLeftPointerDown ||
                isDragging)
            {
                return false;
            }

            if (!isInteractable || string.IsNullOrWhiteSpace(instanceId))
            {
                return false;
            }

            if (interactionContext.area != InteractionArea.Loadout &&
                interactionContext.area != InteractionArea.Combat)
            {
                return false;
            }

            return true;
        }

        bool ValidateRequiredReferences(string stage)
        {
            var missing = new List<string>();
            if (clickButton == null)
            {
                missing.Add(nameof(clickButton));
            }

            if (cardBackgroundImage == null)
            {
                missing.Add(nameof(cardBackgroundImage));
            }

            if (iconImage == null)
            {
                missing.Add(nameof(iconImage));
            }

            if (powerBadgeImage == null)
            {
                missing.Add(nameof(powerBadgeImage));
            }

            if (powerBadgeText == null)
            {
                missing.Add(nameof(powerBadgeText));
            }

            if (rollOverlayText == null)
            {
                missing.Add(nameof(rollOverlayText));
            }

            if (borderOutline == null)
            {
                missing.Add(nameof(borderOutline));
            }

            if (disabledOverlayImage == null)
            {
                missing.Add(nameof(disabledOverlayImage));
            }

            if (tooltipProvider == null)
            {
                missing.Add(nameof(tooltipProvider));
            }

            if (tooltipTarget == null)
            {
                missing.Add(nameof(tooltipTarget));
            }

            if (missing.Count == 0)
            {
                return true;
            }

            Debug.LogError(
                $"[DuelAbilityCardView] Missing serialized references at {stage} on '{name}': {string.Join(", ", missing)}",
                this);
            return false;
        }

        void ApplyBackgroundColorForCurrentState()
        {
            if (cardBackgroundImage == null)
            {
                return;
            }

            Color applied = currentBackgroundColor;
            if (isDragging)
            {
                applied.a = dragVisualAlpha;
            }

            cardBackgroundImage.color = applied;
        }

        void SetCooldownCornerValue(int cooldownTurns)
        {
            if (cooldownCornerText == null)
            {
                return;
            }

            bool shouldShow = cooldownTurns > 0;
            cooldownCornerText.gameObject.SetActive(shouldShow);
            cooldownCornerText.text = shouldShow ? cooldownTurns.ToString() : string.Empty;
        }

        void SetCooldownOverlayVisible(bool isVisible, int remainingTurns)
        {
            if (cooldownOverlayImage != null)
            {
                cooldownOverlayImage.gameObject.SetActive(isVisible);
            }

            if (cooldownOverlayText != null)
            {
                cooldownOverlayText.gameObject.SetActive(isVisible);
                cooldownOverlayText.text = isVisible ? Mathf.Max(0, remainingTurns).ToString() : string.Empty;
            }
        }

        IEnumerator PlayInvalidDropFeedbackRoutine()
        {
            Transform root = transform;
            Vector3 baseScale = root.localScale;
            Vector3 expandedScale = baseScale * invalidFeedbackScaleUp;

            float halfDuration = invalidFeedbackDuration * 0.5f;
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                float t = halfDuration <= 0f ? 1f : elapsed / halfDuration;
                root.localScale = Vector3.Lerp(baseScale, expandedScale, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                float t = halfDuration <= 0f ? 1f : elapsed / halfDuration;
                root.localScale = Vector3.Lerp(expandedScale, baseScale, t);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            root.localScale = baseScale;
            invalidFeedbackRoutine = null;
        }

        static Color ResolveBorderColor(AbilityType abilityType)
        {
            switch (abilityType)
            {
                case AbilityType.Attack:
                    return defaultAttackBorder;
                case AbilityType.Skill:
                    return defaultSkillBorder;
                case AbilityType.Passive:
                    return defaultPassiveBorder;
                default:
                    return defaultAttackBorder;
            }
        }
    }
}


