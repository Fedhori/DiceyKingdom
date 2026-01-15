using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class ResultDamageRow : MonoBehaviour
{
    [SerializeField] private ItemView itemView;
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private Image damageBarFill;

    public void Bind(ItemInstance item, double damage, double maxDamage)
    {
        if (itemView != null)
            itemView.SetIcon(SpriteCache.GetItemSprite(item?.Id));

        if (damageText != null)
            damageText.text = Mathf.FloorToInt((float)damage).ToString();

        if (damageBarFill != null)
        {
            float ratio = 0f;
            if (maxDamage > 0d)
                ratio = Mathf.Clamp01((float)(damage / maxDamage));

            damageBarFill.fillAmount = ratio;
        }
    }
}
