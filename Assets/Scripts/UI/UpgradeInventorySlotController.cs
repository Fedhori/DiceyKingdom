using Data;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class UpgradeInventorySlotController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public int Index { get; private set; } = -1;
    public UpgradeInstance Upgrade { get; private set; }

    [SerializeField] ItemView itemView;
    [SerializeField] ItemTooltipTarget tooltipTarget;
    bool isDragging;

    void Awake()
    {
        if (itemView == null)
            itemView = GetComponentInChildren<ItemView>(true);
        if (tooltipTarget == null)
            tooltipTarget = GetComponentInChildren<ItemTooltipTarget>(true);
    }

    public void SetIndex(int index)
    {
        Index = index;
    }

    public void Bind(UpgradeInstance upgrade)
    {
        Upgrade = upgrade;
        UpdateView();
    }

    void UpdateView()
    {
        if (itemView != null)
        {
            itemView.SetIcon(SpriteCache.GetUpgradeSprite(Upgrade?.Id));
            itemView.SetRarity(Upgrade != null ? Upgrade.Rarity : ItemRarity.Common);
        }

        if (tooltipTarget != null)
        {
            if (Upgrade != null)
                tooltipTarget.BindUpgrade(Upgrade, ItemTooltipTarget.TooltipActionKind.SellUpgrade, Upgrade);
            else
                tooltipTarget.Clear();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (StageManager.Instance == null || StageManager.Instance.CurrentPhase != StagePhase.Shop)
            return;

        var manager = UpgradeInventoryManager.Instance;
        if (manager == null)
            return;

        if (Upgrade == null)
            return;

        bool wasSelected = manager.SelectedIndex == Index;
        UiSelectionEvents.RaiseSelectionCleared();

        if (wasSelected)
            return;

        manager.SelectAt(Index);
        tooltipTarget?.Pin();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (Upgrade == null)
            return;

        if (StageManager.Instance == null || StageManager.Instance.CurrentPhase != StagePhase.Shop)
            return;

        var inventoryManager = UpgradeInventoryManager.Instance;
        if (inventoryManager != null)
            inventoryManager.SelectAt(Index);

        TooltipManager.Instance?.HideForDrag();

        isDragging = true;
        GhostManager.Instance?.ShowGhost(
            SpriteCache.GetUpgradeSprite(Upgrade.Id),
            eventData.position,
            GhostKind.Upgrade,
            Upgrade.Rarity);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        GhostManager.Instance?.UpdateGhostPosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

        isDragging = false;

        var slotManager = ItemSlotManager.Instance;
        if (slotManager != null && Upgrade != null
            && slotManager.TryGetUpgradeSlotFromScreenPos(eventData.position, Upgrade, out int slotIndex))
        {
            UpgradeInventoryManager.Instance?.TryApplySelectedUpgradeAt(slotIndex);
        }

        GhostManager.Instance?.HideGhost(GhostKind.Upgrade);
        TooltipManager.Instance?.RestoreAfterDrag();
    }
}
