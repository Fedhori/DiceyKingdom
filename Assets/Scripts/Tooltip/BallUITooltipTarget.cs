using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BallUITooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] RectTransform anchorRect;
    BallInstance ball;

    readonly Vector3[] corners = new Vector3[4];

    void Awake()
    {
        if (anchorRect == null)
            anchorRect = transform as RectTransform;
    }

    public void Bind(BallInstance ball)
    {
        this.ball = ball;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var manager = TooltipManager.Instance;
        if (manager == null)
            return;

        if (ball == null)
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

        TooltipModel model = BallTooltipUtil.BuildModel(ball);
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