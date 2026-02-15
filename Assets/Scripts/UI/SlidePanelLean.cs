using System;
using UnityEngine;

/// <summary>
/// Unity component that manages slide panel lean runtime behavior.
/// </summary>
public class SlidePanelLean : MonoBehaviour
{
    [Header("Hide Movement")]
    [SerializeField] private Vector2 direction = Vector2.right; // ?④만 ???대룞??諛⑺뼢

    [Header("Tween")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private LeanTweenType ease = LeanTweenType.easeInOutCubic;
    [SerializeField] private bool startHidden = true;           // ?쒖옉???④? ?곹깭濡??섏?

    private LTDescr currentTween;
    public bool IsShown { get; private set; }

    // 湲곗?(蹂댁씠?? ?꾩튂
    private Vector2 shownPos;
    RectTransform panel;
    private Vector2 DirNorm => (direction.sqrMagnitude > 0f) ? direction.normalized : Vector2.right;

    void Awake()
    {
        panel = GetComponent<RectTransform>();

        // ?꾩옱 ?⑤꼸 ?먮━瑜?'蹂댁씠???먮━'濡????
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

    // 湲곗? ?꾩튂瑜??꾩옱 ?꾩튂濡??ъ꽕?뺥븯怨??띠쓣 ???몄텧
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

