using System;
using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [SerializeField] float moveHeight = 50f;

    [Header("Easing Curves")]
    [SerializeField] AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] AnimationCurve fadeCurve = AnimationCurve.Linear(0, 1, 1, 0);
    [SerializeField] AnimationCurve scaleCurve = AnimationCurve.Linear(0, 1, 1, 1);

    TMP_Text textMesh;
    RectTransform rectTransform;
    Transform cachedTransform;

    float timeElapsed;
    float currentLifetime;
    Vector2 startAnchoredPosition;
    Vector3 startScale;
    Action onComplete;
    Color baseColor;

    void Awake()
    {
        cachedTransform = transform;
        textMesh = GetComponent<TMP_Text>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Bind(
        string text,
        Color color,
        float fontSize,
        float lifetime,
        Vector3 startPosition,      // 캔버스 로컬 좌표 (anchoredPosition 용)
        Action onComplete)
    {
        if (textMesh == null)
            textMesh = GetComponent<TMP_Text>();
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        textMesh.text = text;
        textMesh.fontSize = fontSize;

        baseColor = color;
        textMesh.color = color;

        currentLifetime = Mathf.Max(0.01f, lifetime);
        timeElapsed = 0f;

        startScale = Vector3.one;
        cachedTransform.localScale = startScale;

        startAnchoredPosition = new Vector2(startPosition.x, startPosition.y);
        rectTransform.anchoredPosition = startAnchoredPosition;

        this.onComplete = onComplete;
    }

    void Update()
    {
        if (currentLifetime <= 0f)
            return;

        timeElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(timeElapsed / currentLifetime);

        float moveValue = moveCurve.Evaluate(t);
        float fadeValue = fadeCurve.Evaluate(t);
        float scaleValue = scaleCurve.Evaluate(t);

        rectTransform.anchoredPosition =
            startAnchoredPosition + new Vector2(0f, moveValue * moveHeight);

        Color c = baseColor;
        c.a *= fadeValue;
        textMesh.color = c;

        cachedTransform.localScale = startScale * scaleValue;

        if (timeElapsed >= currentLifetime)
        {
            var callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }
    }
}
