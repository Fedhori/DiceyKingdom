using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
/// <summary>
/// Unity component that manages hover scale runtime behavior.
/// </summary>
public sealed class HoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform target;   // 鍮꾩썙?먮㈃ ?먭린 ?먯떊
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float duration = 0.08f;

    Vector3 baseScale;
    Coroutine scaleRoutine;

    void Awake()
    {
        if (!target)
            target = transform as RectTransform;

        if (target != null)
            baseScale = target.localScale;
    }

    void Start()
    {
        if (target != null)
            target.localScale = baseScale;
    }

    void OnDestroy()
    {
        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            scaleRoutine = null;
        }

        if (target != null)
            target.localScale = baseScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StartScale(baseScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartScale(baseScale);
    }

    void StartScale(Vector3 to)
    {
        if (target == null)
            return;

        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        scaleRoutine = StartCoroutine(ScaleCoroutine(to));
    }

    IEnumerator ScaleCoroutine(Vector3 to)
    {
        Vector3 from = target.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;  // ?쇱떆?뺤? ?곹깭?먯꽌???숈옉?섍쾶(unscaled time)
            float t = Mathf.Clamp01(elapsed / duration);

            // 遺?쒕윭??蹂닿컙(smooth interpolation)
            t = t * t * (3f - 2f * t); // smoothstep

            target.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }

        target.localScale = to;
        scaleRoutine = null;
    }
}

