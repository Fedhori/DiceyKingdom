using System;
using System.Collections;
using Game.Infrastructure.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Presentation.Battle
{
    public sealed class BattleAbilityCardView :
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
        static readonly Color defaultIconFill = Colors.Primitive.Bone300;
        static readonly Color defaultPowerBadgeBackground = Colors.Semantic.SurfaceSecondary;
        static readonly Color defaultPowerText = Colors.Semantic.TextPrimary;
        static readonly Color defaultRollOverlayText = Colors.Semantic.TextPrimary;
        static readonly Color finalRollOverlayText = Colors.Semantic.StatePositive;
        static readonly Color defaultAttackBorder = Colors.Semantic.StateDanger;
        static readonly Color defaultSkillBorder = Colors.Semantic.StateInfo;
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
        [SerializeField] Outline borderOutline;
        [SerializeField] Image disabledOverlayImage;

        Action<string> clickHandler;
        Action<string> tooltipEnterHandler;
        Action tooltipExitHandler;
        Action<BattleAbilityCardView, string, InteractionContext, Vector2, Camera> dragStartHandler;
        Action<BattleAbilityCardView, string, InteractionContext, Vector2, Camera> dragMoveHandler;
        Action<BattleAbilityCardView, string, InteractionContext, Vector2, Camera> dragEndHandler;
        Action<BattleAbilityCardView, string, InteractionContext> rightClickHandler;

        string instanceId = string.Empty;
        string tooltipText = string.Empty;
        bool isHoverable;
        bool isInteractable;
        bool isDragging;
        bool isLeftPointerDown;
        InteractionContext interactionContext = InteractionContext.None;
        Color currentBackgroundColor = defaultCardBackground;
        Coroutine invalidFeedbackRoutine;

        public string InstanceId => instanceId;

        void Awake()
        {
            CacheReferencesIfNeeded();
            HideRollOverlay();
        }

        void OnValidate()
        {
            CacheReferencesIfNeeded();
            if (rollOverlayText != null && !UnityEngine.Application.isPlaying)
            {
                rollOverlayText.gameObject.SetActive(false);
            }
        }

        public void Bind(
            BindData bindData,
            bool isSelected,
            bool isInteractable,
            Action<string> onClick,
            Action<string> onTooltipEnter,
            Action onTooltipExit,
            InteractionContext context,
            Action<BattleAbilityCardView, string, InteractionContext, Vector2, Camera> onDragStart,
            Action<BattleAbilityCardView, string, InteractionContext, Vector2, Camera> onDragMove,
            Action<BattleAbilityCardView, string, InteractionContext, Vector2, Camera> onDragEnd,
            Action<BattleAbilityCardView, string, InteractionContext> onRightClick)
        {
            instanceId = bindData.instanceId;
            tooltipText = bindData.tooltipText;
            this.isInteractable = isInteractable;
            interactionContext = context;
            clickHandler = onClick;
            tooltipEnterHandler = onTooltipEnter;
            tooltipExitHandler = onTooltipExit;
            dragStartHandler = onDragStart;
            dragMoveHandler = onDragMove;
            dragEndHandler = onDragEnd;
            rightClickHandler = onRightClick;
            isHoverable = !string.IsNullOrWhiteSpace(tooltipText);

            currentBackgroundColor = isSelected
                ? defaultCardBackgroundSelected
                : defaultCardBackground;
            ApplyBackgroundColorForCurrentState();

            if (iconImage != null)
            {
                iconImage.color = defaultIconFill;
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

            rollOverlayText.text = value.ToString();
            rollOverlayText.color = isFinal ? finalRollOverlayText : defaultRollOverlayText;
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

            if (iconImage == null)
            {
                iconImage = ResolveImage("Icon");
            }

            if (powerBadgeImage == null)
            {
                powerBadgeImage = ResolveImage("PowerBadge");
            }

            if (powerBadgeText == null)
            {
                powerBadgeText = ResolveText("PowerText");
            }

            if (rollOverlayText == null)
            {
                rollOverlayText = ResolveText("RollOverlayText");
            }

            if (borderOutline == null)
            {
                borderOutline = GetComponent<Outline>();
            }

            if (disabledOverlayImage == null)
            {
                disabledOverlayImage = ResolveImage("DisabledOverlay");
            }
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
                default:
                    return defaultAttackBorder;
            }
        }
    }
}
