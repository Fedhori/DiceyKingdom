using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public sealed class UiBlink : MonoBehaviour
{
    [SerializeField] float periodSeconds = 0.5f;

    [Tooltip("baseColor RGB에 곱해지는 배수")]
    [SerializeField] float minBrightness = 0.6f;

    [Tooltip("baseColor RGB에 곱해지는 배수")]
    [SerializeField] float maxBrightness = 1.2f;

    Graphic graphic;
    Color baseColor;

    void Awake()
    {
        graphic = GetComponent<Graphic>();
        baseColor = graphic.color;
    }

    void OnEnable()
    {
        Apply(1f);
    }

    void OnDisable()
    {
        graphic.color = baseColor;
    }

    void Update()
    {
        float p = Mathf.Max(0.0001f, periodSeconds);
        float t = Time.unscaledTime / p * Mathf.PI * 2f;
        float s = (Mathf.Sin(t) + 1f) * 0.5f; // 0..1
        Apply(s);
    }

    void Apply(float s01)
    {
        float b = Mathf.Lerp(minBrightness, maxBrightness, s01);

        Color c = baseColor;

        c.r = Mathf.Clamp01(c.r * b);
        c.g = Mathf.Clamp01(c.g * b);
        c.b = Mathf.Clamp01(c.b * b);

        c.a = baseColor.a; // 알파는 그대로 유지
        graphic.color = c;
    }
}