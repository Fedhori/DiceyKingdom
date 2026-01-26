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
        delta = Vector2.ClampMagnitude(delta, radius);
        currentVector = radius > 0f ? delta / radius : Vector2.zero;

        SetBaseAndHandle(startPos, startPos + delta);
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
            handleRect.anchoredPosition = handlePos;
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
