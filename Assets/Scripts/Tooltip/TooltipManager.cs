using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    [SerializeField] Canvas tooltipCanvas;      // Screen Space - Overlay
    [SerializeField] TooltipView tooltipView;
    [SerializeField] Camera worldCamera;
    [SerializeField] float showDelay = 0.2f;
    [SerializeField] Vector2 screenOffset = new Vector2(16f, -16f);

    // 화면 가장자리와의 최소 여백
    [SerializeField] float edgePadding = 8f;

    object currentOwner;
    TooltipModel currentModel;
    TooltipAnchor currentAnchor;
    bool hasCurrentModel;
    bool isPinned;
    bool dragHidden;

    Coroutine showRoutine;

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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (tooltipCanvas == null)
            tooltipCanvas = GetComponentInParent<Canvas>();

        UpdateWorldCamera();
    }

    void OnEnable()
    {
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        UiSelectionEvents.OnSelectionCleared += HandleSelectionCleared;
        UpdateWorldCamera();
    }

    void OnDisable()
    {
        if (Instance == this)
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        UiSelectionEvents.OnSelectionCleared -= HandleSelectionCleared;
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
            Debug.LogWarning("[TooltipManager] No world camera found. Tooltips will not be positioned.");
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

    void HandleSelectionCleared()
    {
        ClearPin();
    }

    IEnumerator ShowDelayed()
    {
        if (showDelay > 0f)
            yield return new WaitForSecondsRealtime(showDelay);

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

        // 1) 내용 먼저 세팅해서 rect 크기를 최신 상태로 만든다.
        tooltipView.Show(currentModel);

        var tooltipRect = tooltipView.rectTransform;
        if (tooltipRect == null)
            return;

        // 레이아웃 그룹/콘텐츠 사이즈 피터가 있을 수 있으니 즉시 갱신
        LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);

        // Canvas → Pixel 스케일
        float scaleFactor = tooltipCanvas.scaleFactor <= 0f ? 1f : tooltipCanvas.scaleFactor;

        // 툴팁 픽셀 크기
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
                    Debug.LogWarning("[TooltipManager] World camera is null. Cannot place world tooltip.");
                    return;
                }

                Vector2 basePos = worldCamera.WorldToScreenPoint(currentAnchor.WorldPosition);

                // 기본: 우측 배치 시도 (basePos: 대상의 우상단이라고 가정)
                float x = basePos.x + screenOffset.x;
                float right = x + tooltipWidth;

                bool fitsRight = right <= (screenW - padding);
                if (!fitsRight)
                {
                    // 우측에 두면 잘리므로, 같은 앵커에서 좌측으로 플립
                    x = basePos.x - screenOffset.x - tooltipWidth;
                }

                // 좌우 clamp
                float minX = padding;
                float maxX = screenW - padding - tooltipWidth;
                x = Mathf.Clamp(x, minX, maxX);

                // 수직 방향: offset 적용 후 clamp
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
                // Screen 기준: 우상단 / 좌상단 둘 다 알고 있으므로
                // 오른쪽/왼쪽 후보를 각각 계산해서 선택.
                Vector2 rightTop = currentAnchor.ScreenRightTop;
                Vector2 leftTop = currentAnchor.ScreenLeftTop;

                // 오른쪽 배치 후보
                float xRightLeft = rightTop.x + screenOffset.x;
                float xRightRight = xRightLeft + tooltipWidth;

                // 왼쪽 배치 후보
                // 왼쪽일 때: tooltipRight = leftTop.x - offset.x
                //          tooltipLeft  = tooltipRight - tooltipWidth
                float xLeftLeft = leftTop.x - screenOffset.x - tooltipWidth;
                float xLeftRight = xLeftLeft + tooltipWidth;

                bool canPlaceRight = xRightRight <= (screenW - padding);

                float xCandidate = canPlaceRight ? xRightLeft : xLeftLeft;

                // 좌우 clamp
                float minX = padding;
                float maxX = screenW - padding - tooltipWidth;
                float x = Mathf.Clamp(xCandidate, minX, maxX);

                // 수직 방향: top 기준은 양쪽 다 동일한 y 를 사용
                float baseY = rightTop.y; // leftTop.y 와 동일해야 함
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

        // 3) 최종 Screen → Canvas local 변환 후 배치
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            null,
            out var localPos
        );

        // pivot = (0,1) 이므로 localPos는 "툴팁 좌상단" 위치
        tooltipRect.anchoredPosition = localPos;
    }

    void HideImmediate()
    {
        if (tooltipView != null)
            tooltipView.Hide();
    }
}
