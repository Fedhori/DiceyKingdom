using System;
using System.Collections.Generic;
using Data;
using UnityEngine;

public sealed class ItemSlotManager : MonoBehaviour
{
    public static ItemSlotManager Instance { get; private set; }

    [SerializeField] Transform slotContainer;
    [SerializeField] ItemSlotController slotPrefab;
    [SerializeField] SlidePanelLean slotPanelSlide;
    ItemSlotController[] slotControllers;

    ItemSlotController draggingController;
    int draggingStartIndex = -1;
    int currentHighlightIndex = -1;
    int selectedSlotIndex = -1;
    ItemInstance selectedItem;
    bool overSellArea;
    Sprite draggingGhostSprite;
    ItemRarity draggingGhostRarity = ItemRarity.Common;
    GhostKind draggingGhostKind = GhostKind.None;
    bool ghostHidden;

    [SerializeField] Color slotHighlightColor = Color.white;
    ItemInventory inventory;
    StageManager subscribedStageManager;
    UpgradeInventoryManager subscribedUpgradeInventory;
    bool upgradeInventoryHighlightActive;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildSlots();

        if (slotPanelSlide != null)
            slotPanelSlide.HideImmediate();
        else if (slotContainer != null)
            slotContainer.gameObject.SetActive(false);
    }

    void Start()
    {
        TryBindInventory();
        RefreshFromInventory();
        SubscribeStageManager();
        ForceHideSlotPanelImmediate();
        RefreshSlotContainerVisibility();
        UiSelectionEvents.OnSelectionCleared += HandleSelectionCleared;
        TryBindUpgradeInventory();
    }

    void OnDisable()
    {
        UnbindInventory();
        UnbindUpgradeInventory();
        UiSelectionEvents.OnSelectionCleared -= HandleSelectionCleared;
    }

    void OnDestroy()
    {
        UnsubscribeStageManager();
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

    void SubscribeStageManager()
    {
        if (subscribedStageManager != null)
            return;

        var stageManager = StageManager.Instance;
        if (stageManager == null)
            return;

        subscribedStageManager = stageManager;
        subscribedStageManager.OnPhaseChanged += HandlePhaseChanged;
    }

    void UnsubscribeStageManager()
    {
        if (subscribedStageManager == null)
            return;

        subscribedStageManager.OnPhaseChanged -= HandlePhaseChanged;
        subscribedStageManager = null;
    }

    void TryBindUpgradeInventory()
    {
        if (subscribedUpgradeInventory != null)
            return;

        var manager = UpgradeInventoryManager.Instance;
        if (manager == null)
            return;

        subscribedUpgradeInventory = manager;
        subscribedUpgradeInventory.OnSelectionChanged += HandleUpgradeInventorySelectionChanged;
    }

    void UnbindUpgradeInventory()
    {
        if (subscribedUpgradeInventory == null)
            return;

        subscribedUpgradeInventory.OnSelectionChanged -= HandleUpgradeInventorySelectionChanged;
        subscribedUpgradeInventory = null;
    }

    void HandlePhaseChanged(StagePhase phase)
    {
        SetSlotContainerVisible(phase == StagePhase.Shop);
    }

    void RefreshSlotContainerVisibility()
    {
        var stageManager = StageManager.Instance;
        if (stageManager == null)
            return;

        SetSlotContainerVisible(stageManager.CurrentPhase == StagePhase.Shop);
    }

    void SetSlotContainerVisible(bool visible)
    {
        if (slotContainer == null)
            return;

        if (slotPanelSlide == null)
        {
            slotContainer.gameObject.SetActive(visible);
            return;
        }

        if (visible)
        {
            if (!slotContainer.gameObject.activeSelf)
                slotContainer.gameObject.SetActive(true);

            slotPanelSlide.Show();
        }
        else
        {
            if (!slotContainer.gameObject.activeSelf)
                return;

            slotPanelSlide.Hide(() => slotContainer.gameObject.SetActive(false));
        }
    }

    void HandleSlotChanged(int slotIndex, ItemInstance previous, ItemInstance current, ItemInventory.SlotChangeType changeType)
    {
        _ = changeType;

        if (!IsValidIndex(slotIndex))
            return;

        var ctrl = slotControllers[slotIndex];
        if (ctrl == null)
            return;

        ctrl.Bind(current);

        if (slotIndex == selectedSlotIndex && previous != current)
            UiSelectionEvents.RaiseSelectionCleared();
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

    void ForceHideSlotPanelImmediate()
    {
        if (slotPanelSlide != null)
            slotPanelSlide.HideImmediate();

        if (slotContainer != null)
            slotContainer.gameObject.SetActive(false);
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

        RefreshSelectionVisuals();
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

    public int SelectedSlotIndex => selectedSlotIndex;
    public ItemInstance SelectedItem => selectedItem;

    public event Action<ItemInstance> OnSelectedItemChanged;

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
            if (ctrl == null)
                continue;

            var rt = ctrl.DropArea != null ? ctrl.DropArea : ctrl.RectTransform;
            if (rt == null)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos))
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

    public bool UpdatePurchaseHover(Vector2 screenPos)
    {
        return UpdatePurchaseHover(screenPos, null);
    }

    public bool UpdatePurchaseHover(Vector2 screenPos, ItemInstance previewInstance)
    {
        if (!TryGetEmptySlotFromScreenPos(screenPos, out int slotIndex))
        {
            ClearPreviews();
            return false;
        }

        ClearPreviews();

        if (IsValidIndex(slotIndex))
            slotControllers[slotIndex]?.SetPreview(previewInstance);

        return true;
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

    public void HighlightUpgradeSlots(UpgradeInstance upgrade)
    {
        if (slotControllers == null)
            return;

        for (int i = 0; i < slotControllers.Length; i++)
        {
            var ctrl = slotControllers[i];
            if (ctrl == null)
                continue;

            var inst = inventory != null ? inventory.GetSlot(i) : null;
            bool highlight = inst != null && upgrade != null && upgrade.IsApplicable(inst);
            ctrl.SetHighlight(highlight, slotHighlightColor);
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

    public bool HasApplicableUpgradeSlot(UpgradeInstance upgrade)
    {
        if (upgrade == null || inventory == null)
            return false;

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            var inst = inventory.GetSlot(i);
            if (inst == null)
                continue;

            if (upgrade.IsApplicable(inst))
                return true;
        }

        return false;
    }

    public bool TryGetUpgradeSlotFromScreenPos(Vector2 screenPos, UpgradeInstance upgrade, out int slotIndex)
    {
        slotIndex = -1;
        if (upgrade == null)
            return false;

        if (!TryGetSlotFromScreenPos(screenPos, out slotIndex))
            return false;

        if (inventory == null)
            return false;

        var inst = inventory.GetSlot(slotIndex);
        if (inst == null || !upgrade.IsApplicable(inst))
        {
            slotIndex = -1;
            return false;
        }

        return true;
    }

    public bool UpdateUpgradeHover(Vector2 screenPos, UpgradeInstance upgrade)
    {
        if (upgrade == null)
            return false;

        return TryGetUpgradeSlotFromScreenPos(screenPos, upgrade, out _);
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

        TooltipManager.Instance?.HideForDrag();

        draggingController = controller;
        draggingStartIndex = idx;
        var rarity = ItemRarity.Common;
        if (ItemRepository.TryGet(controller.Instance.Id, out var dto) && dto != null)
            rarity = dto.rarity;
        draggingGhostSprite = controller.GetIconSprite();
        draggingGhostRarity = rarity;
        draggingGhostKind = GhostKind.Item;
        ghostHidden = false;
        GhostManager.Instance?.ShowGhost(draggingGhostSprite, screenPos, draggingGhostKind, draggingGhostRarity);
        controller.SetPreview(null);
        overSellArea = false;
        SellOverlayController.Instance?.Show();
    }

    public void EndDrag(ItemSlotController controller, Vector2 screenPos)
    {
        if (draggingController == null || controller != draggingController)
        {
            TooltipManager.Instance?.RestoreAfterDrag();
            SellOverlayController.Instance?.Hide();
            ResetDrag();
            return;
        }

        UiSelectionEvents.RaiseSelectionCleared();

        var overlay = SellOverlayController.Instance;
        overSellArea = overlay != null && overlay.ContainsScreenPoint(screenPos);
        if (overSellArea)
        {
            GhostManager.Instance?.HideGhost(GhostKind.Item);
            ClearHighlights();
            ClearPreviews();
            overlay?.Hide();

            var toSell = draggingController;
            ResetDrag();
            RequestSellItem(toSell);
            TooltipManager.Instance?.RestoreAfterDrag();
            return;
        }

        int targetIndex = FindSlotIndexAtScreenPos(screenPos);
        if (targetIndex >= 0 && targetIndex != draggingStartIndex)
            SwapControllers(draggingStartIndex, targetIndex);

        GhostManager.Instance?.HideGhost(GhostKind.Item);
        ClearHighlights();
        ClearPreviews();

        ResetDrag();
        overlay?.Hide();
        TooltipManager.Instance?.RestoreAfterDrag();
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

            var rt = ctrl.DropArea != null ? ctrl.DropArea : ctrl.RectTransform;
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
        UpdateDragPreview(targetIndex);
        UpdateGhostVisibility(targetIndex, screenPos);

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
        draggingGhostSprite = null;
        draggingGhostRarity = ItemRarity.Common;
        draggingGhostKind = GhostKind.None;
        ghostHidden = false;
    }

    void UpdateGhostVisibility(int targetIndex, Vector2 screenPos)
    {
        if (draggingGhostKind == GhostKind.None)
            return;

        bool validTarget = IsValidIndex(targetIndex) && targetIndex != draggingStartIndex;
        if (validTarget)
        {
            if (!ghostHidden)
            {
                GhostManager.Instance?.HideGhost(draggingGhostKind);
                ghostHidden = true;
            }

            return;
        }

        if (ghostHidden)
        {
            GhostManager.Instance?.ShowGhost(draggingGhostSprite, screenPos, draggingGhostKind, draggingGhostRarity);
            ghostHidden = false;
        }
    }

    void UpdateDragPreview(int targetIndex)
    {
        if (draggingController == null || slotControllers == null)
            return;

        ClearPreviews();

        if (IsValidIndex(draggingStartIndex))
            slotControllers[draggingStartIndex]?.SetPreview(null);

        if (!IsValidIndex(targetIndex) || targetIndex == draggingStartIndex)
            return;

        var targetCtrl = slotControllers[targetIndex];
        if (targetCtrl == null)
            return;

        targetCtrl.SetPreview(draggingController.Instance);

        if (IsValidIndex(draggingStartIndex))
        {
            var startCtrl = slotControllers[draggingStartIndex];
            if (startCtrl != null)
            {
                if (targetCtrl.Instance != null)
                    startCtrl.SetPreview(targetCtrl.Instance);
                else
                    startCtrl.SetPreview(null);
            }
        }
    }

    public void ClearPreviews()
    {
        if (slotControllers == null)
            return;

        for (int i = 0; i < slotControllers.Length; i++)
            slotControllers[i]?.ClearPreview();
    }

    public void ToggleSlotSelection(ItemSlotController controller)
    {
        if (controller == null || controller.Instance == null)
            return;

        if (selectedSlotIndex == controller.SlotIndex)
        {
            UiSelectionEvents.RaiseSelectionCleared();
            return;
        }

        UiSelectionEvents.RaiseSelectionCleared();

        selectedSlotIndex = controller.SlotIndex;
        selectedItem = controller.Instance;
        RefreshSelectionVisuals();
        OnSelectedItemChanged?.Invoke(selectedItem);
        controller.PinTooltip();
    }

    void RefreshSelectionVisuals()
    {
        if (slotControllers == null)
            return;

        for (int i = 0; i < slotControllers.Length; i++)
        {
            var ctrl = slotControllers[i];
            if (ctrl == null)
                continue;

            ctrl.SetSelected(i == selectedSlotIndex);
        }
    }

    void ClearSlotSelection()
    {
        if (selectedSlotIndex < 0)
            return;

        selectedSlotIndex = -1;
        selectedItem = null;
        RefreshSelectionVisuals();
        OnSelectedItemChanged?.Invoke(null);
    }

    void HandleSelectionCleared()
    {
        ClearSlotSelection();
    }

    void HandleUpgradeInventorySelectionChanged()
    {
        if (subscribedUpgradeInventory == null)
            return;

        var upgrade = subscribedUpgradeInventory.SelectedUpgrade;
        if (upgrade != null)
        {
            HighlightUpgradeSlots(upgrade);
            upgradeInventoryHighlightActive = true;
            return;
        }

        if (upgradeInventoryHighlightActive)
        {
            ClearHighlights();
            upgradeInventoryHighlightActive = false;
        }
    }

    private void RequestSellItem(ItemSlotController ctrl)
    {
        if (ctrl == null || ctrl.Instance == null)
            return;

        if (!ItemRepository.TryGet(ctrl.Instance.Id, out var dto) || dto == null)
            return;

        int basePrice = ShopManager.CalculateSellPrice(dto.price);
        int price = basePrice + ctrl.Instance.SellValueBonus;
        if (price < 0)
            return;

        var args = new Dictionary<string, object>
        {
            ["pinName"] = LocalizationUtil.GetItemName(ctrl.Instance.Id),
            ["value"] = price
        };

        ModalManager.Instance.ShowConfirmation(
            "modal",
            "modal.sellitem.title",
            "modal",
            "modal.sellitem.message",
            () => SellItem(ctrl, price),
            () => { },
            args);
    }

    void SellItem(ItemSlotController ctrl, int price)
    {
        if (ctrl == null || ctrl.Instance == null)
            return;

        TryStoreUpgrade(ctrl.Instance);
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

    void TryStoreUpgrade(ItemInstance item)
    {
        if (item == null)
            return;

        var upgrades = item.Upgrades;
        if (upgrades == null || upgrades.Count == 0)
            return;

        var upgradeManager = UpgradeManager.Instance;
        if (upgradeManager == null)
            return;

        var inventoryManager = UpgradeInventoryManager.Instance;
        if (inventoryManager == null)
            return;

        for (int i = 0; i < upgrades.Count; i++)
        {
            var upgrade = upgrades[i];
            if (upgrade != null)
                inventoryManager.Add(upgrade);
        }

        upgradeManager.RemoveUpgrade(item);
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
