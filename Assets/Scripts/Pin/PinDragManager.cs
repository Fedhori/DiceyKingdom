using UnityEngine;

public sealed class PinDragManager : MonoBehaviour
{
    public static PinDragManager Instance { get; private set; }

    [SerializeField] Camera worldCamera;
    [SerializeField] LayerMask pinLayerMask;

    [Header("Drag Visual")]
    [SerializeField, Range(0f, 1f)] float dragAlpha = 0.5f;

    PinController draggingPin;
    SpriteRenderer draggingSprite;
    Color originalColor;
    Vector3 originalPosition;
    int originalRow;
    int originalCol;
    
    Collider2D draggingCollider;

    WorldHighlight currentHighlight;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (pinLayerMask == 0)
            pinLayerMask = LayerMaskUtil.PinLayer;
    }

    bool CanDragPins()
    {
        var flow = FlowManager.Instance;
        if (flow == null)
            return true;

        return flow.CanDragPins;
    }

    public bool BeginDrag(PinController pin, Vector2 screenPos)
    {
        if (pin == null || worldCamera == null)
            return false;

        if (!CanDragPins())
            return false;

        draggingPin = pin;
        originalPosition = pin.transform.position;
        originalRow = pin.RowIndex;
        originalCol = pin.ColumnIndex;

        draggingCollider = pin.GetComponent<Collider2D>();
        if (draggingCollider != null)
            draggingCollider.enabled = false;

        draggingSprite = pin.GetComponentInChildren<SpriteRenderer>();
        if (draggingSprite != null)
        {
            originalColor = draggingSprite.color;
            var c = originalColor;
            c.a = dragAlpha;
            draggingSprite.color = c;
        }

        return true;
    }

    public void UpdateDrag(Vector2 screenPos)
    {
        if (draggingPin == null)
            return;

        if (!CanDragPins())
        {
            CancelDrag();
            return;
        }

        var worldPos = ScreenToWorld(screenPos);
        draggingPin.transform.position = new Vector2(worldPos.x, worldPos.y);

        var target = FindTargetPin(worldPos);
        if (target == draggingPin)
            target = null;

        UpdateHighlight(target);
    }

    public void EndDrag(Vector2 screenPos)
    {
        if (draggingPin == null)
            return;

        if (!CanDragPins())
        {
            CancelDrag();
            return;
        }

        var worldPos = ScreenToWorld(screenPos);
        var target = FindTargetPin(worldPos);

        if (target != null && target != draggingPin && PinManager.Instance != null)
        {
            PinManager.Instance.SwapPins(draggingPin, target);
        }
        else
        {
            draggingPin.transform.position = originalPosition;
        }

        RestoreCollider();
        RestoreSprite();
        ClearHighlight();
        draggingPin = null;
    }

    public bool IsDragging(PinController pin)
    {
        return pin != null && draggingPin == pin;
    }

    public void CancelDrag()
    {
        if (draggingPin != null)
            draggingPin.transform.position = originalPosition;

        RestoreCollider();
        RestoreSprite();
        ClearHighlight();
        draggingPin = null;
    }

    public void CancelDragFromPin(PinController pin)
    {
        if (pin == null)
            return;

        if (draggingPin != pin)
            return;

        CancelDrag();
    }

    void RestoreCollider()
    {
        if (draggingCollider != null)
        {
            draggingCollider.enabled = true;
            draggingCollider = null;
        }
    }

    void RestoreSprite()
    {
        if (draggingSprite != null)
        {
            draggingSprite.color = originalColor;
            draggingSprite = null;
        }
    }

    void ClearHighlight()
    {
        if (currentHighlight != null)
        {
            currentHighlight.SetPulse(false);
            currentHighlight.SetHighlight(false);
            currentHighlight = null;
        }
    }

    void UpdateHighlight(PinController target)
    {
        if (currentHighlight != null)
        {
            currentHighlight.SetPulse(false);
            currentHighlight.SetHighlight(false);
            currentHighlight = null;
        }

        if (target == null)
            return;

        var h = target.GetComponent<WorldHighlight>();
        if (h == null)
            return;

        h.SetHighlight(true);
        h.SetPulse(true);
        currentHighlight = h;
    }

    PinController FindTargetPin(Vector3 worldPos)
    {
        var hit = Physics2D.OverlapPoint(worldPos, pinLayerMask);
        if (hit == null)
            return null;

        return hit.GetComponentInParent<PinController>();
    }

    Vector3 ScreenToWorld(Vector2 screenPos)
    {
        var sp = new Vector3(screenPos.x, screenPos.y, -worldCamera.transform.position.z);
        return worldCamera.ScreenToWorldPoint(sp);
    }
}
