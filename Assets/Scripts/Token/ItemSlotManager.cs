using System.Collections.Generic;
using Data;
using UnityEngine;

public sealed class ItemSlotManager : MonoBehaviour
{
    public static ItemSlotManager Instance { get; private set; }

    [SerializeField] Transform slotContainer;
    [SerializeField] ItemSlotController slotPrefab;
    ItemSlotController[] slotControllers;

    ItemSlotController draggingController;
    int draggingStartIndex = -1;
    int currentHighlightIndex = -1;
    bool overSellArea;

    [SerializeField] Color slotHighlightColor = Color.white;
    ItemInventory inventory;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildSlots();
    }

    void OnEnable()
    {
        TryBindInventory();
        RefreshFromInventory();
    }

    void Start()
    {
        TryBindInventory();
        RefreshFromInventory();
    }

    void OnDisable()
    {
        UnbindInventory();
    }

    void TryBindInventory()
    {
        if (inventory != null)
            return;

        var manager = ItemManager.Instance;
        if (manager == null)
            return;

        inventory = manager.Inventory;
        if (inventory == null)
            return;

        inventory.OnSlotChanged += HandleSlotChanged;
    }

    void UnbindInventory()
    {
        if (inventory == null)
            return;

        inventory.OnSlotChanged -= HandleSlotChanged;
        inventory = null;
    }

    void HandleSlotChanged(int slotIndex, ItemInstance previous, ItemInstance current)
    {
        _ = previous;

        if (!IsValidIndex(slotIndex))
            return;

        var ctrl = slotControllers[slotIndex];
        if (ctrl == null)
            return;

        ctrl.Bind(current);
    }

    void BuildSlots()
    {
        if (slotContainer == null || slotPrefab == null)
        {
            Debug.LogError("[ItemSlotManager] slotContainer or slotPrefab is not assigned.");
            slotControllers = System.Array.Empty<ItemSlotController>();
            return;
        }

        // 기존 자식 슬롯 정리
        for (int i = slotContainer.childCount - 1; i >= 0; i--)
        {
            var child = slotContainer.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }

        int slotCount = Mathf.Max(0, GameConfig.ItemSlotCount);
        slotControllers = new ItemSlotController[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            var ctrl = Instantiate(slotPrefab, slotContainer);
            ctrl.SetSlotIndex(i);
            slotControllers[i] = ctrl;
        }

    }

    void RefreshFromInventory()
    {
        if (inventory == null || slotControllers == null)
            return;

        int count = Mathf.Min(slotControllers.Length, inventory.SlotCount);
        for (int i = 0; i < count; i++)
        {
            var ctrl = slotControllers[i];
            if (ctrl == null)
                continue;

            ctrl.Bind(inventory.GetSlot(i));
        }

        for (int i = count; i < slotControllers.Length; i++)
            slotControllers[i]?.Bind(null);
    }

    public bool TryAddItemAt(string itemId, int slotIndex, out ItemInstance instance)
    {
        instance = null;

        if (!IsValidIndex(slotIndex))
        {
            Debug.LogWarning($"[ItemSlotManager] Invalid slot index: {slotIndex}");
            return false;
        }

        if (inventory == null)
        {
            Debug.LogWarning("[ItemSlotManager] ItemInventory not bound.");
            return false;
        }

        if (!ItemRepository.IsInitialized)
        {
            Debug.LogWarning("[ItemSlotManager] ItemRepository not initialized.");
            return false;
        }

        if (!ItemRepository.TryGet(itemId, out var dto) || dto == null)
        {
            Debug.LogWarning($"[ItemSlotManager] Item id not found: {itemId}");
            return false;
        }

        if (!inventory.IsSlotEmpty(slotIndex))
        {
            Debug.LogWarning($"[ItemSlotManager] Slot {slotIndex} is not empty.");
            return false;
        }

        instance = new ItemInstance(dto);
        if (!inventory.TrySetSlot(slotIndex, instance))
            return false;

        ClearHighlights();
        return true;
    }

    public int SlotCount => inventory != null ? inventory.SlotCount : (slotControllers != null ? slotControllers.Length : 0);

    public bool TryGetFirstEmptySlot(out int index)
    {
        index = -1;
        if (inventory == null)
            return false;

        return inventory.TryGetFirstEmptySlot(out index);
    }

    public bool IsSlotEmpty(int slotIndex)
    {
        if (!IsValidIndex(slotIndex))
            return false;

        return inventory != null && inventory.IsSlotEmpty(slotIndex);
    }
    
    public bool TryGetSlotFromScreenPos(Vector2 screenPos, out int slotIndex)
    {
        slotIndex = -1;
        if (slotControllers == null)
            return false;

        for (int i = 0; i < slotControllers.Length; i++)
        {
            var ctrl = slotControllers[i];
            if (ctrl == null || ctrl.RectTransform == null)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(ctrl.RectTransform, screenPos))
            {
                slotIndex = i;
                return true;
            }
        }

        return false;
    }

    public bool TryGetEmptySlotFromScreenPos(Vector2 screenPos, out int slotIndex)
    {
        slotIndex = -1;

        if (!TryGetSlotFromScreenPos(screenPos, out slotIndex))
            return false;

        if (inventory == null || !inventory.IsSlotEmpty(slotIndex))
        {
            slotIndex = -1;
            return false;
        }

        return true;
    }

    public void UpdatePurchaseHover(Vector2 screenPos)
    {
        if (!TryGetEmptySlotFromScreenPos(screenPos, out int slotIndex))
        {
            ClearHighlight();
            return;
        }

        UpdateHighlight(slotIndex);
    }

    public void HighlightEmptySlots()
    {
        if (slotControllers == null)
            return;

        for (int i = 0; i < slotControllers.Length; i++)
        {
            var ctrl = slotControllers[i];
            if (ctrl == null)
                continue;

            bool empty = inventory != null && inventory.IsSlotEmpty(i);
            ctrl.SetHighlight(empty, slotHighlightColor);
        }
    }

    public void ClearHighlights()
    {
        if (slotControllers == null)
            return;

        for (int i = 0; i < slotControllers.Length; i++)
        {
            var ctrl = slotControllers[i];
            if (ctrl == null)
                continue;

            ctrl.SetHighlight(false, slotHighlightColor);
        }

        currentHighlightIndex = -1;
    }

    public void BeginDrag(ItemSlotController controller, Vector2 screenPos)
    {
        if (controller == null)
            return;

        if (!StageManager.Instance.CanDragItems)
            return;

        int idx = controller.SlotIndex;
        if (controller.Instance == null)
            return;

        draggingController = controller;
        draggingStartIndex = idx;
        GhostManager.Instance?.ShowGhost(controller.GetIconSprite(), screenPos, GhostKind.Item);
        controller.SetIconVisible(false);
        overSellArea = false;
        SellOverlayController.Instance?.Show();
    }

    public void EndDrag(ItemSlotController controller, Vector2 screenPos)
    {
        if (draggingController == null || controller != draggingController)
        {
            SellOverlayController.Instance?.Hide();
            ResetDrag();
            return;
        }

        var overlay = SellOverlayController.Instance;
        overSellArea = overlay != null && overlay.ContainsScreenPoint(screenPos);
        if (overSellArea)
        {
            GhostManager.Instance?.HideGhost(GhostKind.Item);
            ClearHighlights();
            draggingController?.SetIconVisible(true);
            overlay?.Hide();

            var toSell = draggingController;
            ResetDrag();
            RequestSellItem(toSell);
            return;
        }

        int targetIndex = FindSlotIndexAtScreenPos(screenPos);
        if (targetIndex >= 0 && targetIndex != draggingStartIndex)
            SwapControllers(draggingStartIndex, targetIndex);

        GhostManager.Instance?.HideGhost(GhostKind.Item);
        ClearHighlights();
        draggingController?.SetIconVisible(true);

        ResetDrag();
        overlay?.Hide();
    }

    int FindSlotIndexAtScreenPos(Vector2 screenPos)
    {
        if (slotControllers == null || slotControllers.Length == 0)
            return -1;

        for (int i = 0; i < slotControllers.Length; i++)
        {
            var ctrl = slotControllers[i];
            if (ctrl == null)
                continue;

            var rt = ctrl.RectTransform;
            if (rt == null)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
                return i;
        }

        return -1;
    }

    public void UpdateDrag(ItemSlotController controller, Vector2 screenPos)
    {
        if (draggingController == null || controller != draggingController)
            return;

        GhostManager.Instance?.UpdateGhostPosition(screenPos);

        int targetIndex = FindSlotIndexAtScreenPos(screenPos);
        UpdateHighlight(targetIndex);

        var overlay = SellOverlayController.Instance;
        overSellArea = overlay != null && overlay.ContainsScreenPoint(screenPos);
    }

    void SwapControllers(int indexA, int indexB)
    {
        if (inventory == null)
            return;

        if (!IsValidIndex(indexA) || !IsValidIndex(indexB) || indexA == indexB)
            return;

        if (!inventory.TrySwap(indexA, indexB))
            return;

        RefreshFromInventory();
    }

    void ResetDrag()
    {
        draggingController = null;
        draggingStartIndex = -1;
        overSellArea = false;
    }

    private void RequestSellItem(ItemSlotController ctrl)
    {
        if (ctrl == null || ctrl.Instance == null)
            return;

        if (!ItemRepository.TryGet(ctrl.Instance.Id, out var dto) || dto == null)
            return;

        int price = ShopManager.CalculateSellPrice(dto.price);
        if (price < 0)
            return;

        var args = new Dictionary<string, object>
        {
            ["pinName"] = LocalizationUtil.GetItemName(ctrl.Instance.Id),
            ["value"] = price
        };

        ModalManager.Instance.ShowConfirmation(
            "modal",
            "modal.sellpin.title",
            "modal",
            "modal.sellpin.message",
            () => SellItem(ctrl, price),
            () => { },
            args);
    }

    void SellItem(ItemSlotController ctrl, int price)
    {
        if (ctrl == null || ctrl.Instance == null)
            return;

        CurrencyManager.Instance?.AddCurrency(price);
        AudioManager.Instance?.Play("Buy");
        RemoveItem(ctrl);
    }

    void RemoveItem(ItemSlotController ctrl)
    {
        if (ctrl == null)
            return;

        if (inventory == null)
            return;

        inventory.TryRemoveAt(ctrl.SlotIndex, out _);
    }

    bool IsValidIndex(int index)
    {
        return slotControllers != null && index >= 0 && index < slotControllers.Length;
    }

    void UpdateHighlight(int targetIndex)
    {
        if (currentHighlightIndex == targetIndex)
            return;

        ClearHighlight();

        if (!IsValidIndex(targetIndex))
            return;

        var ctrl = slotControllers[targetIndex];
        if (ctrl == null)
            return;

        ctrl.SetHighlight(true, slotHighlightColor);
        currentHighlightIndex = targetIndex;
    }

    void ClearHighlight()
    {
        if (IsValidIndex(currentHighlightIndex))
        {
            var ctrl = slotControllers[currentHighlightIndex];
            if (ctrl != null)
            {
                ctrl.SetHighlight(false, slotHighlightColor);
            }
        }

        currentHighlightIndex = -1;
    }
}
