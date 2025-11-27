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
    [SerializeField] TMP_Text countText;
    int count;

    [SerializeField] SpriteRenderer pinSprite;

    bool initialized;
    Vector3 baseScale;
    Coroutine hitRoutine;

    int rowIndex = -1;
    int columnIndex = -1;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    public void Initialize(string pinId, int row, int column)
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

        Instance = new PinInstance(dto, row, column);
        pinSprite.sprite = SpriteCache.GetPinSprite(Instance.Id);

        rowIndex = row;
        columnIndex = column;

        if (PinManager.Instance != null)
            PinManager.Instance.RegisterPin(this, rowIndex, columnIndex);

        initialized = true;
    }

    public void PlayHitEffect()
    {
        if (hitRoutine != null)
            StopCoroutine(hitRoutine);

        hitRoutine = StartCoroutine(HitScaleRoutine());
        count++;
        if (countText != null)
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

    void OnDisable()
    {
        if (!initialized)
            return;

        if (PinManager.Instance != null && rowIndex >= 0 && columnIndex >= 0)
            PinManager.Instance.UnregisterPin(this, rowIndex, columnIndex);
    }
}
