using UnityEngine;
using UnityEngine.InputSystem;

public sealed class PinDragManager : MonoBehaviour
{
    public static PinDragManager Instance { get; private set; }

    [SerializeField] Camera worldCamera;
    [SerializeField] LayerMask pinLayerMask;

    [Header("Drag Visual")]
    [SerializeField, Range(0f, 1f)] float dragAlpha = 0.5f;
    [SerializeField] float dragZOffset = -0.1f;

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

    void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || worldCamera == null)
            return;

        if (draggingPin != null && !CanDragPins())
        {
            CancelDrag();
            return;
        }

        var mousePos = mouse.position.ReadValue();

        if (draggingPin == null)
        {
            if (!CanDragPins())
                return;

            if (mouse.leftButton.wasPressedThisFrame)
                TryBeginDrag(mousePos);
        }
        else
        {
            if (mouse.leftButton.isPressed)
                UpdateDrag(mousePos);

            if (mouse.leftButton.wasReleasedThisFrame)
                EndDrag(mousePos);
        }
    }

    bool CanDragPins()
    {
        var flow = FlowManager.Instance;
        if (flow == null)
            return true;

        return flow.CanDragPins;
    }

    void TryBeginDrag(Vector2 screenPos)
    {
        var worldPos = ScreenToWorld(screenPos);

        var hit = Physics2D.OverlapPoint(worldPos, pinLayerMask);
        if (hit == null)
            return;

        var pin = hit.GetComponentInParent<PinController>();
        if (pin == null)
            return;

        draggingPin = pin;
        originalPosition = pin.transform.position;
        originalRow = pin.RowIndex;
        originalCol = pin.ColumnIndex;

        draggingCollider = hit;
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
    }

    void UpdateDrag(Vector2 screenPos)
    {
        if (draggingPin == null)
            return;

        var worldPos = ScreenToWorld(screenPos);
        draggingPin.transform.position = new Vector3(worldPos.x, worldPos.y, originalPosition.z + dragZOffset);

        var target = FindTargetPin(worldPos);
        if (target == draggingPin)
        {
            Debug.Log("Target is Self");
            return;
        }

        UpdateHighlight(target);
    }

    void EndDrag(Vector2 screenPos)
    {
        if (draggingPin == null)
            return;

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

    void CancelDrag()
    {
        if (draggingPin != null)
            draggingPin.transform.position = originalPosition;

        RestoreCollider();
        RestoreSprite();
        ClearHighlight();
        draggingPin = null;
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
