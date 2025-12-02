using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(BallController))]
public sealed class BallTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    BallController ballController;
    SpriteRenderer spriteRenderer;

    void Awake()
    {
        ballController = GetComponent<BallController>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var manager = TooltipManager.Instance;
        if (manager == null)
            return;

        if (ballController == null || ballController.Instance == null)
            return;

        var ball = ballController.Instance;

        // 월드 상에서 볼의 우상단 위치 계산 (핀과 동일한 방식)
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

        TooltipModel model = BallTooltipUtil.BuildModel(ball);
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