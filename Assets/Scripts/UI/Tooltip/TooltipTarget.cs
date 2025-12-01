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
        // 핀의 시각적 영역을 대표할 SpriteRenderer (필요시 GetComponentInChildren 으로 확장)
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipManager.Instance == null)
            return;
        if (pinController == null || pinController.Instance == null)
            return;

        // 기본 anchor: 핀의 "우상단" 월드 좌표
        Vector3 worldAnchor;

        if (spriteRenderer != null)
        {
            var b = spriteRenderer.bounds;
            // bounds.max == (우상단) 이지만, z 는 center.z 를 써서 카메라 z 와의 관계 유지
            worldAnchor = new Vector3(b.max.x, b.max.y, b.center.z);
        }
        else
        {
            // 스프라이트가 없으면 transform 기준으로 대체 (대략적인 위치)
            worldAnchor = transform.position;
        }

        TooltipManager.Instance.BeginHover(pinController.Instance, worldAnchor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance == null)
            return;
        if (pinController == null || pinController.Instance == null)
            return;

        TooltipManager.Instance.EndHover(pinController.Instance);
    }
}