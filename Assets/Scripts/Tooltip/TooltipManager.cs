using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    [SerializeField] Canvas tooltipCanvas;      // Screen Space - Overlay
    [SerializeField] TooltipView tooltipView;
    [SerializeField] Camera worldCamera;
    [SerializeField] float showDelay = 0.2f;
    [SerializeField] Vector2 screenOffset = new Vector2(16f, -16f);

    object currentOwner;
    TooltipModel currentModel;
    TooltipAnchor currentAnchor;
    bool hasCurrentModel;

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
        UpdateWorldCamera();
    }

    void OnDisable()
    {
        if (Instance == this)
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        UpdateWorldCamera();

        currentOwner = null;
        hasCurrentModel = false;

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

        Vector2 screenPos;

        switch (currentAnchor.Type)
        {
            case TooltipAnchorType.World:
                if (worldCamera == null)
                {
                    Debug.LogWarning("[TooltipManager] World camera is null. Cannot place world tooltip.");
                    return;
                }

                screenPos = worldCamera.WorldToScreenPoint(currentAnchor.WorldPosition);
                break;

            case TooltipAnchorType.Screen:
                screenPos = currentAnchor.ScreenPosition;
                break;

            default:
                return;
        }

        screenPos += screenOffset;

        RectTransform tooltipRect = tooltipView.rectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            null,
            out var localPos
        );

        tooltipRect.anchoredPosition = localPos;

        tooltipView.Show(currentModel);
    }

    void HideImmediate()
    {
        if (tooltipView != null)
            tooltipView.Hide();
    }
}