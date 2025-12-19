using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public sealed class UiBlink : MonoBehaviour
{
    [SerializeField] float frequencyHz = 2f;
    [SerializeField] float minAlpha = 0.2f;
    [SerializeField] float maxAlpha = 1f;

    Graphic graphic;
    CanvasGroup canvasGroup;
    Color baseColor;

    void Awake()
    {
        graphic = GetComponent<Graphic>();
        canvasGroup = GetComponent<CanvasGroup>();

        baseColor = graphic.color;
    }

    void OnEnable()
    {
        ApplyAlpha(maxAlpha);
    }

    void OnDisable()
    {
        ApplyAlpha(maxAlpha);
    }

    void Update()
    {
        float t = Time.unscaledTime * frequencyHz * Mathf.PI * 2f;
        float s = (Mathf.Sin(t) + 1f) * 0.5f;
        float a = Mathf.Lerp(minAlpha, maxAlpha, s);
        ApplyAlpha(a);
    }

    void ApplyAlpha(float a)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = a;
            return;
        }

        var c = baseColor;
        c.a = a;
        graphic.color = c;
    }
}