using Data;
using UnityEngine;

public sealed class UpgradeInventorySlotController : MonoBehaviour
{
    public int Index { get; private set; } = -1;
    public UpgradeInstance Upgrade { get; private set; }

    [SerializeField] ItemView itemView;
    [SerializeField] ItemTooltipTarget tooltipTarget;

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
                tooltipTarget.BindUpgrade(Upgrade);
            else
                tooltipTarget.Clear();
        }
    }
}
