using UnityEngine;
using UnityEngine.EventSystems;

public sealed class VirtualJoystickController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static VirtualJoystickController Instance { get; private set; }

    [SerializeField] private RectTransform baseRect;
    [SerializeField] private RectTransform handleRect;
    [SerializeField] private Canvas canvas;
    [SerializeField] private float radius = 100f;

    int activePointerId = -1;
    Vector2 startPos;
    Vector2 currentVector;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SetVisible(false);
    }

    void Update()
    {
        var stage = StageManager.Instance;
        if (activePointerId >= 0 && (stage == null || stage.CurrentPhase != StagePhase.Play))
            ResetJoystick();
    }

    public Vector2 GetMoveVector()
    {
        return currentVector;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsAllowedPlatform())
            return;

        if (!IsPlayable())
            return;

        if (activePointerId >= 0)
            return;

        activePointerId = eventData.pointerId;
        startPos = ScreenToCanvasPoint(eventData.position);
        currentVector = Vector2.zero;

        SetVisible(true);
        SetBaseAndHandle(startPos, startPos);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (activePointerId != eventData.pointerId)
            return;

        if (!IsPlayable())
        {
            ResetJoystick();
            return;
        }

        Vector2 pos = ScreenToCanvasPoint(eventData.position);
        Vector2 delta = pos - startPos;
        if (delta.sqrMagnitude <= 0.0001f || radius <= 0f)
        {
            currentVector = Vector2.zero;
            SetBaseAndHandle(startPos, startPos);
            return;
        }

        Vector2 direction = SnapToEightDirections(delta.normalized);
        currentVector = direction;

        Vector2 handleOffset = direction * radius;
        SetBaseAndHandle(startPos, startPos + handleOffset);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (activePointerId != eventData.pointerId)
            return;

        ResetJoystick();
    }

    bool IsPlayable()
    {
        var stage = StageManager.Instance;
        return stage != null && stage.CurrentPhase == StagePhase.Play;
    }

    static bool IsAllowedPlatform()
    {
#if UNITY_EDITOR
        return true;
#else
        return Application.isMobilePlatform;
#endif
    }

    void ResetJoystick()
    {
        activePointerId = -1;
        currentVector = Vector2.zero;
        SetVisible(false);
    }

    void SetVisible(bool visible)
    {
        if (baseRect != null)
            baseRect.gameObject.SetActive(visible);
        if (handleRect != null)
            handleRect.gameObject.SetActive(visible);
    }

    void SetBaseAndHandle(Vector2 basePos, Vector2 handlePos)
    {
        if (baseRect != null)
            baseRect.anchoredPosition = basePos;
        if (handleRect != null)
        {
            if (baseRect != null && handleRect.transform.parent == baseRect)
                handleRect.anchoredPosition = handlePos - basePos;
            else
                handleRect.anchoredPosition = handlePos;
        }
    }

    static Vector2 SnapToEightDirections(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
            return Vector2.zero;

        float angle = Mathf.Atan2(direction.y, direction.x);
        float step = Mathf.PI / 4f;
        float snapped = Mathf.Round(angle / step) * step;
        return new Vector2(Mathf.Cos(snapped), Mathf.Sin(snapped));
    }

    Vector2 ScreenToCanvasPoint(Vector2 screenPos)
    {
        var cv = canvas != null ? canvas : GetComponentInParent<Canvas>();
        if (cv == null)
            return screenPos;

        var root = cv.transform as RectTransform;
        if (root == null)
            return screenPos;

        Camera cam = null;
        if (cv.renderMode is RenderMode.ScreenSpaceCamera or RenderMode.WorldSpace)
            cam = cv.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPos, cam, out var localPoint);
        return localPoint;
    }
}
