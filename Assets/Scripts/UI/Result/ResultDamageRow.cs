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
    }
}
