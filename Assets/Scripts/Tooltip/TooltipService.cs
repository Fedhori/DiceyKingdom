using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Unity component that provides tooltip runtime behavior.
/// </summary>
public sealed class TooltipService : MonoBehaviour
{
    [SerializeField] Canvas tooltipCanvas;      // Screen Space - Overlay
    [SerializeField] TooltipView tooltipView;
    [SerializeField] Camera worldCamera;
    [SerializeField] Vector2 screenOffset = new Vector2(16f, -16f);

    // ?붾㈃ 媛?μ옄由ъ???理쒖냼 ?щ갚
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

        // 1) ?댁슜 癒쇱? ?명똿?댁꽌 rect ?ш린瑜?理쒖떊 ?곹깭濡?留뚮뱺??
        tooltipView.Show(currentModel);
        UpdateToggleState();

        var tooltipRect = tooltipView.rectTransform;
        if (tooltipRect == null)
            return;

        // ?덉씠?꾩썐 洹몃９/肄섑뀗痢??ъ씠利??쇳꽣媛 ?덉쓣 ???덉쑝??利됱떆 媛깆떊
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

        // Canvas ??Pixel ?ㅼ???
        float scaleFactor = tooltipCanvas.scaleFactor <= 0f ? 1f : tooltipCanvas.scaleFactor;

        // ?댄똻 ?쎌? ?ш린
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

                // 湲곕낯: ?곗륫 諛곗튂 ?쒕룄 (basePos: ??곸쓽 ?곗긽?⑥씠?쇨퀬 媛??
                float x = basePos.x + screenOffset.x;
                float right = x + tooltipWidth;

                bool fitsRight = right <= (screenW - padding);
                if (!fitsRight)
                {
                    // ?곗륫???먮㈃ ?섎━誘濡? 媛숈? ?듭빱?먯꽌 醫뚯륫?쇰줈 ?뚮┰
                    x = basePos.x - screenOffset.x - tooltipWidth;
                }

                // 醫뚯슦 clamp
                float minX = padding;
                float maxX = screenW - padding - tooltipWidth;
                x = Mathf.Clamp(x, minX, maxX);

                // ?섏쭅 諛⑺뼢: offset ?곸슜 ??clamp
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
                // Screen 湲곗?: ?곗긽??/ 醫뚯긽???????뚭퀬 ?덉쑝誘濡?
                // ?ㅻⅨ履??쇱そ ?꾨낫瑜?媛곴컖 怨꾩궛?댁꽌 ?좏깮.
                Vector2 rightTop = currentAnchor.ScreenRightTop;
                Vector2 leftTop = currentAnchor.ScreenLeftTop;

                // ?ㅻⅨ履?諛곗튂 ?꾨낫
                float xRightLeft = rightTop.x + screenOffset.x;
                float xRightRight = xRightLeft + tooltipWidth;

                // ?쇱そ 諛곗튂 ?꾨낫
                // ?쇱そ???? tooltipRight = leftTop.x - offset.x
                //          tooltipLeft  = tooltipRight - tooltipWidth
                float xLeftLeft = leftTop.x - screenOffset.x - tooltipWidth;
                float xLeftRight = xLeftLeft + tooltipWidth;

                bool canPlaceRight = xRightRight <= (screenW - padding);

                float xCandidate = canPlaceRight ? xRightLeft : xLeftLeft;

                // 醫뚯슦 clamp
                float minX = padding;
                float maxX = screenW - padding - tooltipWidth;
                float x = Mathf.Clamp(xCandidate, minX, maxX);

                // ?섏쭅 諛⑺뼢: top 湲곗?? ?묒そ ???숈씪??y 瑜??ъ슜
                float baseY = rightTop.y; // leftTop.y ? ?숈씪?댁빞 ??
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

        // 3) 理쒖쥌 Screen ??Canvas local 蹂????諛곗튂
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            null,
            out var localPos
        );

        // pivot = (0,1) ?대?濡?localPos??"?댄똻 醫뚯긽?? ?꾩튂
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


