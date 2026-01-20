using UnityEngine;

public sealed class UpgradeInventoryNotice : MonoBehaviour
{
    public static UpgradeInventoryNotice Instance { get; private set; }

    [SerializeField] private GameObject noticeRoot;
    [SerializeField] private UpgradeInventoryView inventoryView;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SetActive(false);
    }

    void OnEnable()
    {
        // TODO - 초기화 안됨..
        var manager = UpgradeInventoryManager.Instance;
        if (manager != null)
            manager.OnNewUpgradeAdded += HandleNewUpgradeAdded;

        var stageManager = StageManager.Instance;
        if (stageManager != null)
            stageManager.OnPhaseChanged += HandlePhaseChanged;
    }

    void OnDisable()
    {
        var manager = UpgradeInventoryManager.Instance;
        if (manager != null)
            manager.OnNewUpgradeAdded -= HandleNewUpgradeAdded;

        var stageManager = StageManager.Instance;
        if (stageManager != null)
            stageManager.OnPhaseChanged -= HandlePhaseChanged;
    }

    public void Clear()
    {
        SetActive(false);
    }

    void HandleNewUpgradeAdded()
    {
        if (IsInventoryOpen())
            return;

        SetActive(true);
    }

    void HandlePhaseChanged(StagePhase phase)
    {
        if (phase != StagePhase.Shop)
            SetActive(false);
    }

    bool IsInventoryOpen()
    {
        if (inventoryView == null)
            return false;

        return inventoryView.gameObject.activeInHierarchy;
    }

    void SetActive(bool active)
    {
        if (noticeRoot != null)
            noticeRoot.SetActive(active);
    }
}
