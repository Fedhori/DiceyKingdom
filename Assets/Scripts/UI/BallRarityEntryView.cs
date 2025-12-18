using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BallRarityEntryView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text multiplierText;
    [SerializeField] private TMP_Text probabilityText;

    public void Bind(Color color, double multiplier, float probabilityPercent)
    {
        if (iconImage != null)
            iconImage.color = color;

        if (multiplierText != null)
            multiplierText.text = $"x{multiplier:0.#}";

        if (probabilityText != null)
            probabilityText.text = $"{probabilityPercent:0.#}%";
    }
}
