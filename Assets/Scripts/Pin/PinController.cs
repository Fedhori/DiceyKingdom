using System.Collections;
using Data;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class PinController : MonoBehaviour
{
    public PinInstance Instance { get; private set; }

    [Header("Hit Scale Effect")]
    [SerializeField] float hitScaleMultiplier = 1.2f;
    [SerializeField] float growDuration = 0.06f;
    [SerializeField] float shrinkDuration = 0.08f;
    [SerializeField] private TMP_Text countText;
    private int count;

    bool initialized;
    Vector3 baseScale;
    Coroutine hitRoutine;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    public void Initialize(string pinId)
    {
        if (initialized)
        {
            Debug.LogWarning($"[PinController] Already initialized on {name}.");
            return;
        }

        PinDto dto;
        try
        {
            dto = PinRepository.GetOrThrow(pinId);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PinController] Failed to initialize {pinId}: {e}");
            return;
        }

        Instance = new PinInstance(dto);
        initialized = true;
    }

    public void PlayHitEffect()
    {
        if (hitRoutine != null)
            StopCoroutine(hitRoutine);

        hitRoutine = StartCoroutine(HitScaleRoutine());
        count++;
        countText.text = count.ToString();
    }

    IEnumerator HitScaleRoutine()
    {
        var t = 0f;
        var start = baseScale;
        var target = baseScale * hitScaleMultiplier;

        while (t < growDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / growDuration);
            transform.localScale = Vector3.Lerp(start, target, u);
            yield return null;
        }

        t = 0f;
        while (t < shrinkDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / shrinkDuration);
            transform.localScale = Vector3.Lerp(target, baseScale, u);
            yield return null;
        }

        transform.localScale = baseScale;
        hitRoutine = null;
    }
}