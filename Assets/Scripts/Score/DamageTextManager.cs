using System;
using UnityEngine;

public sealed class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    [SerializeField] private float minFontSize = 12f;
    [SerializeField] private float maxFontSize = 48f;

    double minValue = 10;
    double maxValue = 10000;

    void Awake()
    {
        Instance = this;
    }

    public void SetMinMaxValue(double blockHealth)
    {
        if (blockHealth <= 0)
        {
            minValue = 10;
            maxValue = 10000;
            return;
        }

        maxValue = blockHealth;
        minValue = blockHealth / 100.0;

        if (minValue < 0)
            minValue = 0;

        if (maxValue <= minValue)
            maxValue = minValue + 1.0;
    }

    float GetFontSizeForDamage(double damage)
    {
        var value = Math.Clamp(damage, minValue, maxValue);

        double t = (Math.Log(value) - Math.Log(minValue)) / (Math.Log(maxValue) - Math.Log(minValue));
        t = Math.Clamp(t, 0.0, 1.0);
        return Mathf.Lerp(minFontSize, maxFontSize, (float)t);
    }

    public void ShowDamageText(double amount, int criticalLevel, Vector2 position)
    {
        if (amount == 0)
            return;

        var color = Colors.GetCriticalColor(criticalLevel);

        var postFix = "";
        if (criticalLevel == 1)
            postFix = "!";
        else if (criticalLevel >= 2)
            postFix = "!!";

        FloatingTextManager.Instance.ShowText(
            amount + postFix,
            color,
            GetFontSizeForDamage(amount),
            1f,
            position
        );
    }
}
