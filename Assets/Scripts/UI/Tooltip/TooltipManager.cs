using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    [SerializeField] Canvas tooltipCanvas;          // Screen Space - Overlay
    [SerializeField] TooltipView tooltipView;
    [SerializeField] Camera worldCamera;            // 핀을 보는 카메라
    [SerializeField] float showDelay = 0.2f;        // 호버 후 표시 딜레이
    [SerializeField] Vector2 screenOffset = new Vector2(16f, -16f);

    RectTransform CanvasRect => tooltipCanvas != null
        ? tooltipCanvas.transform as RectTransform
        : null;

    PinInstance currentPin;
    Vector3 currentWorldPos;
    Coroutine showRoutine;

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
        // 씬이 이미 로드된 상태에서 Enable 될 수도 있으니 한 번 더 보정
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
        // 씬 바뀔 때 남아있던 툴팁은 숨겨두는게 안전
        currentPin = null;
        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }
        HideImmediate();
    }

    void UpdateWorldCamera()
    {
        // 인스펙터에서 이미 지정해둔 카메라가 살아있으면 그대로 사용
        if (worldCamera != null)
            return;

        // 없으면 MainCamera 태그로 다시 찾기
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

    /// <summary>
    /// 특정 핀에 포인터가 진입했을 때 호출.
    /// </summary>
    public void BeginHover(PinInstance pin, Vector3 worldPosition)
    {
        if (pin == null)
            return;

        currentPin      = pin;
        currentWorldPos = worldPosition;

        if (showRoutine != null)
            StopCoroutine(showRoutine);

        showRoutine = StartCoroutine(ShowDelayed());
    }

    /// <summary>
    /// 해당 핀에서 포인터가 빠져나갈 때 호출.
    /// </summary>
    public void EndHover(PinInstance pin)
    {
        if (pin != null && pin != currentPin)
            return; // 다른 핀에서 온 Exit 이면 무시

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        currentPin = null;
        HideImmediate();
    }

    System.Collections.IEnumerator ShowDelayed()
    {
        if (showDelay > 0f)
            yield return new WaitForSecondsRealtime(showDelay);

        if (currentPin == null)
            yield break;

        ShowNow();
        showRoutine = null;
    }

    void ShowNow()
    {
        if (tooltipCanvas == null || tooltipView == null || worldCamera == null)
            return;

        var canvasRect = CanvasRect;
        if (canvasRect == null)
            return;

        Vector2 screenPos = worldCamera.WorldToScreenPoint(currentWorldPos);
        screenPos += screenOffset;

        RectTransform tooltipRect = tooltipView.rectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPos,
            null,                     // Overlay Canvas
            out var localPos
        );

        tooltipRect.anchoredPosition = localPos;

        tooltipView.Show(currentPin);
    }

    void HideImmediate()
    {
        if (tooltipView != null)
            tooltipView.Hide();
    }
}
