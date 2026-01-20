using UnityEngine;

public sealed class UpgradePanelPresenter : MonoBehaviour
{
    [SerializeField] UpgradePanelView panelView;

    ItemSlotManager subscribedSlotManager;
    ItemInstance currentItem;

    void Awake()
    {
        if (panelView == null)
            panelView = GetComponentInChildren<UpgradePanelView>(true);
    }

    void OnEnable()
    {
        UpgradePanelEvents.OnToggleRequested += HandleToggleRequested;
        UiSelectionEvents.OnSelectionCleared += HandleSelectionCleared;
        BindSlotManager();
    }

    void Start()
    {
        BindSlotManager();
    }

    void OnDisable()
    {
        UpgradePanelEvents.OnToggleRequested -= HandleToggleRequested;
        UiSelectionEvents.OnSelectionCleared -= HandleSelectionCleared;
        UnbindSlotManager();
    }

    void HandleToggleRequested(ItemInstance item)
    {
        if (panelView == null || item == null || item.Upgrades.Count == 0)
            return;

        if (panelView.IsOpen && ReferenceEquals(currentItem, item))
        {
            ClosePanel();
            return;
        }

        panelView.Open(item.Upgrades);
        currentItem = item;
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
}
