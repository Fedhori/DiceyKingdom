using System.Collections;
using Game.App;
using Game.Common;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;




namespace Game.UI.Tooltip
{
public sealed class TooltipService : MonoBehaviour
{
    [SerializeField] Canvas tooltipCanvas;      
    [SerializeField] TooltipView tooltipView;
    [SerializeField] Camera worldCamera;
    [SerializeField] Vector2 screenOffset = new Vector2(16f, -16f);

    
    [SerializeField] float edgePadding = 8f;
    [SerializeField] float showDelaySeconds = 0.2f;

    object currentOwner;
    TooltipModel currentModel;
    TooltipAnchor currentAnchor;
    bool hasCurrentModel;
    bool isPinned;
    bool dragHidden;

    Coroutine showRoutine;
    readonly DisposableBag subscriptions = new();

    RectTransform CanvasRect
    {
        get
        {
            if (tooltipCanvas == null)
                return null;
            return tooltipCanvas.transform as RectTransform;
        }
    }

    void Awake()
    {
        if (tooltipCanvas == null)
            tooltipCanvas = GetComponentInParent<Canvas>();

        UpdateWorldCamera();
    }

    void OnEnable()
    {
        subscriptions.Clear();
        subscriptions.Add(EventSubscription.Create(
            () => SceneManager.activeSceneChanged += OnActiveSceneChanged,
            () => SceneManager.activeSceneChanged -= OnActiveSceneChanged));
        UpdateWorldCamera();
    }

    void OnDisable()
    {
        subscriptions.Clear();

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }
        HideImmediate();
    }

    void OnDestroy()
    {
        subscriptions.Clear();
    }

    void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        UpdateWorldCamera();

