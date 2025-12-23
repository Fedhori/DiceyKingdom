using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class PinDragManager : MonoBehaviour
{
    public static PinDragManager Instance { get; private set; }

    [SerializeField] Camera worldCamera;
    [SerializeField] LayerMask pinLayerMask;

    [Header("Drag Visual")]
    [SerializeField, Range(0f, 1f)] float dragAlpha = 0.5f;
    [SerializeField] float minMoveSpeed = 8f;
    [SerializeField] float moveDuration = 0.2f;

    PinController draggingPin;
    Collider2D draggingCollider;
    PinController highlightedPin;
    bool overSellArea;
    Vector3 originalPosition;
    Vector2 originalScreenPos;
    Graphic[] cachedGraphics = System.Array.Empty<Graphic>();
    SpriteRenderer[] cachedSprites = System.Array.Empty<SpriteRenderer>();
    Color[] spriteOriginalColors = System.Array.Empty<Color>();
    Coroutine animationRoutine;

    bool IsAnimating => animationRoutine != null;

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

        if (pin.Instance != null && pin.Instance.Id == GameConfig.BasicPinId)
            return false;

        if (!CanDragPins())
            return false;

        StopActiveAnimation();

        draggingPin = pin;
        originalPosition = pin.transform.position;
        originalScreenPos = WorldToScreen(originalPosition);
        overSellArea = false;

        CacheVisuals(pin);
        SetVisualsEnabled(pin, false);
        DisableCollider(pin);
        ShowGhost(pin, screenPos);
        SellOverlayController.Instance?.Show();

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

        GhostManager.Instance?.UpdateGhostPosition(screenPos);

        var worldPos = ScreenToWorld(screenPos);
        var target = FindTargetPin(worldPos);
        if (target == draggingPin)
            target = null;

        UpdateHighlight(target);

        var sellOverlay = SellOverlayController.Instance;
        overSellArea = sellOverlay != null && sellOverlay.ContainsScreenPoint(screenPos);
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

        var sellOverlay = SellOverlayController.Instance;
        overSellArea = sellOverlay != null && sellOverlay.ContainsScreenPoint(screenPos);

        if (overSellArea)
        {
            GhostManager.Instance?.HideGhost(GhostKind.Pin);
            draggingPin.transform.position = originalPosition;
            RestoreCollider();
            SetVisualsEnabled(draggingPin, true);
            ClearHighlight();
            sellOverlay?.Hide();

            var pinToSell = draggingPin;
            draggingPin = null;
            PinManager.Instance?.RequestSellPin(pinToSell);
            return;
        }

        ClearHighlight();

        var worldPos = ScreenToWorld(screenPos);
        var target = FindTargetPin(worldPos);

        if (target != null && target != draggingPin && PinManager.Instance != null)
        {
            animationRoutine = StartCoroutine(PlaySwapAnimation(draggingPin, target, worldPos));
        }
        else
        {
            animationRoutine = StartCoroutine(PlayReturnAnimation());
        }
    }

    public bool IsDragging(PinController pin)
    {
        return pin != null && draggingPin == pin;
    }

    public void CancelDrag()
    {
        StopActiveAnimation();

        RestoreCollider();
        SetVisualsEnabled(draggingPin, true);
        GhostManager.Instance?.HideGhost(GhostKind.Pin);
        ClearHighlight();
        SellOverlayController.Instance?.Hide();
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

    void ClearHighlight()
    {
        if (highlightedPin != null && highlightedPin.dragHighlightMask != null)
            highlightedPin.dragHighlightMask.SetActive(false);

        highlightedPin = null;
    }

    void UpdateHighlight(PinController target)
    {
        if (highlightedPin != null && highlightedPin.dragHighlightMask != null)
            highlightedPin.dragHighlightMask.SetActive(false);

        highlightedPin = null;

        if (target == null)
            return;

        if (target.dragHighlightMask != null)
        {
            target.dragHighlightMask.SetActive(true);
            highlightedPin = target;
        }
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

    Vector2 WorldToScreen(Vector3 worldPos)
    {
        return worldCamera != null ? (Vector2)worldCamera.WorldToScreenPoint(worldPos) : Vector2.zero;
    }

    void CacheVisuals(PinController pin)
    {
        cachedGraphics = pin != null ? pin.GetComponentsInChildren<Graphic>(true) : System.Array.Empty<Graphic>();
        cachedSprites = pin != null ? pin.GetComponentsInChildren<SpriteRenderer>(true) : System.Array.Empty<SpriteRenderer>();
        spriteOriginalColors = new Color[cachedSprites.Length];
        for (int i = 0; i < cachedSprites.Length; i++)
            spriteOriginalColors[i] = cachedSprites[i].color;
    }

    void SetVisualsEnabled(PinController pin, bool enabled)
    {
        if (pin == null)
            return;

        for (int i = 0; i < cachedGraphics.Length; i++)
        {
            var g = cachedGraphics[i];
            if (g == null)
                continue;

            g.enabled = enabled;
            g.raycastTarget = enabled;
        }

        for (int i = 0; i < cachedSprites.Length; i++)
        {
            var sr = cachedSprites[i];
            if (sr == null)
                continue;

            sr.enabled = enabled;
            var c = spriteOriginalColors[i];
            if (!enabled)
                c.a = dragAlpha;
            sr.color = c;
        }
    }

    void DisableCollider(PinController pin)
    {
        draggingCollider = pin != null ? pin.GetComponent<Collider2D>() : null;
        if (draggingCollider != null)
            draggingCollider.enabled = false;
    }

    void ShowGhost(PinController pin, Vector2 screenPos)
    {
        var ghost = GhostManager.Instance;
        if (ghost == null)
            return;

        Sprite sprite = null;
        if (pin != null && pin.Instance != null)
            sprite = SpriteCache.GetPinSprite(pin.Instance.Id);

        ghost.ShowGhost(sprite, screenPos, GhostKind.Pin);
    }

    IEnumerator PlayReturnAnimation()
    {
        var ghost = GhostManager.Instance;
        if (ghost == null || draggingPin == null)
        {
            RestoreCollider();
            SetVisualsEnabled(draggingPin, true);
            draggingPin = null;
            animationRoutine = null;
            yield break;
        }

        Vector2 start = ghost.IsVisible ? ghost.CurrentScreenPosition : originalScreenPos;
        Vector2 end = originalScreenPos;
        float duration = ComputeGhostDuration(start, end);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / duration);
            var pos = Vector2.Lerp(start, end, u);
            ghost.UpdateGhostPosition(pos);
            yield return null;
        }

        ghost.UpdateGhostPosition(end);
        ghost.HideGhost(GhostKind.Pin);
        RestoreCollider();
        SetVisualsEnabled(draggingPin, true);
        draggingPin = null;
        animationRoutine = null;
        SellOverlayController.Instance?.Hide();
    }

    IEnumerator PlaySwapAnimation(PinController current, PinController target, Vector3 dropWorldPos)
    {
        var ghost = GhostManager.Instance;
        if (ghost != null)
            ghost.HideGhost(GhostKind.Pin);

        if (current == null || target == null)
        {
            RestoreCollider();
            SetVisualsEnabled(current, true);
            draggingPin = null;
            animationRoutine = null;
            yield break;
        }

        SetVisualsEnabled(current, true);
        SetVisualsEnabled(target, true);
        var targetCollider = target.GetComponent<Collider2D>();
        bool targetColliderWasEnabled = targetCollider != null && targetCollider.enabled;
        if (targetCollider != null)
            targetCollider.enabled = false;

        // 현재 핀은 고스트 위치에서 목표 슬롯으로, 타겟 핀은 동시에 원본 슬롯으로 이동
        current.transform.position = dropWorldPos;

        Vector3 posAStart = dropWorldPos;
        Vector3 posAEnd = target.transform.position;
        Vector3 posBStart = target.transform.position;
        Vector3 posBEnd = originalPosition;

        float durA = ComputeDuration(posAStart, posAEnd);
        float durB = ComputeDuration(posBStart, posBEnd);

        var moveA = MoveTransform(current.transform, posAStart, posAEnd, durA);
        var moveB = MoveTransform(target.transform, posBStart, posBEnd, durB);

        while (moveA.MoveNext() | moveB.MoveNext())
            yield return null;

        current.transform.position = posAEnd;
        target.transform.position = posBEnd;

        PinManager.Instance?.SwapPins(current, target, moveTransforms: false);

        RestoreCollider();
        if (targetCollider != null)
            targetCollider.enabled = targetColliderWasEnabled;

        draggingPin = null;
        animationRoutine = null;
        SellOverlayController.Instance?.Hide();
    }

    float ComputeDuration(Vector3 from, Vector3 to)
    {
        float dist = Vector3.Distance(from, to);
        float speedByTime = dist <= 0.001f || moveDuration <= 0.001f ? minMoveSpeed : dist / moveDuration;
        float speed = Mathf.Max(minMoveSpeed, speedByTime);
        return speed <= 0f ? 0.01f : dist / speed;
    }

    float ComputeGhostDuration(Vector2 from, Vector2 to)
    {
        float dist = Vector2.Distance(from, to);
        float speed = moveDuration > 0.001f ? dist / moveDuration : float.MaxValue;
        if (speed <= 0f || float.IsInfinity(speed))
            speed = minMoveSpeed;
        return speed <= 0f ? 0.01f : dist / speed;
    }

    IEnumerator MoveTransform(Transform t, Vector3 from, Vector3 to, float duration)
    {
        if (t == null)
            yield break;

        float tAccum = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (tAccum < duration)
        {
            tAccum += Time.deltaTime;
            float u = Mathf.Clamp01(tAccum / duration);
            t.position = Vector3.Lerp(from, to, u);
            yield return null;
        }

        t.position = to;
    }

    void StopActiveAnimation()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        if (draggingPin != null)
        {
            RestoreCollider();
            SetVisualsEnabled(draggingPin, true);
            draggingPin = null;
        }

        GhostManager.Instance?.HideGhost(GhostKind.Pin);
        ClearHighlight();
        SellOverlayController.Instance?.Hide();
    }
}
