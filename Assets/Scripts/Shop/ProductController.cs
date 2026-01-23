using Data;
using TMPro;
using UnityEngine;

public sealed class ProductController : ProductViewBase
{
    [SerializeField] private ItemView itemView;
    [SerializeField] private ItemTooltipTarget tooltipTarget;
    [SerializeField] private TMP_Text priceText;

    bool isSelected;
    IProduct boundProduct;
    ItemProduct boundItem;

    void Awake()
    {
        if (itemView == null)
            itemView = GetComponentInChildren<ItemView>(true);
        if (tooltipTarget == null)
            tooltipTarget = GetComponentInChildren<ItemTooltipTarget>(true);
    }

    public override void SetData(IProduct product, int price, bool canBuy, bool sold)
    {
        boundProduct = product;
        boundItem = product as ItemProduct;
        SetViewType(product?.ProductType ?? ViewType);

        bool canInteract = (product != null) && !sold;
        CanDrag = canBuy && canInteract;
        CanClick = canInteract;

        if (product == null)
        {
            gameObject.SetActive(false);
            if (tooltipTarget != null)
                tooltipTarget.Clear();
            return;
        }

        gameObject.SetActive(true);

        if (itemView != null)
        {
            itemView.SetIcon(product.Icon);
            if (boundItem != null)
                itemView.SetRarity(boundItem.Rarity);
            itemView.SetSelected(isSelected);
        }

        if (tooltipTarget != null)
        {
            if (boundItem != null)
                tooltipTarget.Bind(boundItem.PreviewInstance, true);
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
