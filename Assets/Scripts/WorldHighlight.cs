using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public sealed class WorldHighlight : MonoBehaviour
{
    [SerializeField] bool highlightOnStart = false;
    [SerializeField] Color highlightColor = Color.cyan;
    [SerializeField] bool pulse = false;
    [SerializeField] float pulseSpeed = 4f;
    [SerializeField, Range(0f, 1f)] float minMultiplier = 0.6f;
    [SerializeField, Range(0f, 2f)] float maxMultiplier = 1.4f;

    SpriteRenderer sprite;
    Color baseColor;
    bool visible;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        baseColor = sprite != null ? sprite.color : Color.white;

        visible = highlightOnStart;
        ApplyImmediate();
    }

    void OnEnable()
    {
        ApplyImmediate();
    }

    void Update()
    {
        if (sprite == null)
            return;

        if (!visible || !pulse)
            return;

        float t = (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f;
        float m = Mathf.Lerp(minMultiplier, maxMultiplier, t);

        var c = highlightColor;
        c.a = baseColor.a * Mathf.Clamp01(m);
        sprite.color = c;
    }

    void ApplyImmediate()
    {
        if (sprite == null)
            return;

        if (!visible)
        {
            sprite.color = baseColor;
            return;
        }

        sprite.color = highlightColor;
    }

    public void SetHighlight(bool on)
    {
        visible = on;
        ApplyImmediate();
    }

    public void SetPulse(bool on)
    {
        pulse = on;

        if (!pulse && visible)
            sprite.color = highlightColor;
    }
}