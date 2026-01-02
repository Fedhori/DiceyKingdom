using System;
using UnityEngine;

// TODO - 스테이지 구조가 확립되면 스테이지 난이도에 맞춰 텍스트 크기가 가변적으로 조정되게 한다.
public sealed class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    [SerializeField] private float minFontSize = 12f;
    [SerializeField] private float maxFontSize = 48f;

    double minValue = 1;
    double maxValue = 1000;

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
            0.5f,
            position
        );
    }
}
