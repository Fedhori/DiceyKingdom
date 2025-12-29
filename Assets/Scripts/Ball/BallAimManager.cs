using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class BallAimManager : MonoBehaviour
{
    [SerializeField] Collider2D dragAreaCollider;
    [SerializeField] Camera worldCamera;
    [SerializeField] Transform cannonAnchor;
    [SerializeField] SpriteRenderer startMarker;
    [SerializeField] LineRenderer aimLine;

    [SerializeField] float fixedStartY = 0f;
    [SerializeField] float minDragDistance = 0.5f;
    [Header("Cannon Angles & Speed")]
    [SerializeField, Range(0f, 89f)] float minUpAngleDegrees = 15f;
    [SerializeField, Range(91f, 179f)] float maxUpAngleDegrees = 165f;
    [SerializeField] float initialAngleDegrees = 90f;
    [SerializeField] float rotateDegreesPerSecond = 30f;

    [SerializeField] float aimLineLength = 160f;

    [SerializeField] float pixelsPerUnit = 1f;
    [SerializeField] float lineWidthPx = 8f;
    [SerializeField] float dashPx = 16f;
    [SerializeField] float gapPx = 16f;

    [Header("Trajectory (first hit)")]
    [SerializeField] LayerMask aimCollideMask;
    [SerializeField] LayerMask sideWallMask;
    [SerializeField] int maxSideBounces = 8;
    [SerializeField] float castSkin = 0.001f;

    [Header("Render (segments)")]
    [SerializeField] int maxAimSegments = 16;

    public Vector2 AimOrigin { get; private set; }
    public Vector2 AimDirection { get; private set; }
    public bool HasValidAim { get; private set; }

    bool dragging;
    Vector2 dragStart;

    readonly List<Vector3> aimPoints = new();
    readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[8];
    readonly List<LineRenderer> segmentLines = new();

    void Awake()
    {
        if (worldCamera == null)
            worldCamera = Camera.main;

        InitSegmentLines();
        ApplyLineStyle();
        HideVisuals();
    }

    void Update()
    {
        if (FlowManager.Instance != null && !FlowManager.Instance.CanAimBalls)
        {
            CancelDrag();
            return;
        }

        var pointer = Pointer.current;
        if (pointer == null) return;

        var press = pointer.press;
        if (press == null) return;

        Vector2 screenPos = pointer.position.ReadValue();

        if (press.wasPressedThisFrame) TryBeginDrag(screenPos);
        else if (dragging && press.isPressed) UpdateDrag(screenPos);
        else if (dragging && press.wasReleasedThisFrame) EndDrag(screenPos);
    }

    public void ResetAim()
    {
        dragging = false;
        HasValidAim = false;
        AimDirection = Vector2.up;
        HideVisuals();
    }

    void InitSegmentLines()
    {
        segmentLines.Clear();
        if (aimLine == null) return;

        segmentLines.Add(aimLine);
        aimLine.positionCount = 0;
        aimLine.enabled = false;

        int target = Mathf.Max(1, maxAimSegments);
        for (int i = 1; i < target; i++)
        {
            var clone = Instantiate(aimLine, aimLine.transform.parent);
            clone.name = $"{aimLine.name}_Seg{i}";
            clone.positionCount = 0;
            clone.enabled = false;
            segmentLines.Add(clone);
        }
    }

    void ApplyLineStyle()
    {
        if (segmentLines.Count == 0) return;

        float widthWorld = lineWidthPx / Mathf.Max(0.0001f, pixelsPerUnit);

        for (int i = 0; i < segmentLines.Count; i++)
        {
            var lr = segmentLines[i];
            if (lr == null) continue;

            lr.startWidth = widthWorld;
            lr.endWidth = widthWorld;
            lr.textureMode = LineTextureMode.Tile;
            lr.numCornerVertices = 0;
            lr.numCapVertices = 0;
            lr.useWorldSpace = true;
        }
    }

    void TryBeginDrag(Vector2 screenPos)
    {
        if (worldCamera == null || dragAreaCollider == null) return;

        Vector3 worldPos = ScreenToWorldOnZ0(screenPos);
        if (!dragAreaCollider.OverlapPoint(worldPos)) return;

        dragging = true;
        dragStart = new Vector2(worldPos.x, fixedStartY);

        AimOrigin = dragStart;
        HasValidAim = false;
        AimDirection = Vector2.zero;

        ShowStartMarker(dragStart);
        UpdateAimLine(Vector2.zero, false);
    }

    void UpdateDrag(Vector2 screenPos)
    {
        if (!dragging || worldCamera == null) return;

        Vector3 worldPos = ScreenToWorldOnZ0(screenPos);
        Vector2 dragVec = dragStart - (Vector2)worldPos;

        bool valid = dragVec.magnitude >= minDragDistance;

        Vector2 adjustedDir = dragVec.normalized;
        if (valid)
        {
            float absAngle = Vector2.Angle(Vector2.up, dragVec);
            float maxAngle = 90f - minUpAngleDegrees;
            if (absAngle > maxAngle)
            {
                float rad = maxAngle * Mathf.Deg2Rad;
                float xSign = Mathf.Sign(dragVec.x);
                adjustedDir = new Vector2(Mathf.Sin(rad) * xSign, Mathf.Cos(rad));
            }
        }

        HasValidAim = valid;
        AimDirection = valid ? adjustedDir : Vector2.zero;

        UpdateAimLine(adjustedDir, valid);
    }

    void EndDrag(Vector2 screenPos)
    {
        if (!dragging) return;

        UpdateDrag(screenPos);

        if (!HasValidAim)
        {
            CancelDrag();
            return;
        }

        dragging = false;
        StageManager.Instance?.OnAimConfirmed(AimOrigin, AimDirection);
    }

    void CancelDrag()
    {
        dragging = false;
        HasValidAim = false;
        AimDirection = Vector2.zero;
        HideVisuals();
    }

    Vector3 ScreenToWorldOnZ0(Vector2 screenPos)
    {
        float z = -worldCamera.transform.position.z;
        Vector3 wp = worldCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, z));
        wp.z = 0f;
        return wp;
    }

    void ShowStartMarker(Vector2 pos)
    {
        if (startMarker == null) return;

        startMarker.transform.position = new Vector3(pos.x, pos.y, startMarker.transform.position.z);
        startMarker.enabled = true;
    }

    void UpdateAimLine(Vector2 aimDir, bool valid)
    {
        if (segmentLines.Count == 0) return;

        if (!dragging || !valid || aimDir == Vector2.zero)
        {
            DisableAllSegments();
            return;
        }

        BuildFirstHitPath(dragStart, aimDir.normalized, aimLineLength);

        int segCount = Mathf.Max(0, aimPoints.Count - 1);
        if (segCount == 0)
        {
            DisableAllSegments();
            return;
        }

        float periodWorld = (dashPx + gapPx) / Mathf.Max(0.0001f, pixelsPerUnit);
        Vector2 texScale = new Vector2(1f / Mathf.Max(0.0001f, periodWorld), 1f);

        int usable = Mathf.Min(segCount, segmentLines.Count);

        for (int i = 0; i < usable; i++)
        {
            var lr = segmentLines[i];
            if (lr == null) continue;

            lr.positionCount = 2;
            lr.SetPosition(0, aimPoints[i]);
            lr.SetPosition(1, aimPoints[i + 1]);
            lr.textureScale = texScale;
            lr.enabled = true;
        }

        for (int i = usable; i < segmentLines.Count; i++)
        {
            var lr = segmentLines[i];
            if (lr == null) continue;
            lr.positionCount = 0;
            lr.enabled = false;
        }
    }

    void BuildFirstHitPath(Vector2 origin, Vector2 dir, float maxLen)
    {
        aimPoints.Clear();
        aimPoints.Add(new Vector3(origin.x, origin.y, 0f));

        float remaining = maxLen;
        Vector2 pos = origin;

        var filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = aimCollideMask;
        filter.useTriggers = false;

        Collider2D prevWall = null;
        int wallBounces = 0;
        int safety = 0;

        while (remaining > 0f && wallBounces <= maxSideBounces && safety++ < maxSideBounces + 32)
        {
            int count = Physics2D.CircleCast(pos, GameConfig.BallRadius, dir, filter, hitBuffer, remaining);

            if (count <= 0)
            {
                pos += dir * remaining;
                aimPoints.Add(new Vector3(pos.x, pos.y, 0f));
                break;
            }

            bool found = false;
            RaycastHit2D best = default;
            float bestDist = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                var h = hitBuffer[i];
                if (h.collider == null) continue;

                bool isSideWall = IsInLayerMask(h.collider.gameObject.layer, sideWallMask);
                if (isSideWall && h.collider == prevWall) continue;

                if (h.distance < bestDist)
                {
                    best = h;
                    bestDist = h.distance;
                    found = true;
                }
            }

            if (!found)
            {
                float step = Mathf.Max(castSkin, 0.01f);
                pos += dir * step;
                remaining = Mathf.Max(0f, remaining - step);
                prevWall = null;
                continue;
            }

            float d = Mathf.Max(0f, best.distance);
            pos += dir * d;
            aimPoints.Add(new Vector3(pos.x, pos.y, 0f));

            bool hitSideWall = IsInLayerMask(best.collider.gameObject.layer, sideWallMask);
            if (!hitSideWall)
                break;

            remaining -= d;
            if (remaining <= 0f) break;

            Vector2 n = best.normal;
            if (n.sqrMagnitude < 0.0001f) break;

            dir = Vector2.Reflect(dir, n).normalized;

            float push = Mathf.Max(castSkin, 0.01f);
            pos += n * push;
            remaining = Mathf.Max(0f, remaining - push);

            prevWall = best.collider;
            wallBounces++;
        }
    }

    static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    void DisableAllSegments()
    {
        for (int i = 0; i < segmentLines.Count; i++)
        {
            var lr = segmentLines[i];
            if (lr == null) continue;
            lr.positionCount = 0;
            lr.enabled = false;
        }
    }

    void HideVisuals()
    {
        if (startMarker != null) startMarker.enabled = false;
        DisableAllSegments();
    }
}
