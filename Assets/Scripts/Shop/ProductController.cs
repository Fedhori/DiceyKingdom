using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ProductController : ProductViewBase
{
    [SerializeField] private ItemView itemView;
    [SerializeField] private ItemTooltipTarget tooltipTarget;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private GameObject priceTagRoot;
    [SerializeField] private Image priceBackground;
    [SerializeField] private GameObject soldPanel;

    bool isSelected;
    IProduct boundProduct;
    ItemProduct boundItem;

    void Awake()
    {
        if (itemView == null)
            itemView = GetComponentInChildren<ItemView>(true);
        if (tooltipTarget == null)
            tooltipTarget = GetComponentInChildren<ItemTooltipTarget>(true);
        if (priceBackground == null)
            priceBackground = FindPriceBackground();
    }

    public override void SetData(IProduct product, int price, bool canBuy, bool canDrag, bool sold)
    {
        boundProduct = product;
        boundItem = product as ItemProduct;
        SetViewType(product?.ProductType ?? ViewType);

        bool canInteract = (product != null) && !sold;
        CanDrag = canDrag && canInteract;
        CanClick = canInteract;

        if (product == null)
        {
            gameObject.SetActive(false);
            if (tooltipTarget != null)
                tooltipTarget.Clear();
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
                priceText.color = canBuy ? Colors.White : Colors.Invalid;
            }
        }

        if (priceBackground != null)
            priceBackground.color = Colors.Item;
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

    Image FindPriceBackground()
    {
        if (priceTagRoot != null)
        {
            var images = priceTagRoot.GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                if (image != null && image.gameObject.name == "PriceBackground")
                    return image;
            }
        }

        var allImages = GetComponentsInChildren<Image>(true);
        foreach (var image in allImages)
        {
            if (image != null && image.gameObject.name == "PriceBackground")
                return image;
        }

        return null;
    }
}
