using System;
using UnityEngine;

public class SlidePanelLean : MonoBehaviour
{
    [Header("Hide Movement")]
    [SerializeField] private Vector2 direction = Vector2.right; // 숨길 때 이동할 방향

    [Header("Tween")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private LeanTweenType ease = LeanTweenType.easeInOutCubic;
    [SerializeField] private bool startHidden = true;           // 시작을 숨김 상태로 둘지

    private LTDescr currentTween;
    public bool IsShown { get; private set; }

    // 기준(보이는) 위치
    private Vector2 shownPos;
    RectTransform panel;
    private Vector2 DirNorm => (direction.sqrMagnitude > 0f) ? direction.normalized : Vector2.right;

    void Awake()
    {
        panel = GetComponent<RectTransform>();

        // 현재 패널 자리를 '보이는 자리'로 저장
        shownPos = panel.anchoredPosition;

        if (startHidden)
        {
            panel.anchoredPosition = GetHiddenPosition();
            IsShown = false;
        }
        else
        {
            panel.anchoredPosition = shownPos;
            IsShown = true;
        }
    }

    public void Show()  => StartMove(shownPos, true, null);
    public void Hide()  => StartMove(GetHiddenPosition(), false, null);
    public void Show(Action onComplete)  => StartMove(shownPos, true, onComplete);
    public void Hide(Action onComplete)  => StartMove(GetHiddenPosition(), false, onComplete);
    public void Toggle() { if (IsShown) Hide(); else Show(); }
    public void HideImmediate() => ApplyImmediate(GetHiddenPosition(), false);
    public void ShowImmediate() => ApplyImmediate(shownPos, true);

    private void StartMove(Vector2 end, bool toShown, Action onComplete)
    {
        if (!panel) return;

        if (currentTween != null) LeanTween.cancel(panel.gameObject);

        currentTween = LeanTween.move(panel, end, duration)
            .setEase(ease)
            .setIgnoreTimeScale(true)
            .setOnComplete(() =>
            {
                IsShown = toShown;
                currentTween = null;
                onComplete?.Invoke();
            });
    }

    void ApplyImmediate(Vector2 pos, bool shown)
    {
        if (!panel) return;

        if (currentTween != null)
            LeanTween.cancel(panel.gameObject);

        panel.anchoredPosition = pos;
        IsShown = shown;
        currentTween = null;
    }

    // 기준 위치를 현재 위치로 재설정하고 싶을 때 호출
    public void ReanchorShownToCurrent()
    {
        shownPos = panel.anchoredPosition;
    }

    Vector2 GetHiddenPosition()
    {
        if (panel == null)
            return shownPos;

        var parent = panel.parent as RectTransform;
        if (parent == null)
            return shownPos;

        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, panel);
        var parentRect = parent.rect;
        var dir = DirNorm;
        float needed;

        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
        {
            needed = dir.x >= 0f
                ? parentRect.xMax - bounds.min.x
                : bounds.max.x - parentRect.xMin;
        }
        else
        {
            needed = dir.y >= 0f
                ? parentRect.yMax - bounds.min.y
                : bounds.max.y - parentRect.yMin;
        }

        return shownPos + dir * Mathf.Max(0f, needed);
    }
}
