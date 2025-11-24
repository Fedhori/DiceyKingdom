using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(BoxCollider2D))]
public class WorldGaugeBar : MonoBehaviour
{
    [SerializeField] private SpriteRenderer fillRenderer;
    [SerializeField] private TextMeshPro hoverText;
    [SerializeField] private bool isShowText = true;

    // 호버를 받을 전용 콜라이더(자식이어도 OK)
    [SerializeField] private Collider2D hoverCollider;

    private float current;
    private float max = 1f;

    private void Awake()
    {
        if (hoverText != null)
            hoverText.gameObject.SetActive(false);

        hoverCollider = GetComponent<BoxCollider2D>();

        if (hoverCollider != null)
        {
            var proxy = hoverCollider.GetComponent<GaugeBarHoverProxy>();
            if (proxy == null)
                proxy = hoverCollider.gameObject.AddComponent<GaugeBarHoverProxy>();

            proxy.Bind(this);
        }
    }

    public void UpdateFill(float newCurrent, float newMax)
    {
        // 캐싱 스킵
        if (Mathf.Approximately(newCurrent, current) && Mathf.Approximately(newMax, max))
            return;

        max = newMax <= 0f ? 1f : newMax;
        current = Mathf.Clamp(newCurrent, 0f, max);

        float ratio = current / max;

        var s = fillRenderer.transform.localScale;
        s.x = ratio;
        fillRenderer.transform.localScale = s;

        if (hoverText != null)
            hoverText.text = $"{current:N0} / {max:N0}";
    }

    // 프록시로부터 호출됨
    internal void SetHover(bool hovered)
    {
        if (!isShowText || hoverText == null)
            return;

        hoverText.gameObject.SetActive(hovered);
    }

    private void OnDisable()
    {
        if (!isShowText || hoverText == null)
            return;

        hoverText.gameObject.SetActive(false);
    }
}

/// <summary>
/// 지정된 Collider2D에 붙어서 마우스/터치 포인터 이벤트를 GaugeBar로 포워딩
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GaugeBarHoverProxy : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private WorldGaugeBar target;

    public void Bind(WorldGaugeBar target)
    {
        this.target = target;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        target?.SetHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        target?.SetHover(false);
    }

    private void OnDisable()
    {
        target?.SetHover(false);
    }
}