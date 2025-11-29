// ShopManager.cs
using UnityEngine;

public sealed class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    bool isOpen;
    StageInstance currentStage;
    int nextRoundIndex;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void Open(StageInstance stage, int nextRoundIndex)
    {
        currentStage = stage;
        this.nextRoundIndex = nextRoundIndex;
        isOpen = true;

        Debug.Log($"[ShopManager] Open shop for stage {stage.StageIndex + 1}, before round {nextRoundIndex + 1}");
        Close();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;
        Debug.Log("[ShopManager] Close shop");

        StageManager.Instance?.HandleShopClosed();
    }
}