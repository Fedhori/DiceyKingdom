using System.Collections.Generic;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public sealed class SellOverlayController : MonoBehaviour
{
    public static SellOverlayController Instance { get; private set; }

    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMP_Text labelText;

    const string LabelKey = "shop.selloverlay.label";

    public bool IsVisible => overlayRoot != null && overlayRoot.activeSelf;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    public void Show()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(true);
    }

    public void Show(ItemInstance item)
    {
        UpdateLabel(item);
        Show();
    }

    public void Hide()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    public bool ContainsScreenPoint(Vector2 screenPos)
    {
        var rt = overlayRoot.transform as RectTransform;
        if (rt == null)
            return false;

        var cv = canvas != null ? canvas : rt.GetComponentInParent<Canvas>();
        if (cv == null)
            return false;

        Camera cam = null;
        if (cv.renderMode is RenderMode.ScreenSpaceCamera or RenderMode.WorldSpace)
            cam = cv.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, cam);
    }

    void UpdateLabel(ItemInstance item)
    {
        if (labelText == null)
            return;

        int price = 0;
        if (item != null && ItemRepository.TryGet(item.Id, out var dto) && dto != null)
        {
            int basePrice = ShopManager.CalculateSellPrice(dto.price);
            price = Mathf.Max(0, basePrice + item.SellValueBonus);
        }

        var args = new Dictionary<string, object>
        {
            ["value"] = price.ToString("0")
        };

        var loc = new LocalizedString("shop", LabelKey)
        {
            Arguments = new object[] { args }
        };
        labelText.text = loc.GetLocalizedString();
    }
}
