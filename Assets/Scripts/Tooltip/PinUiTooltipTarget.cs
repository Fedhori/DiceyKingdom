using UnityEngine;
using UnityEngine.EventSystems;
using Data; // PinInstance 네임스페이스 맞게 유지

public sealed class PinUiTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] RectTransform anchorRect;

    PinInstance pin;

    readonly Vector3[] corners = new Vector3[4];

    void Awake()
    {
        if (anchorRect == null)
            anchorRect = transform as RectTransform;
    }

    /// <summary>
    /// PinItemView.SetData 에서 호출해서 상점 아이템의 핀 정보를 주입.
    /// </summary>
    public void Bind(PinInstance pin)
    {
        this.pin = pin;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var manager = TooltipManager.Instance;
        if (manager == null)
            return;

        if (pin == null)
            return;

        var rect = anchorRect != null ? anchorRect : transform as RectTransform;
        if (rect == null)
            return;

        rect.GetWorldCorners(corners);
        // corners: 0=BL, 1=TL, 2=TR, 3=BR
        Vector3 topLeftWorld = corners[1];
        Vector3 topRightWorld = corners[2];

        Vector2 screenRightTop = RectTransformUtility.WorldToScreenPoint(null, topRightWorld);
        Vector2 screenLeftTop = RectTransformUtility.WorldToScreenPoint(null, topLeftWorld);

        TooltipModel model = PinTooltipUtil.BuildModel(pin);
        TooltipAnchor anchor = TooltipAnchor.FromScreen(screenRightTop, screenLeftTop);

        manager.BeginHover(this, model, anchor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        var manager = TooltipManager.Instance;
        if (manager == null)
            return;

        manager.EndHover(this);
    }

    void OnDisable()
    {
        var manager = TooltipManager.Instance;
        if (manager == null)
            return;

        manager.EndHover(this);
    }
}