using System.Collections;
using Game.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI.Tooltip
{
    public sealed class TooltipService : MonoBehaviour
    {
        public static TooltipService Active { get; private set; }

        [SerializeField] Canvas tooltipCanvas;
        [SerializeField] TooltipView tooltipView;
        [SerializeField] Camera worldCamera;
        [SerializeField] Vector2 defaultScreenOffset = new(16f, 0f);
        [SerializeField] float defaultEdgePadding = 8f;
        [SerializeField] float defaultShowDelaySeconds = 0.2f;

        object currentOwner;
        TooltipModel currentModel;
        ITooltipAnchorSource currentAnchorSource;
        TooltipPresentationOptions currentOptions;
        bool hasCurrentModel;
        bool isVisible;
        Coroutine showRoutine;

        readonly DisposableBag subscriptions = new();

        RectTransform CanvasRect
        {
            get
            {
                if (tooltipCanvas == null)
                {
                    return null;
                }

                return tooltipCanvas.transform as RectTransform;
            }
        }

        void Awake()
        {
            if (tooltipCanvas == null)
            {
                tooltipCanvas = GetComponentInParent<Canvas>();
            }

            UpdateWorldCamera();
        }

        void OnEnable()
        {
            Active = this;
            subscriptions.Clear();
            subscriptions.Add(EventSubscription.Create(
                () => SceneManager.activeSceneChanged += OnActiveSceneChanged,
                () => SceneManager.activeSceneChanged -= OnActiveSceneChanged));
            UpdateWorldCamera();
            ResetStateAndHide();
        }

        void Update()
        {
            if (!isVisible || !hasCurrentModel || currentOwner == null || currentAnchorSource == null)
            {
                return;
            }

            RepositionNow();
        }

        void OnDisable()
        {
            if (ReferenceEquals(Active, this))
            {
                Active = null;
            }

            subscriptions.Clear();
            StopShowRoutineIfRunning();
            ResetStateAndHide();
        }

        void OnDestroy()
        {
            if (ReferenceEquals(Active, this))
            {
                Active = null;
            }

            subscriptions.Clear();
        }

        public void BeginHover(
            object owner,
            TooltipModel model,
            ITooltipAnchorSource anchorSource,
            TooltipPresentationOptions options)
        {
            if (owner == null || anchorSource == null)
            {
                return;
            }

            currentOwner = owner;
            currentModel = model;
            currentAnchorSource = anchorSource;
            currentOptions = options;
            hasCurrentModel = true;
            isVisible = false;

            StopShowRoutineIfRunning();
            showRoutine = StartCoroutine(ShowDelayed());
        }

        public void EndHover(object owner)
        {
            if (owner == null || !ReferenceEquals(owner, currentOwner))
            {
                return;
            }

            StopShowRoutineIfRunning();
            ResetStateAndHide();
        }

        public bool ValidateConfiguration(Transform appRoot)
        {
            bool valid = true;
            if (tooltipCanvas == null)
            {
                Debug.LogError("[TooltipService] tooltipCanvas is not assigned.");
                valid = false;
            }

            if (tooltipView == null)
            {
                Debug.LogError("[TooltipService] tooltipView is not assigned.");
                valid = false;
            }

            if (appRoot != null)
            {
                if (tooltipCanvas != null && !tooltipCanvas.transform.IsChildOf(appRoot))
                {
                    Debug.LogError("[TooltipService] tooltipCanvas must be placed under GameApp in editor.");
                    valid = false;
                }

                if (tooltipView != null && !tooltipView.transform.IsChildOf(appRoot))
                {
                    Debug.LogError("[TooltipService] tooltipView must be placed under GameApp in editor.");
                    valid = false;
                }
            }

            return valid;
        }

        IEnumerator ShowDelayed()
        {
            float delay = ResolveShowDelay(currentOptions);
            if (delay > 0f)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            showRoutine = null;
            if (!hasCurrentModel || currentOwner == null || currentAnchorSource == null)
            {
                yield break;
            }

            ShowNow();
        }

        void ShowNow()
        {
            if (tooltipCanvas == null || tooltipView == null)
            {
                return;
            }

            RectTransform canvasRect = CanvasRect;
            if (canvasRect == null || !hasCurrentModel || currentAnchorSource == null)
            {
                return;
            }

            tooltipView.Show(currentModel);
            RectTransform tooltipRect = tooltipView.rectTransform;
            if (tooltipRect == null)
            {
                return;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
            isVisible = true;
            RepositionNow();
        }

        void RepositionNow()
        {
            if (tooltipCanvas == null || tooltipView == null || currentAnchorSource == null)
            {
                return;
            }

            if (!currentAnchorSource.TryBuildAnchor(out TooltipAnchor anchor))
            {
                return;
            }

            RectTransform canvasRect = CanvasRect;
            RectTransform tooltipRect = tooltipView.rectTransform;
            if (canvasRect == null || tooltipRect == null)
            {
                return;
            }

            if (!TryResolveScreenAnchor(anchor, out TooltipAnchor screenAnchor))
            {
                return;
            }

            float scaleFactor = tooltipCanvas.scaleFactor <= 0f ? 1f : tooltipCanvas.scaleFactor;
            Vector2 tooltipSize = new(
                tooltipRect.rect.width * scaleFactor,
                tooltipRect.rect.height * scaleFactor);
            Vector2 screenSize = new(Screen.width, Screen.height);
            Vector2 screenOffset = ResolveScreenOffset(currentOptions);
            float edgePadding = ResolveEdgePadding(currentOptions);

            Vector2 screenTopLeft = TooltipPlacementCalculator.ComputeScreenTopLeft(
                screenAnchor,
                tooltipSize,
                screenSize,
                screenOffset,
                edgePadding);

            Camera canvasCamera = ResolveCanvasCamera(tooltipCanvas);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenTopLeft,
                    canvasCamera,
                    out Vector2 localPos))
            {
                return;
            }

            tooltipRect.anchoredPosition = localPos;
        }

        bool TryResolveScreenAnchor(TooltipAnchor anchor, out TooltipAnchor screenAnchor)
        {
            if (anchor.Type == TooltipAnchorType.Screen)
            {
                screenAnchor = anchor;
                return true;
            }

            UpdateWorldCamera();
            Camera sourceCamera = worldCamera;
            if (sourceCamera == null)
            {
                Debug.LogWarning("[TooltipService] World camera is null. Cannot place world tooltip.");
                screenAnchor = default;
                return false;
            }

            Vector2 point = sourceCamera.WorldToScreenPoint(anchor.WorldPosition);
            screenAnchor = TooltipAnchor.FromScreen(point, point);
            return true;
        }

        void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            UpdateWorldCamera();
            StopShowRoutineIfRunning();
            ResetStateAndHide();
        }

        void StopShowRoutineIfRunning()
        {
            if (showRoutine == null)
            {
                return;
            }

            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        void ResetStateAndHide()
        {
            currentOwner = null;
            currentAnchorSource = null;
            hasCurrentModel = false;
            isVisible = false;
            if (tooltipView != null)
            {
                tooltipView.Hide();
            }
        }

        void UpdateWorldCamera()
        {
            if (worldCamera != null)
            {
                return;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                return;
            }

            worldCamera = mainCamera;
        }

        float ResolveShowDelay(TooltipPresentationOptions options)
        {
            if (options.useShowDelayOverride)
            {
                return Mathf.Max(0f, options.showDelayOverride);
            }

            return Mathf.Max(0f, defaultShowDelaySeconds);
        }

        Vector2 ResolveScreenOffset(TooltipPresentationOptions options)
        {
            if (options.useScreenOffsetOverride)
            {
                return options.screenOffsetOverride;
            }

            return defaultScreenOffset;
        }

        float ResolveEdgePadding(TooltipPresentationOptions options)
        {
            if (options.useEdgePaddingOverride)
            {
                return Mathf.Max(0f, options.edgePaddingOverride);
            }

            return Mathf.Max(0f, defaultEdgePadding);
        }

        static Camera ResolveCanvasCamera(Canvas canvas)
        {
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera;
        }
    }
}
