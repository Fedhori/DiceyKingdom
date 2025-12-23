using System.Collections;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;

[RequireComponent(typeof(Collider2D))]
public sealed class PinController : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public PinInstance Instance { get; private set; }

    [Header("Hit Scale Effect")] [SerializeField]
    float hitScaleMultiplier = 1.2f;

    [SerializeField] float growDuration = 0.06f;
    [SerializeField] float shrinkDuration = 0.08f;

    [SerializeField] SpriteRenderer pinSprite;
    [SerializeField] TMP_Text remainingHitsText;
    [SerializeField] TMP_Text hitCountText;
    public GameObject dragHighlightMask;

    bool initialized;
    bool eventsAttached;
    Vector3 baseScale;
    Coroutine hitRoutine;

    int rowIndex = -1;
    int columnIndex = -1;
    public int RowIndex => rowIndex;
    public int ColumnIndex => columnIndex;
    bool IsBasicPin => Instance != null && Instance.Id == GameConfig.BasicPinId;

    void Awake()
    {
        baseScale = transform.localScale;
    }

    public void Initialize(string pinId, int row, int column, int hitCount)
    {
        if (!initialized)
        {
            rowIndex = row;
            columnIndex = column;
            if (PinManager.Instance != null)
                PinManager.Instance.RegisterPin(this, rowIndex, columnIndex);
            initialized = true;
        }

        BindNewInstance(pinId, hitCount, row, column);
    }

    public void BindExistingInstance(PinInstance instance)
    {
        if (instance == null)
            return;

        DetachEvents();
        Instance = instance;
        Instance.SetGridPosition(rowIndex, columnIndex);
        UpdateSprite();
        AttachEvents();
    }

    void OnDisable()
    {
        if (!initialized)
            return;

        if (PinManager.Instance != null && rowIndex >= 0 && columnIndex >= 0)
            PinManager.Instance.UnregisterPin(this, rowIndex, columnIndex);

        DetachEvents();

        PinDragManager.Instance?.CancelDragFromPin(this);
    }

    void AttachEvents()
    {
        if (eventsAttached || Instance == null)
            return;

        Instance.OnRemainingHitsChanged += UpdateRemainingHits;
        Instance.OnHitCountChanged += HandleHitCountChanged;
        if (ShopManager.Instance != null)
            ShopManager.Instance.OnSelectionChanged += HandleSelectionChanged;
        eventsAttached = true;
    }

    void DetachEvents()
    {
        if (!eventsAttached || Instance == null)
            return;

        Instance.OnRemainingHitsChanged -= UpdateRemainingHits;
        Instance.OnHitCountChanged -= HandleHitCountChanged;
        if (ShopManager.Instance != null)
            ShopManager.Instance.OnSelectionChanged -= HandleSelectionChanged;
        eventsAttached = false;
    }

    void HandleHitCountChanged(int hitCount)
    {
        hitCountText.text = hitCount.ToString();
    }

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
        // 너무 시끄러워서 비활성화. 다른 방식으로 이펙트, 사운드를 채우는게 맞을듯?
        // AudioManager.Instance.Play("Score");
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (!IsBasicPin)
            return;

        var shop = ShopManager.Instance;
        if (shop == null || shop.CurrentSelectionIndex < 0)
            return;

        var flow = FlowManager.Instance;
        if (flow != null && flow.CurrentPhase != FlowPhase.Shop)
            return;

        shop.TryPurchaseSelectedAt(rowIndex, columnIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        var flow = FlowManager.Instance;
        if (flow != null && !flow.CanDragPins)
            return;

        PinDragManager.Instance?.BeginDrag(this, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        var dragMgr = PinDragManager.Instance;
        if (dragMgr == null || !dragMgr.IsDragging(this))
            return;

        dragMgr.UpdateDrag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        var dragMgr = PinDragManager.Instance;
        if (dragMgr == null || !dragMgr.IsDragging(this))
            return;

        dragMgr.EndDrag(eventData.position);
    }

    void HandleSelectionChanged(int selectedIndex)
    {
        bool shouldHighlight = false;
        if (IsBasicPin && selectedIndex >= 0)
        {
            var shop = ShopManager.Instance;
            var item = shop != null ? shop.GetSelectedItem() : null;
            shouldHighlight = item != null && item.ItemType == ShopItemType.Pin;
        }

        if (dragHighlightMask != null)
            dragHighlightMask.SetActive(false);
    }

    void BindNewInstance(string pinId, int hitCount, int row, int column)
    {
        if (!PinRepository.TryGet(pinId, out var dto))
        {
            Debug.LogError($"[PinController] Failed to initialize {pinId}");
            return;
        }

        DetachEvents();
        Instance = new PinInstance(dto, row, column);
        UpdateSprite();
        AttachEvents();
        Instance.ResetData(hitCount);
    }

    void UpdateSprite()
    {
        if (pinSprite != null && Instance != null)
            pinSprite.sprite = SpriteCache.GetPinSprite(Instance.Id);
    }
}
