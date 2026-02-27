using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.UI.Tooltip
{
    public sealed class TooltipTarget : MonoBehaviour,
        ITooltipAnchorSource,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler
    {
        [SerializeField] TooltipService tooltipService;
        [SerializeField] MonoBehaviour contentProvider;
        [SerializeField] RectTransform anchorRect;
        [SerializeField] TooltipAnchorType anchorType = TooltipAnchorType.Screen;

        [Header("Optional Overrides")]
        [SerializeField] bool useShowDelayOverride;
        [SerializeField] float showDelayOverride = 0.2f;
        [SerializeField] bool useScreenOffsetOverride;
        [SerializeField] Vector2 screenOffsetOverride = new(16f, 0f);
        [SerializeField] bool useEdgePaddingOverride;
        [SerializeField] float edgePaddingOverride = 8f;

        readonly Vector3[] corners = new Vector3[4];
        ITooltipContentProvider provider;
        bool didLogInvalidSetup;

        void Awake()
        {
            provider = contentProvider as ITooltipContentProvider;
        }

        void OnValidate()
        {
            provider = contentProvider as ITooltipContentProvider;
            didLogInvalidSetup = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!TryResolveDependencies())
            {
                return;
            }

            if (!provider.TryBuildTooltipModel(out TooltipModel model))
            {
                return;
            }

            TooltipPresentationOptions options = new(
                useShowDelayOverride,
                showDelayOverride,
                useScreenOffsetOverride,
                screenOffsetOverride,
                useEdgePaddingOverride,
                edgePaddingOverride);

            tooltipService.BeginHover(this, model, this, options);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            EndHoverIfPossible();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            EndHoverIfPossible();
        }

        void OnDisable()
        {
            EndHoverIfPossible();
        }

        void OnDestroy()
        {
            EndHoverIfPossible();
        }

        bool TryResolveDependencies()
        {
            if (tooltipService == null)
            {
                tooltipService = TooltipService.Active;
            }

            if (tooltipService != null && provider != null)
            {
                return true;
            }

            if (didLogInvalidSetup)
            {
                return false;
            }

            didLogInvalidSetup = true;
            Debug.LogError(
                $"[TooltipTarget] Missing reference(s) on '{name}'. " +
                $"tooltipServiceAssigned={(tooltipService != null)}, providerAssigned={(provider != null)}",
                this);
            return false;
        }

        void EndHoverIfPossible()
        {
            if (tooltipService == null)
            {
                return;
            }

            tooltipService.EndHover(this);
        }

        public bool TryBuildAnchor(out TooltipAnchor anchor)
        {
            if (anchorType == TooltipAnchorType.World)
            {
                anchor = TooltipAnchor.FromWorld(transform.position);
                return true;
            }

            RectTransform rect = anchorRect != null ? anchorRect : transform as RectTransform;
            if (rect == null)
            {
                anchor = default;
                return false;
            }

            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

            rect.GetWorldCorners(corners);
            Vector3 topLeftWorld = corners[1];
            Vector3 topRightWorld = corners[2];

            Vector2 screenRightTop = RectTransformUtility.WorldToScreenPoint(eventCamera, topRightWorld);
            Vector2 screenLeftTop = RectTransformUtility.WorldToScreenPoint(eventCamera, topLeftWorld);
            anchor = TooltipAnchor.FromScreen(screenRightTop, screenLeftTop);
            return true;
        }
    }
}
