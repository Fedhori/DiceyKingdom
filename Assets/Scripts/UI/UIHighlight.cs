using System;
using UnityEngine;
using Coffee.UIEffects;   // UIEffect 네임스페이스

[RequireComponent(typeof(UIEffect))]
public sealed class UIHighlight : MonoBehaviour
{
    [SerializeField] bool highlightOnStart = false;
    [SerializeField] bool pulseOnStart     = false;

    [Header("Pulse Settings")]
    [SerializeField] bool isPulsing = false;
    [SerializeField] float speed = 4f;              // 깜빡이는 속도
    [SerializeField, Range(0f, 1f)]
    float minFadeMultiplier = 0.3f;                 // 기본 Shadow Fade 의 최소 배수
    [SerializeField, Range(0f, 1f)]
    float maxFadeMultiplier = 1.0f;
    [SerializeField, Range(0f, 1f)]
    float startShadowAlpha = 1.0f;

    UIEffect effect;

    // 시작 시 UIEffect에서 가져온 기본 Shadow Color
    Color baseShadowColor;

    bool highlightVisible = false;                  // 하이라이트 “자체”가 켜져 있는지

    void Awake()
    {
        effect = GetComponent<UIEffect>();

        // 인스펙터에서 설정한 Shadow Color를 기본값으로 캐싱
        baseShadowColor = effect.shadowColor;

        highlightVisible = highlightOnStart;
        isPulsing        = pulseOnStart;

        ApplyImmediate();
    }

    void OnEnable()
    {
        if (effect == null)
            effect = GetComponent<UIEffect>();

        ApplyImmediate();
    }

    private void OnDisable()
    {
        ApplyImmediate();
    }

    void Update()
    {
        if (effect == null)
            return;

        // 하이라이트 자체가 꺼져 있으면, UIEffect 비활성 + 리턴
        if (!highlightVisible)
        {
            effect.enabled    = false;
            effect.shadowFade = 0f;
            return;
        }

        effect.enabled = true;
        effect.shadowFade = startShadowAlpha;

        if (!isPulsing)
            return;

        // 0~1 사이 t
        float t = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
        float m = Mathf.Lerp(minFadeMultiplier, maxFadeMultiplier, t);

        effect.shadowFade = Mathf.Clamp01(startShadowAlpha * m);
    }

    void ApplyImmediate()
    {
        if (effect == null)
            return;

        if (!highlightVisible)
        {
            effect.enabled    = false;
            effect.shadowFade = 0f;
            return;
        }

        effect.enabled    = true;
        effect.shadowFade = startShadowAlpha;

        if (!isPulsing)
            effect.shadowFade = startShadowAlpha;
    }
    
    public void SetHighlight(bool on, Color? colorOverride = null)
    {
        if (!this)
        {
            Debug.LogError("UIHighlight is invalid");
            return;
        }
           

        if (effect == null)
            effect = GetComponent<UIEffect>();

        highlightVisible = on;

        if (!on)
        {
            isPulsing = false;

            if (effect != null)
            {
                effect.shadowFade  = 0f;
                effect.shadowColor = baseShadowColor;
            }

            ApplyImmediate();
            return;
        }

        if (effect != null)
        {
            effect.shadowColor = colorOverride.HasValue
                ? colorOverride.Value
                : baseShadowColor;
        }

        ApplyImmediate();
    }

    /// <summary>
    /// 깜빡임(펄스) 켜기/끄기. 하이라이트가 켜져 있어야 의미 있음.
    /// </summary>
    public void SetPulse(bool on)
    {
        isPulsing = on;

        if (effect == null)
            effect = GetComponent<UIEffect>();

        if (!highlightVisible)
        {
            // 하이라이트가 꺼져 있으면 펄스는 아무 의미 없음
            ApplyImmediate();
            return;
        }

        if (!on && effect != null)
        {
            // 펄스 끄면 기본 알파로 고정
            effect.shadowFade = startShadowAlpha;
        }
    }
}
