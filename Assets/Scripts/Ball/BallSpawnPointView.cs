using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public sealed class BallSpawnPointView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.3f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.7f;
    [SerializeField] private float fadePeriod = 1.2f;

    public System.Action<BallSpawnPointView> OnClicked;

    System.Collections.IEnumerator fadeRoutine;
    bool isSelected;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        RefreshFadeState();
    }

    void OnDisable()
    {
        StopFade();
        ApplyAlpha(isSelected ? 1f : CalculateAlpha());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(this);
    }

    public void SetSelected(bool selected)
    {
        if (isSelected == selected)
            return;

        isSelected = selected;
        RefreshFadeState();
    }

    void RefreshFadeState()
    {
        if (isSelected)
        {
            StopFade();
            ApplyAlpha(1f);
        }
        else
        {
            ApplyAlpha(CalculateAlpha());
            StartFade();
        }
    }

    void StartFade()
    {
        StopFade();
        fadeRoutine = FadeRoutine();
        StartCoroutine(fadeRoutine);
    }

    void StopFade()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
    }

    System.Collections.IEnumerator FadeRoutine()
    {
        if (spriteRenderer == null)
            yield break;

        float period = Mathf.Max(0.01f, fadePeriod);
        while (true)
        {
            float t = Mathf.PingPong(Time.time / period, 1f);
            float a = Mathf.Lerp(minAlpha, maxAlpha, t);
            ApplyAlpha(a);
            yield return null;
        }
    }

    void ApplyAlpha(float alpha)
    {
        if (spriteRenderer == null)
            return;

        var c = spriteRenderer.color;
        c.a = alpha;
        spriteRenderer.color = c;
    }

    float CalculateAlpha()
    {
        float period = Mathf.Max(0.01f, fadePeriod);
        float t = Mathf.PingPong(Time.time / period, 1f);
        return Mathf.Lerp(minAlpha, maxAlpha, t);
    }
}
