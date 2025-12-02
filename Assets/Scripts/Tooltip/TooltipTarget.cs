using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PinController))]
public sealed class TooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    PinController pinController;
    SpriteRenderer spriteRenderer;

    void Awake()
    {
        pinController = GetComponent<PinController>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipManager.Instance == null)
            return;
        if (pinController == null || pinController.Instance == null)
            return;

        var pin = pinController.Instance;

        Vector3 worldAnchor;
        if (spriteRenderer != null)
        {
            var b = spriteRenderer.bounds;
            worldAnchor = new Vector3(b.max.x, b.max.y, b.center.z);
        }
        else
        {
            worldAnchor = transform.position;
        }

        var model = BuildPinTooltipModel(pin);
        var anchor = TooltipAnchor.FromWorld(worldAnchor);

        TooltipManager.Instance.BeginHover(this, model, anchor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance == null)
            return;

        TooltipManager.Instance.EndHover(this);
    }

    TooltipModel BuildPinTooltipModel(PinInstance pin)
    {
        string title = LocalizationUtil.GetPinName(pin.Id);
        Sprite icon = SpriteCache.GetPinSprite(pin.Id);

        float mult = pin.ScoreMultiplier;
        string body = $"Score x{mult:0.##}";

        return new TooltipModel(
            title,
            body,
            icon,
            TooltipKind.Pin
        );
    }
}