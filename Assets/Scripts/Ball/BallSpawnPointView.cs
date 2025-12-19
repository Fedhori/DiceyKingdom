using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public sealed class BallSpawnPointView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField, Range(0f, 1f)] private float minAlpha = 0.35f;
    [SerializeField, Range(0f, 1f)] private float maxAlpha = 1f;
    [SerializeField] private float fadePeriod = 1.2f;

    public System.Action<Vector2> OnClicked;

    Coroutine fadeRoutine;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        StartFade();
    }

    void OnDisable()
    {
        StopFade();
        ResetAlpha();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(transform.position);
    }

    void StartFade()
    {
        StopFade();
        fadeRoutine = StartCoroutine(FadeRoutine());
    }

    void StopFade()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
    }

    IEnumerator FadeRoutine()
    {
        if (spriteRenderer == null)
            yield break;

        float timer = 0f;
        while (true)
        {
            timer += Time.deltaTime;
            float t = Mathf.PingPong(timer / Mathf.Max(0.01f, fadePeriod), 1f);
            float a = Mathf.Lerp(minAlpha, maxAlpha, t);

            var c = spriteRenderer.color;
            c.a = a;
            spriteRenderer.color = c;

            yield return null;
        }
    }

    void ResetAlpha()
    {
        if (spriteRenderer == null)
            return;

        var c = spriteRenderer.color;
        c.a = maxAlpha;
        spriteRenderer.color = c;
    }
}
