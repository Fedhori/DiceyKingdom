using System.Collections.Generic;
using UnityEngine;

// 툴팁 안에 동일한 타입의 툴팁이 뜨는 경우는 되도록 피한다.
// 만약 자식 툴팁이 파괴되는 등의 이유로 HideTooltip을 호출해버리면 부모가 SetActive(false)가 되며 gameObject.IsDestroying()이 호출될 수 있다.
namespace UI.Tooltip
{
    public sealed class TooltipManager : MonoBehaviour
    {
        public static TooltipManager Instance { get; private set; }

        [System.Serializable]
        private struct PrefabMapping
        {
            public TooltipKind kind;
            public TooltipView prefab;
        }

        [SerializeField] private List<PrefabMapping> prefabMappings;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Vector2 screenOffset = new Vector2(12f, -12f);
        [SerializeField] private float clampMargin = 8f;

        private readonly Dictionary<TooltipKind, TooltipView> prefabRegistry = new();
        private readonly Dictionary<TooltipKind, List<TooltipView>> pool = new();
        private readonly Dictionary<TooltipKind, TooltipView> pinnedTooltips = new();

        private readonly Dictionary<TooltipKind, TooltipView> activeTooltips = new();
        private bool isPinning; // 재진입 버그를 막기 위한 플래그

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            foreach (var mapping in prefabMappings)
                if (mapping.prefab != null)
                    prefabRegistry[mapping.kind] = mapping.prefab;
        }
        

        // Main entry point, now accepts a sourceTarget to check against pinned tooltips.
        public void ShowTooltip(Vector2 screenPos, TooltipContent data, TooltipKind kind, TooltipTarget sourceTarget)
        {
            // Check if the source of this tooltip already has a pinned tooltip.
            foreach (var pinnedView in pinnedTooltips.Values)
                if (pinnedView.SourceTarget == sourceTarget)
                    return; // Don't show a new tooltip if this target's tooltip is already pinned.

            ShowInternal(screenPos, data, kind, sourceTarget);
        }

        public void HideTooltip(TooltipKind kind)
        {
            // Guard against re-entrant calls during a pinning operation.
            if (isPinning) return;

            if (activeTooltips.TryGetValue(kind, out var tooltipToHide))
            {
                ReturnToPool(tooltipToHide);
                activeTooltips.Remove(kind);
            }
        }

        public void PinCurrentTooltip(TooltipKind kind)
        {
            if (isPinning) return; // Prevent nested pinning.
            
            if (!activeTooltips.TryGetValue(kind, out var tooltipToPin)) return;

            try
            {
                isPinning = true;

                if (pinnedTooltips.ContainsKey(kind)) UnpinTooltip(pinnedTooltips[kind]);

                pinnedTooltips[kind] = tooltipToPin;
                if (tooltipToPin.closeButton != null) tooltipToPin.closeButton.gameObject.SetActive(true);

                activeTooltips.Remove(kind);
            }
            finally
            {
                isPinning = false; // Ensure the flag is always reset.
            }
        }

        public void UnpinTooltip(TooltipView tooltip)
        {
            if (tooltip == null) return;

            var kind = tooltip.kind;
            if (pinnedTooltips.ContainsKey(kind) && pinnedTooltips[kind] == tooltip)
            {
                pinnedTooltips.Remove(kind);
                ReturnToPool(tooltip);
            }
        }

        private void ShowInternal(Vector2 screenPos, TooltipContent data, TooltipKind kind, TooltipTarget sourceTarget, bool clampPosition = true)
        {
            if (activeTooltips.TryGetValue(kind, out var existingTooltip)) ReturnToPool(existingTooltip);

            var activeTooltip = GetFromPool(kind);
            if (activeTooltip == null)
            {
                Debug.LogError($"[TooltipManager] No prefab registered for kind: {kind}");
                return;
            }
            activeTooltips[kind] = activeTooltip;

            activeTooltip.kind = kind;
            activeTooltip.SourceTarget = sourceTarget; // Set the source target
            activeTooltip.SetData(data);
            if (activeTooltip.closeButton != null) activeTooltip.closeButton.gameObject.SetActive(false);
            activeTooltip.gameObject.SetActive(true);
            activeTooltip.transform.SetAsLastSibling();

            if (clampPosition) ClampToScreen(screenPos, activeTooltip);
        }

        private void ClampToScreen(Vector2 screenPos, TooltipView activeTooltip)
        {
            var tooltipRect = activeTooltip.GetComponent<RectTransform>();
            var canvasRect = (RectTransform)canvas.transform;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, canvas.worldCamera, out var localPoint);
            
            Vector2 pivot = tooltipRect.pivot;
            
            localPoint.x += pivot.x * tooltipRect.rect.width;
            localPoint.y -= (1 - pivot.y) * tooltipRect.rect.height;
            
            localPoint += screenOffset;

            var halfCanvas = canvasRect.rect.size * 0.5f;
            var tooltipSize = tooltipRect.rect.size;
            localPoint.x = Mathf.Clamp(localPoint.x, -halfCanvas.x + clampMargin, halfCanvas.x - tooltipSize.x / 2 - clampMargin);
            localPoint.y = Mathf.Clamp(localPoint.y, -halfCanvas.y + tooltipSize.y / 2 + clampMargin, halfCanvas.y - clampMargin);

            tooltipRect.anchoredPosition = localPoint;
        }

        private TooltipView GetFromPool(TooltipKind kind)
        {
            if (!pool.TryGetValue(kind, out var list))
            {
                list = new List<TooltipView>();
                pool[kind] = list;
            }

            foreach (var view in list)
                if (!view.gameObject.activeSelf)
                    return view;

            if (prefabRegistry.TryGetValue(kind, out var prefab))
            {
                var newInstance = Instantiate(prefab, canvas.transform);
                newInstance.gameObject.name = $"{kind}Tooltip_Pooled_{list.Count}";
                list.Add(newInstance);
                return newInstance;
            }

            return null;
        }

        private void ReturnToPool(TooltipView view)
        {
            view.SourceTarget = null; // Clear the source target reference
            view.gameObject.SetActive(false);
        }
    }
}
