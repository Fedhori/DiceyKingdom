using UnityEngine;

public sealed class UpgradePanelPresenter : MonoBehaviour
{
    [SerializeField] UpgradePanelView panelView;

    ItemSlotManager subscribedSlotManager;
    UpgradeManager subscribedUpgradeManager;
    ItemInstance currentItem;
    UpgradeReplaceRequest pendingReplace;

    void Awake()
    {
        if (panelView == null)
            panelView = GetComponentInChildren<UpgradePanelView>(true);
    }

    void OnEnable()
    {
        UpgradePanelEvents.OnToggleRequested += HandleToggleRequested;
        UiSelectionEvents.OnSelectionCleared += HandleSelectionCleared;
    }

    void Start()
    {
        BindSlotManager();
        BindUpgradeManager();
    }

    void OnDisable()
    {
        UpgradePanelEvents.OnToggleRequested -= HandleToggleRequested;
        UiSelectionEvents.OnSelectionCleared -= HandleSelectionCleared;
        UnbindUpgradeManager();
        UnbindSlotManager();
    }

    void HandleToggleRequested(ItemInstance item)
    {
        if (panelView == null || item == null || item.Upgrades.Count == 0)
            return;

        if (pendingReplace != null)
        {
            ClosePanel();
            return;
        }

        if (panelView.IsOpen && ReferenceEquals(currentItem, item))
        {
            ClosePanel();
            return;
        }

        panelView.Open();
        panelView.SetReplaceMode(false, null);
        panelView.SetUpgrades(item.Upgrades);
        currentItem = item;
    }

    void HandleReplaceRequested(UpgradeReplaceRequest request)
    {
        if (panelView == null || request == null || request.TargetItem == null)
            return;

        pendingReplace = request;
        currentItem = request.TargetItem;
        panelView.Open();
        panelView.SetReplaceMode(true, HandleReplaceClicked);
        panelView.SetUpgrades(request.TargetItem.Upgrades, request.PendingUpgrade);
    }

    void HandleSelectionCleared()
    {
        ClosePanel();
    }

    void HandleSelectedItemChanged(ItemInstance selected)
    {
        if (panelView == null || !panelView.IsOpen)
            return;

        if (!ReferenceEquals(selected, currentItem))
            ClosePanel();
    }

    void ClosePanel()
    {
        if (panelView != null && panelView.IsOpen)
            panelView.Close();

        currentItem = null;
        if (pendingReplace != null)
            UpgradeManager.Instance?.CancelReplace();
        pendingReplace = null;
    }

    void BindSlotManager()
    {
        if (subscribedSlotManager != null)
            return;

        var manager = ItemSlotManager.Instance;
        if (manager == null)
            return;

        subscribedSlotManager = manager;
        subscribedSlotManager.OnSelectedItemChanged += HandleSelectedItemChanged;
    }

    void UnbindSlotManager()
    {
        if (subscribedSlotManager == null)
            return;

        subscribedSlotManager.OnSelectedItemChanged -= HandleSelectedItemChanged;
        subscribedSlotManager = null;
    }

    void BindUpgradeManager()
    {
        if (subscribedUpgradeManager != null)
            return;

        var manager = UpgradeManager.Instance;
        if (manager == null)
            return;

        subscribedUpgradeManager = manager;
        subscribedUpgradeManager.OnReplaceRequested += HandleReplaceRequested;
    }

    void UnbindUpgradeManager()
    {
        if (subscribedUpgradeManager == null)
            return;

        subscribedUpgradeManager.OnReplaceRequested -= HandleReplaceRequested;
        subscribedUpgradeManager = null;
    }

    void HandleReplaceClicked(UpgradeInstance existingUpgrade)
    {
        if (pendingReplace == null || existingUpgrade == null)
            return;

        bool success = UpgradeManager.Instance != null
            && UpgradeManager.Instance.TryConfirmReplace(existingUpgrade);

        if (success)
            ClosePanel();
    }
}
