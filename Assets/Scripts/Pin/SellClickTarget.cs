using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public sealed class SellClickTarget : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private PinController pinController;

    void Awake()
    {
        if (pinController == null)
            pinController = GetComponentInParent<PinController>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (pinController == null)
        {
            Debug.LogWarning("[SellClickTarget] PinController not found.");
            return;
        }

        PinManager.Instance.SellPin(pinController);

        Debug.Log($"[SellClickTarget] Sell clicked for pin {pinController.Instance?.Id}");
    }
}