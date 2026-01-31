using UnityEngine;

public sealed class UpgradeInventoryOpener : MonoBehaviour
{
    [SerializeField] private UpgradeInventoryView inventoryView;

    public void Open()
    {
        if (!CanOpen())
            return;

        UiSelectionEvents.RaiseSelectionCleared();
        TooltipManager.Instance?.ClearPin();
        inventoryView?.Open();
    }

    public void Close()
    {
        inventoryView?.Close();
    }

    public void Toggle()
    {
        if (inventoryView == null)
            return;

        if (inventoryView.gameObject.activeSelf)
            Close();
        else
            Open();
    }

    bool CanOpen()
    {
        var stageManager = StageManager.Instance;
        return stageManager != null && stageManager.CurrentPhase == StagePhase.Shop;
    }
}
