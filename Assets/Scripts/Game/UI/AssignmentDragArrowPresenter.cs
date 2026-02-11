using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AssignmentDragArrowPresenter : MonoBehaviour
{
    [SerializeField] RectTransform overlayRoot;
    [SerializeField] float lineThickness = 6f;
    [SerializeField] Color lineColor = new(0.92f, 0.96f, 1f, 0.95f);
    [SerializeField] Color arrowColor = new(0.96f, 0.84f, 0.26f, 1f);
    [SerializeField] float arrowFontSize = 44f;

    RectTransform lineRect;
    Image lineImage;
    RectTransform arrowRect;
    TextMeshProUGUI arrowText;
    Canvas canvas;
    Vector2 dragStartScreenPosition;
    bool isDragging;

    void Awake()
    {
        if (overlayRoot == null)
            overlayRoot = transform as RectTransform;

        canvas = GetComponentInParent<Canvas>();
        EnsureVisuals();
        SetVisualsActive(false);
    }

    void OnEnable()
    {
        AssignmentDragSession.DragStarted += OnDragStarted;
        AssignmentDragSession.DragMoved += OnDragMoved;
        AssignmentDragSession.DragEnded += OnDragEnded;
    }

    void OnDisable()
    {
        AssignmentDragSession.DragStarted -= OnDragStarted;
        AssignmentDragSession.DragMoved -= OnDragMoved;
        AssignmentDragSession.DragEnded -= OnDragEnded;
    }

    void OnDragStarted(string _, Vector2 startScreenPosition)
    {
        if (overlayRoot == null)
            return;

        isDragging = true;
        dragStartScreenPosition = startScreenPosition;
        UpdateVisual(startScreenPosition, startScreenPosition);
        SetVisualsActive(true);
    }

    void OnDragMoved(string _, Vector2 currentScreenPosition)
    {
        if (!isDragging)
            return;

        UpdateVisual(dragStartScreenPosition, currentScreenPosition);
    }

    void OnDragEnded(string _, bool __)
    {
        isDragging = false;
        SetVisualsActive(false);
    }

    void UpdateVisual(Vector2 startScreenPosition, Vector2 endScreenPosition)
    {
        if (!TryScreenToLocal(startScreenPosition, out var startLocal))
            return;
        if (!TryScreenToLocal(endScreenPosition, out var endLocal))
            return;

        var delta = endLocal - startLocal;
        float distance = delta.magnitude;
        if (distance < 0.1f)
            distance = 0.1f;

        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        var midpoint = (startLocal + endLocal) * 0.5f;

        lineRect.anchoredPosition = midpoint;
        lineRect.sizeDelta = new Vector2(distance, lineThickness);
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);

        arrowRect.anchoredPosition = endLocal;
        arrowRect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    bool TryScreenToLocal(Vector2 screenPosition, out Vector2 localPosition)
    {
        localPosition = Vector2.zero;
        if (overlayRoot == null)
            return false;

        Camera eventCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            eventCamera = canvas.worldCamera;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            overlayRoot,
            screenPosition,
            eventCamera,
            out localPosition);
    }

    void EnsureVisuals()
    {
        if (overlayRoot == null)
            return;

        if (lineRect == null)
        {
            var lineObject = new GameObject("DragLine", typeof(RectTransform), typeof(Image));
            lineObject.layer = LayerMask.NameToLayer("UI");
            lineRect = lineObject.GetComponent<RectTransform>();
            lineRect.SetParent(overlayRoot, false);
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);

            lineImage = lineObject.GetComponent<Image>();
            lineImage.raycastTarget = false;
            lineImage.color = lineColor;
        }

        if (arrowRect == null)
        {
            var arrowObject = new GameObject("DragArrowHead", typeof(RectTransform), typeof(TextMeshProUGUI));
            arrowObject.layer = LayerMask.NameToLayer("UI");
            arrowRect = arrowObject.GetComponent<RectTransform>();
            arrowRect.SetParent(overlayRoot, false);
            arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
            arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.sizeDelta = new Vector2(36f, 36f);

            arrowText = arrowObject.GetComponent<TextMeshProUGUI>();
            arrowText.raycastTarget = false;
            arrowText.fontSize = arrowFontSize;
            arrowText.fontStyle = FontStyles.Bold;
            arrowText.alignment = TextAlignmentOptions.Center;
            arrowText.color = arrowColor;
            arrowText.text = ">";
        }

        lineRect.SetAsLastSibling();
        arrowRect.SetAsLastSibling();
    }

    void SetVisualsActive(bool isActive)
    {
        if (lineRect != null && lineRect.gameObject.activeSelf != isActive)
            lineRect.gameObject.SetActive(isActive);
        if (arrowRect != null && arrowRect.gameObject.activeSelf != isActive)
            arrowRect.gameObject.SetActive(isActive);
    }
}
