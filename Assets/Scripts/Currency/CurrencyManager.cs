using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

public sealed class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [SerializeField] TMP_Text currencyText;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnEnable()
    {
        SubscribePlayer();
        RefreshUI();
    }

    void OnDisable()
    {
        UnsubscribePlayer();
    }

    private void Start()
    {
        SubscribePlayer();
        RefreshUI();
    }

    void SubscribePlayer()
    {
        UnsubscribePlayer();

        var pm = PlayerManager.Instance;
        if (pm == null)
            return;

        var player = pm.Current;
        if (player == null)
            return;

        player.OnCurrencyChanged += HandleCurrencyChanged;
    }

    void UnsubscribePlayer()
    {
        var pm = PlayerManager.Instance;
        if (pm == null)
            return;

        var player = pm.Current;
        if (player == null)
            return;

        player.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    void HandleCurrencyChanged(int value)
    {
        currencyText.text = $"${value}";
    }

    void RefreshUI()
    {
        var pm = PlayerManager.Instance;
        if (pm?.Current != null)
        {
            HandleCurrencyChanged(pm.Current.Currency);
        }
    }

    public int CurrentCurrency =>
        PlayerManager.Instance?.Current?.Currency ?? 0;

    public void AddCurrency(int amount)
    {
        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return;

        player.AddCurrency(amount);
    }

    public bool TrySpend(int cost)
    {
        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return false;

        return player.TrySpendCurrency(cost);
    }
}