using Data;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public sealed class UpgradeProductController : ProductViewBase
{
    [SerializeField] private ItemView itemView;
    [SerializeField] private ItemTooltipTarget tooltipTarget;
    [SerializeField] private TMP_Text priceText;

    bool isSelected;
    UpgradeProduct boundUpgrade;

    void Awake()
    {
        if (itemView == null)
            itemView = GetComponentInChildren<ItemView>(true);
        if (tooltipTarget == null)
            tooltipTarget = GetComponentInChildren<ItemTooltipTarget>(true);
    }

    public override void SetData(IProduct product, int price, bool canBuy, bool sold)
    {
        boundUpgrade = product as UpgradeProduct;
        SetViewType(product?.ProductType ?? ViewType);

        bool canInteract = (product != null) && !sold;
        CanDrag = canBuy && canInteract;
        CanClick = canInteract;

        if (product == null)
        {
            gameObject.SetActive(false);
            tooltipTarget?.Clear();
            return;
        }

        gameObject.SetActive(true);

        if (itemView != null)
        {
            itemView.SetIcon(product.Icon);
            itemView.SetRarity(boundUpgrade != null ? boundUpgrade.Rarity : ItemRarity.Common);
            itemView.SetSelected(isSelected);
        }

        if (tooltipTarget != null)
        {
            if (boundUpgrade != null)
                tooltipTarget.BindUpgrade(boundUpgrade.PreviewInstance, ItemTooltipTarget.TooltipActionKind.BuyUpgrade, boundUpgrade);
            else
                tooltipTarget.Clear();
        }

        if (priceText != null)
        {
            if (sold)
            {
                priceText.text = LocalizationUtil.SoldString;
                priceText.color = Colors.White;
            }
            else
            {
                priceText.text = $"${price}";
                priceText.color = canBuy ? Colors.White : Colors.Red;
            }
        }
    }

    public override void SetSelected(bool selected)
    {
        isSelected = selected;
        itemView?.SetSelected(isSelected);
    }

    public override void PinTooltip()
    {
        tooltipTarget?.Pin();
    }
}
