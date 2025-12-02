using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PinController))]
public sealed class PinTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
        var manager = TooltipManager.Instance;
        if (manager == null)
            return;

        if (pinController == null || pinController.Instance == null)
            return;

        var pin = pinController.Instance;

        // 월드 상에서 핀의 우상단 위치 계산
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

        TooltipModel model = PinTooltipUtil.BuildModel(pin);
        TooltipAnchor anchor = TooltipAnchor.FromWorld(worldAnchor);

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