        currentOwner = null;
        hasCurrentModel = false;
        isPinned = false;
        dragHidden = false;

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        HideImmediate();
    }

    void UpdateWorldCamera()
    {
        if (worldCamera != null)
            return;

        var mainCam = Camera.main;
        if (mainCam != null)
        {
            worldCamera = mainCam;
        }
        else
        {
            Debug.LogWarning("[TooltipService] No world camera found. Tooltips will not be positioned.");
        }
    }

    public void BeginHover(object owner, TooltipModel model, TooltipAnchor anchor)
    {
        if (owner == null)
            return;

        if (isPinned)
            return;

        currentOwner = owner;
        currentModel = model;
        currentAnchor = anchor;
        hasCurrentModel = true;

        if (showRoutine != null)
            StopCoroutine(showRoutine);

        showRoutine = StartCoroutine(ShowDelayed());
    }

    public void EndHover(object owner)
    {
        if (owner == null)
            return;

        if (isPinned)
            return;

        if (!ReferenceEquals(owner, currentOwner))
            return;

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        currentOwner = null;
        hasCurrentModel = false;

        HideImmediate();
    }

    public void TogglePin(object owner, TooltipModel model, TooltipAnchor anchor)
    {
        if (owner == null)
            return;

        if (isPinned && ReferenceEquals(owner, currentOwner))
        {
            ClearPin();
            return;
        }

        isPinned = true;
        dragHidden = false;
        currentOwner = owner;
        currentModel = model;
        currentAnchor = anchor;
        hasCurrentModel = true;

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        ShowNow();
    }

    public void Pin(object owner, TooltipModel model, TooltipAnchor anchor)
    {
        if (owner == null)
            return;

        isPinned = true;
        dragHidden = false;
        currentOwner = owner;
        currentModel = model;
        currentAnchor = anchor;
        hasCurrentModel = true;

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        ShowNow();
    }

    public void ClearPin()
    {
        if (!isPinned)
            return;

        isPinned = false;
        dragHidden = false;
        currentOwner = null;
        hasCurrentModel = false;

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        HideImmediate();
    }

    public void ClearOwner(object owner)
    {
        if (owner == null)
            return;

        if (isPinned && ReferenceEquals(owner, currentOwner))
        {
            ClearPin();
            return;
        }

        EndHover(owner);
    }

    public void HideForDrag()
    {
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        if (isPinned)
        {
            dragHidden = true;
            HideImmediate();
            return;
        }

        currentOwner = null;
        hasCurrentModel = false;
        HideImmediate();
    }

    public void RestoreAfterDrag()
    {
        if (!isPinned || !dragHidden)
            return;

        dragHidden = false;
        ShowNow();
    }

    IEnumerator ShowDelayed()
    {
        float delay = Mathf.Max(0f, showDelaySeconds);
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        if (!hasCurrentModel || currentOwner == null)
        {
            showRoutine = null;
            yield break;
        }

        ShowNow();
        showRoutine = null;
    }

    void ShowNow()
    {
        if (tooltipCanvas == null || tooltipView == null)
            return;

        var canvasRect = CanvasRect;
        if (canvasRect == null)
            return;

        if (!hasCurrentModel)
            return;

        
        tooltipView.Show(currentModel);
        UpdateToggleState();

        var tooltipRect = tooltipView.rectTransform;
        if (tooltipRect == null)
            return;

        
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

        
        float scaleFactor = tooltipCanvas.scaleFactor <= 0f ? 1f : tooltipCanvas.scaleFactor;

        
        float tooltipWidth = tooltipRect.rect.width * scaleFactor;
        float tooltipHeight = tooltipRect.rect.height * scaleFactor;

        float padding = Mathf.Max(0f, edgePadding);
        float screenW = Screen.width;
        float screenH = Screen.height;

        Vector2 screenPos;

        switch (currentAnchor.Type)
        {
            case TooltipAnchorType.World:
            {
                if (worldCamera == null)
                {
                    Debug.LogWarning("[TooltipService] World camera is null. Cannot place world tooltip.");
                    return;
                }

                Vector2 basePos = worldCamera.WorldToScreenPoint(currentAnchor.WorldPosition);

                
                float x = basePos.x + screenOffset.x;
                float right = x + tooltipWidth;

                bool fitsRight = right <= (screenW - padding);
                if (!fitsRight)
                {
                    
                    x = basePos.x - screenOffset.x - tooltipWidth;
                }

                
                float minX = padding;
                float maxX = screenW - padding - tooltipWidth;
                x = Mathf.Clamp(x, minX, maxX);

                
                float y = basePos.y + screenOffset.y;
                float top = y;
                float bottom = y - tooltipHeight;

                if (top > screenH - padding)
                    y = screenH - padding;

                if (bottom < padding)
                    y = padding + tooltipHeight;

                screenPos = new Vector2(x, y);
                break;
            }

            case TooltipAnchorType.Screen:
            {
                
                
                Vector2 rightTop = currentAnchor.ScreenRightTop;
                Vector2 leftTop = currentAnchor.ScreenLeftTop;

                
                float xRightLeft = rightTop.x + screenOffset.x;
                float xRightRight = xRightLeft + tooltipWidth;

                
                
                
                float xLeftLeft = leftTop.x - screenOffset.x - tooltipWidth;
                float xLeftRight = xLeftLeft + tooltipWidth;

                bool canPlaceRight = xRightRight <= (screenW - padding);

                float xCandidate = canPlaceRight ? xRightLeft : xLeftLeft;

                
                float minX = padding;
                float maxX = screenW - padding - tooltipWidth;
                float x = Mathf.Clamp(xCandidate, minX, maxX);

                
                float baseY = rightTop.y; 
                float y = baseY + screenOffset.y;

                float top = y;
                float bottom = y - tooltipHeight;

                if (top > screenH - padding)
                    y = screenH - padding;

                if (bottom < padding)
                    y = padding + tooltipHeight;

                screenPos = new Vector2(x, y);
                break;
            }

            default:
                return;
        }

        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            null,
            out var localPos
        );

        
        tooltipRect.anchoredPosition = localPos;
    }

    void UpdateToggleState()
    {
        if (tooltipView == null)
            return;

        if (!isPinned)
        {
            tooltipView.SetToggleButton(false, null, default, false, null);
            return;
        }

        var config = currentModel.buttonConfig;
        if (config == null)
        {
            tooltipView.SetToggleButton(false, null, default, false, null);
            return;
        }

        tooltipView.SetToggleButton(true, config.LabelKey, config.BackgroundColor, config.Interactable, config.OnClick);
    }

    void HideImmediate()
    {
        if (tooltipView != null)
            tooltipView.Hide();
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
}


}
