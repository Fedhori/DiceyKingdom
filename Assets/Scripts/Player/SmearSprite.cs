using UnityEngine;

public sealed class SmearSprite : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    float lifetimeSeconds = 0.15f;
    float elapsedSeconds;
    float startAlpha = 0.5f;
    Color baseColor = Color.white;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void Initialize(
        Sprite sprite,
        Color color,
        int sortingLayerId,
        int sortingOrder,
        float lifetimeSeconds,
        float startAlpha,
        Vector2 size)
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        this.lifetimeSeconds = Mathf.Max(0.01f, lifetimeSeconds);
        this.startAlpha = Mathf.Clamp01(startAlpha);
        elapsedSeconds = 0f;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            spriteRenderer.size = size;
            spriteRenderer.sortingLayerID = sortingLayerId;
            spriteRenderer.sortingOrder = sortingOrder;

            baseColor = color;
            baseColor.a = this.startAlpha;
            spriteRenderer.color = baseColor;
        }

        gameObject.SetActive(true);
    }

    void Update()
    {
        if (lifetimeSeconds <= 0f)
            return;

        elapsedSeconds += Time.deltaTime;
        float t = Mathf.Clamp01(elapsedSeconds / lifetimeSeconds);

        if (spriteRenderer != null)
        {
            var c = baseColor;
            c.a = Mathf.Lerp(startAlpha, 0f, t);
            spriteRenderer.color = c;
        }

        if (elapsedSeconds >= lifetimeSeconds)
            gameObject.SetActive(false);
    }
}
