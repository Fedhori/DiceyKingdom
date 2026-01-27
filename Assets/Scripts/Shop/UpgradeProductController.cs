using Data;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public sealed class UpgradeProductController : ProductViewBase
{
    [SerializeField] private ItemView itemView;
    [SerializeField] private ItemTooltipTarget tooltipTarget;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private GameObject priceTagRoot;
    [SerializeField] private GameObject soldPanel;

    bool isSelected;
    UpgradeProduct boundUpgrade;

    void Awake()
    {
        if (itemView == null)
            itemView = GetComponentInChildren<ItemView>(true);
        if (tooltipTarget == null)
            tooltipTarget = GetComponentInChildren<ItemTooltipTarget>(true);
    }

    public override void SetData(IProduct product, int price, bool canBuy, bool canDrag, bool sold)
    {
        boundUpgrade = product as UpgradeProduct;
        SetViewType(product?.ProductType ?? ViewType);

        bool canInteract = (product != null) && !sold;
        CanDrag = canDrag && canInteract;
        CanClick = canInteract;

        if (product == null)
        {
            gameObject.SetActive(false);
            tooltipTarget?.Clear();
            return;
        }

        gameObject.SetActive(true);
        if (soldPanel != null)
            soldPanel.SetActive(sold);
        if (priceTagRoot != null)
            priceTagRoot.SetActive(!sold);

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

        if (priceText != null && (!sold || priceTagRoot == null))
        {
            if (sold)
            {
                priceText.gameObject.SetActive(false);
            }
            else
            {
                if (!priceText.gameObject.activeSelf)
                    priceText.gameObject.SetActive(true);
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
