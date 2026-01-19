using UnityEngine;

public sealed class UpgradeInventoryPresenter : MonoBehaviour
{
    [SerializeField] private UpgradeInventoryView inventoryView;
    [SerializeField] private UpgradeInventoryManager inventoryManager;

    bool isBound;

    void Awake()
    {
        if (inventoryView == null)
            inventoryView = GetComponentInChildren<UpgradeInventoryView>(true);
    }

    void Start()
    {
        Bind();
    }

    void OnEnable()
    {
        Bind();
    }

    void OnDisable()
    {
        Unbind();
    }

    void Bind()
    {
        if (isBound)
            return;

        if (inventoryView == null)
            return;

        if (inventoryManager == null)
            inventoryManager = UpgradeInventoryManager.Instance;

        if (inventoryManager == null)
            return;

        inventoryManager.OnInventoryChanged += Refresh;
        isBound = true;
        Refresh();
    }

    void Unbind()
    {
        if (!isBound)
            return;

        if (inventoryManager != null)
            inventoryManager.OnInventoryChanged -= Refresh;

        isBound = false;
    }

    void Refresh()
    {
        if (inventoryManager == null || inventoryView == null)
            return;

        inventoryView.SetSlots(inventoryManager.Upgrades);
    }
}
