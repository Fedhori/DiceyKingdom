using UnityEngine;
using TMPro;
using System;

public class FloatingText : MonoBehaviour
{
    [SerializeField] private float moveHeight = 50f;

    [Header("Easing Curves")]
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.Linear(0, 1, 1, 0);
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.Linear(0, 1, 1, 1);

    private TMP_Text textMesh;
    private RectTransform rectTransform;
    private Transform cachedTransform;

    private float timeElapsed;
    private float currentLifetime;
    private Vector3 startWorldPosition;
    private Vector2 startAnchoredPosition;
    private Vector3 startScale;
    private Action onComplete;
    private Color baseColor;
    private bool useWorldSpace;

    private void Awake()
    {
        cachedTransform = transform;
        textMesh = GetComponent<TMP_Text>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Bind(string text, Color color, float fontSize, float lifetime, Vector3 startPosition, bool worldSpace, Action onComplete)
    {
        if (textMesh == null) textMesh = GetComponent<TMP_Text>();

        textMesh.text = text;
        textMesh.fontSize = fontSize;
        baseColor = color;
        textMesh.color = color;
        currentLifetime = Mathf.Max(0.01f, lifetime);
        timeElapsed = 0f;
        cachedTransform.localScale = Vector3.one;
        startScale = cachedTransform.localScale;
        this.onComplete = onComplete;
        useWorldSpace = worldSpace;

        if (useWorldSpace || rectTransform == null)
        {
            startWorldPosition = startPosition;
            cachedTransform.position = startWorldPosition;
        }
        else
        {
            startAnchoredPosition = new Vector2(startPosition.x, startPosition.y);
            rectTransform.anchoredPosition = startAnchoredPosition;
        }
    }

    private void Update()
    {
        timeElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(timeElapsed / currentLifetime);

        float moveValue = moveCurve.Evaluate(t);
        float fadeValue = fadeCurve.Evaluate(t);
        float scaleValue = scaleCurve.Evaluate(t);

        if (useWorldSpace || rectTransform == null)
            cachedTransform.position = startWorldPosition + new Vector3(0f, moveValue * moveHeight, 0f);
        else
            rectTransform.anchoredPosition = startAnchoredPosition + new Vector2(0f, moveValue * moveHeight);

        Color color = baseColor;
        color.a *= fadeValue;
        textMesh.color = color;

        cachedTransform.localScale = startScale * scaleValue;

        if (timeElapsed >= currentLifetime) onComplete?.Invoke();
    }
}







