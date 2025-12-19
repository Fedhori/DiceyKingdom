using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class UiBlink : MonoBehaviour
{
    [SerializeField] float periodSeconds = 0.5f;
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
        float p = Mathf.Max(0.0001f, periodSeconds);
        float t = Time.unscaledTime / p * Mathf.PI * 2f;
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