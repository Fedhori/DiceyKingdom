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
    int tweenId = -1;

    void Awake()
    {
        graphic = GetComponent<Graphic>();
        baseColor = graphic.color;
    }

    void OnEnable()
    {
        StartTween();
    }

    void OnDisable()
    {
        StopTween();
        graphic.color = baseColor;
    }

    void StartTween()
    {
        StopTween();

        float p = Mathf.Max(0.0001f, periodSeconds);
        float half = p * 0.5f;

        ApplyBrightness(maxBrightness);

        tweenId = LeanTween.value(gameObject, maxBrightness, minBrightness, half)
            .setEaseInOutSine()
            .setLoopPingPong()
            .setOnUpdate((float b) => ApplyBrightness(b))
            .id;
    }

    void StopTween()
    {
        if (tweenId >= 0)
        {
            LeanTween.cancel(tweenId);
            tweenId = -1;
        }
    }

    void ApplyBrightness(float b)
    {
        Color c = baseColor;

        c.r = Mathf.Clamp01(c.r * b);
        c.g = Mathf.Clamp01(c.g * b);
        c.b = Mathf.Clamp01(c.b * b);

        c.a = baseColor.a;
        graphic.color = c;
    }
}