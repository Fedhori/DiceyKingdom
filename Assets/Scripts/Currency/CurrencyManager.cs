using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

public sealed class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [SerializeField] TMP_Text currencyText;

    PlayerInstance subscribedPlayer;

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

    void SubscribePlayer()
    {
        UnsubscribePlayer();

        var pm = PlayerManager.Instance;
        if (pm == null)
            return;

        var player = pm.Current;
        if (player == null)
            return;

        subscribedPlayer = player;
        subscribedPlayer.OnCurrencyChanged += HandleCurrencyChanged;
    }

    void UnsubscribePlayer()
    {
        if (subscribedPlayer != null)
        {
            subscribedPlayer.OnCurrencyChanged -= HandleCurrencyChanged;
            subscribedPlayer = null;
        }
    }

    void HandleCurrencyChanged(int value)
    {
        // if (currencyText.StringReference.TryGetValue("value", out var v) && v is StringVariable sv)
        //     sv.Value = value.ToString();
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

    // PlayerManager.CreatePlayer 이후 호출됨
    public void OnPlayerCreated(PlayerInstance player)
    {
        SubscribePlayer();
        RefreshUI();
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