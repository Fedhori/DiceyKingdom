using TMPro;
using UnityEngine;
public sealed class ResultDamageRow : MonoBehaviour
{
    [SerializeField] private ItemView itemView;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private RectTransform damageBarGraph;

    float cachedBarWidth = -1f;

    public void Bind(ItemInstance item, double damage, double maxDamage)
    {
        if (itemView != null)
            itemView.SetIcon(SpriteCache.GetItemSprite(item?.Id));

        if (damageText != null)
            damageText.text = Mathf.FloorToInt((float)damage).ToString();

        if (damageBarGraph == null)
            return;

        float ratio = 0f;
        if (maxDamage > 0d)
            ratio = Mathf.Clamp01((float)(damage / maxDamage));

        SetBarWidth(ratio);
    }

    void SetBarWidth(float ratio)
    {
        float maxWidth = GetMaxBarWidth();
        if (maxWidth <= 0f)
            return;

        float width = Mathf.Clamp(maxWidth * ratio, 0f, maxWidth);
        damageBarGraph.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
    }

    float GetMaxBarWidth()
    {
        if (cachedBarWidth <= 0f)
        {
            if (damageBarGraph == null)
                return 0f;

            cachedBarWidth = damageBarGraph.rect.width;
            if (cachedBarWidth <= 0f)
                cachedBarWidth = damageBarGraph.sizeDelta.x;
        }

        return cachedBarWidth;
    }
}
