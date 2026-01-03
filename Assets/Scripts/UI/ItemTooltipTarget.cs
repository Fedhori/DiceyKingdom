using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ItemTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TooltipAnchorType anchorType = TooltipAnchorType.Screen;

    ItemInstance instance;

    public void Bind(ItemInstance boundInstance)
    {
        instance = boundInstance;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (instance == null)
            return;

        var manager = TooltipManager.Instance;
        if (manager == null)
            return;

        var model = ItemTooltipUtil.BuildModel(instance);
        TooltipAnchor anchor = anchorType == TooltipAnchorType.World
            ? TooltipAnchor.FromWorld(transform.position)
            : TooltipAnchor.FromScreen(eventData.position, eventData.position);

        manager.BeginHover(this, model, anchor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        var manager = TooltipManager.Instance;
        if (manager == null)
            return;

        manager.EndHover(this);
    }
}
