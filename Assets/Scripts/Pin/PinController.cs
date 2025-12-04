using System.Collections;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(Collider2D))]
public sealed class PinController : MonoBehaviour
{
    public PinInstance Instance { get; private set; }

    [Header("Hit Scale Effect")] [SerializeField]
    float hitScaleMultiplier = 1.2f;

    [SerializeField] float growDuration = 0.06f;
    [SerializeField] float shrinkDuration = 0.08f;

    [SerializeField] SpriteRenderer pinSprite;
    [SerializeField] TMP_Text remainingHitsText;
    [SerializeField] TMP_Text hitCountText;

    bool initialized;
    Vector3 baseScale;
    Coroutine hitRoutine;

    int rowIndex = -1;
    int columnIndex = -1;

    public int RowIndex => rowIndex;
    public int ColumnIndex => columnIndex;

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
        if (pinSprite != null)
            pinSprite.sprite = SpriteCache.GetPinSprite(Instance.Id);

        rowIndex = row;
        columnIndex = column;

        if (PinManager.Instance != null)
            PinManager.Instance.RegisterPin(this, rowIndex, columnIndex);

        AttachEvents();

        initialized = true;
        Instance.ResetData();
    }

    void OnDisable()
    {
        if (!initialized)
            return;

        if (PinManager.Instance != null && rowIndex >= 0 && columnIndex >= 0)
            PinManager.Instance.UnregisterPin(this, rowIndex, columnIndex);

        DetachEvents();
    }

    void AttachEvents()
    {
        if (Instance == null)
        {
            Debug.LogError($"[PinController] Failed to AttachEvents {name}.");
            return;
        }

        Instance.OnRemainingHitsChanged += UpdateRemainingHits;
        Instance.OnHitCountChanged += HandleHitCountChanged;
    }

    void DetachEvents()
    {
        if (Instance == null)
        {
            Debug.LogError($"[PinController] Failed to DetachEvents {name}.");
            return;
        }

        Instance.OnRemainingHitsChanged -= UpdateRemainingHits;
        Instance.OnHitCountChanged -= HandleHitCountChanged;
    }

    void HandleHitCountChanged(int hitCount)
    {
        hitCountText.text = hitCount.ToString();
    }
    
    // TODO - 음.. 이게 remainingHits만을 위한 UI는 아니게 될 예정
    // 예를 들어 N턴후 효과가 발동되는 녀석이라던지? 그런 타이머로도 활용 가능해야함
    void UpdateRemainingHits(int remainingHits)
    {
        if (remainingHitsText == null)
            return;

        if (remainingHits == -1)
        {
            remainingHitsText.text = "";
            return;
        }

        remainingHitsText.text = remainingHits.ToString();
    }

    public void PlayHitEffect()
    {
        if (hitRoutine != null)
            StopCoroutine(hitRoutine);

        hitRoutine = StartCoroutine(HitScaleRoutine());
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

    // PinManager에서만 호출하는 용도
    public void SetGridIndices(int row, int column)
    {
        rowIndex = row;
        columnIndex = column;
        Instance?.SetGridPosition(row, column);
    }
}