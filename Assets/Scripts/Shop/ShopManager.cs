using UnityEngine;

public sealed class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    bool isOpen;
    StageInstance currentStage;
    int nextRoundIndex;
    ShopOpenContext context;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <param name="context">
    /// 라운드 사이 상점인지, 스테이지 후 상점인지.
    /// </param>
    /// <param name="nextRoundIndex">
    /// 라운드 사이 상점이면 다음 라운드 인덱스(0-based), 스테이지 후 상점이면 -1.
    /// </param>
    public void Open(StageInstance stage, ShopOpenContext context, int nextRoundIndex)
    {
        currentStage = stage;
        this.context = context;
        this.nextRoundIndex = nextRoundIndex;
        isOpen = true;

        if (stage != null)
        {
            switch (context)
            {
                case ShopOpenContext.BetweenRounds:
                    Debug.Log($"[ShopManager] Open shop for stage {stage.StageIndex + 1}, before round {nextRoundIndex + 1}");
                    break;
                case ShopOpenContext.AfterStage:
                    Debug.Log($"[ShopManager] Open shop after stage {stage.StageIndex + 1}");
                    break;
            }
        }
        else
        {
            Debug.Log("[ShopManager] Open shop (stage is null)");
        }

        // TODO: 실제 상점 UI를 띄우고, 닫기 버튼에서 Close() 호출하도록 변경.
        // 지금은 플로우 테스트용으로 바로 닫음.
        Close();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;
        Debug.Log("[ShopManager] Close shop");

        FlowManager.Instance?.OnShopClosed(context);
    }
}