using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PinController))]
public sealed class TooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    PinController pinController;

    void Awake()
    {
        pinController = GetComponent<PinController>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (TooltipManager.Instance == null)
            return;
        if (pinController == null || pinController.Instance == null)
            return;

        // 필요하면 개별 딜레이를 PinTooltipManager에 넘기는 방식으로 확장 가능
        TooltipManager.Instance.BeginHover(pinController.Instance, transform.position);
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