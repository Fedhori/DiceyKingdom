using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public sealed class UiBlink : MonoBehaviour
{
    [SerializeField] float periodSeconds = 0.5f;

    [Tooltip("baseColor alpha에 곱해지는 최소 배수")]
    [FormerlySerializedAs("minBrightness")]
    [SerializeField] float minAlphaMultiplier = 0.6f;

    [Tooltip("baseColor alpha에 곱해지는 최대 배수")]
    [FormerlySerializedAs("maxBrightness")]
    [SerializeField] float maxAlphaMultiplier = 1.0f;

    Graphic graphic;
    Color baseColor;
    int tweenId = -1;

    void Awake()
    {
        graphic = GetComponent<Graphic>();
        baseColor = graphic.color;
    }

    void Start()
    {
        StartTween();
    }

    void OnDestroy()
    {
        StopTween();
        graphic.color = baseColor;
    }

    void StartTween()
    {
        StopTween();

        float p = Mathf.Max(0.0001f, periodSeconds);
        float half = p * 0.5f;

        ApplyAlphaMultiplier(maxAlphaMultiplier);

        tweenId = LeanTween.value(gameObject, maxAlphaMultiplier, minAlphaMultiplier, half)
            .setEaseInOutSine()
            .setLoopPingPong()
            .setOnUpdate(ApplyAlphaMultiplier)
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

    void ApplyAlphaMultiplier(float multiplier)
    {
        Color c = baseColor;
        c.a = Mathf.Clamp01(baseColor.a * multiplier);
        graphic.color = c;
    }
}
