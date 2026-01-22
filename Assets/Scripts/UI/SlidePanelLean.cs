using System;
using UnityEngine;

public class SlidePanelLean : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private RectTransform panel;        // 슬라이드할 패널(없으면 자동)

    [Header("Hide Movement")]
    [SerializeField] private Vector2 direction = Vector2.right; // 숨길 때 이동할 방향
    [SerializeField] private float scalar = 400f;                // 이동 거리(픽셀)
    [SerializeField] private bool usePanelHeight = true;        // 패널 높이 기반으로 이동 거리 계산
    [SerializeField] private float heightPadding = 500f;           // 높이 기반 이동 시 추가 여유값

    [Header("Tween")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private LeanTweenType ease = LeanTweenType.easeInOutCubic;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool startHidden = true;           // 시작을 숨김 상태로 둘지

    [Header("Interaction (optional)")]
    [SerializeField] private CanvasGroup canvasGroup;            // 없으면 자동
    [SerializeField] private bool blockRaycastsWhenHidden = true;

    private LTDescr currentTween;
    public bool IsShown { get; private set; }

    // 기준(보이는) 위치
    private Vector2 shownPos;
    private Vector2 DirNorm => (direction.sqrMagnitude > 0f) ? direction.normalized : Vector2.right;
    private Vector2 HiddenPos => shownPos + DirNorm * scalar;

    void Awake()
    {
        if (panel == null) panel = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        // 현재 패널 자리를 '보이는 자리'로 저장
        shownPos = panel.anchoredPosition;
        RefreshScalarFromPanel();

        if (startHidden)
        {
            panel.anchoredPosition = HiddenPos;
            IsShown = false;
            ApplyInteractable(false);
        }
        else
        {
            panel.anchoredPosition = shownPos;
            IsShown = true;
            ApplyInteractable(true);
        }
    }

    public void Show()  => StartMove(shownPos, true, null);
    public void Hide()  => StartMove(HiddenPos, false, null);
    public void Show(Action onComplete)  => StartMove(shownPos, true, onComplete);
    public void Hide(Action onComplete)  => StartMove(HiddenPos, false, onComplete);
    public void Toggle() { if (IsShown) Hide(); else Show(); }

    private void StartMove(Vector2 end, bool toShown, Action onComplete)
    {
        if (!panel) return;

        RefreshScalarFromPanel();

        if (currentTween != null) LeanTween.cancel(panel.gameObject);

        // 이동 중에는 입력 잠깐 끄기
        ApplyInteractable(false);

        currentTween = LeanTween.move(panel, end, duration)
            .setEase(ease)
            .setIgnoreTimeScale(useUnscaledTime)
            .setOnComplete(() =>
            {
                IsShown = toShown;
                ApplyInteractable(toShown);
                currentTween = null;
                onComplete?.Invoke();
            });
    }

    // 기준 위치를 현재 위치로 재설정하고 싶을 때 호출
    public void ReanchorShownToCurrent()
    {
        shownPos = panel.anchoredPosition;
    }

    void RefreshScalarFromPanel()
    {
        if (!usePanelHeight || panel == null)
            return;

        float height = panel.rect.height;
        if (height > 0f)
            scalar = height + Mathf.Max(0f, heightPadding);
    }

    private void ApplyInteractable(bool shown)
    {
        if (!canvasGroup) return;
        canvasGroup.interactable = shown;
        if (blockRaycastsWhenHidden)
            canvasGroup.blocksRaycasts = shown;
        // 원하면 페이드도 함께: canvasGroup.alpha = shown ? 1f : 0f;
    }
}
