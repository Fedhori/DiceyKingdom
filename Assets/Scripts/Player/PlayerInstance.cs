using System;
using Data;
using GameStats;
using UnityEngine;

public sealed class PlayerInstance
{
    public PlayerDto BaseDto { get; }
    public string Id => BaseDto.id;

    public StatSet Stats { get; }

    public float ScoreBase => Stats.GetValue(PlayerStatIds.Score);
    public float ScoreMultiplier => Stats.GetValue(PlayerStatIds.ScoreMultiplier);
    public float CriticalChance => Stats.GetValue(PlayerStatIds.CriticalChance);
    public float CriticalMultiplier => Stats.GetValue(PlayerStatIds.CriticalMultiplier);

    // TODO - PlayerRunState라는 값을 만들어서,
    // 해당 Run에서 저장되어야 할 값들(영구 보너스, 재화, 볼 구성, 핀 구성)을 통합해서 관리해야 하지 않을까?
    // 지금은 저장되어야 할 값들이 게임 파일들 곳곳에 뿔뿔이 흩어져있다.
    
    // 플레이어가 들고 있는 볼 덱
    public BallDeck BallDeck { get; }

    // 상점/보상에 사용하는 통화
    public int Currency { get; private set; }

    public event Action<int> OnCurrencyChanged;

    public PlayerInstance(PlayerDto dto)
    {
        BaseDto = dto ?? throw new ArgumentNullException(nameof(dto));

        Stats = new StatSet();
        Stats.SetBase(PlayerStatIds.Score, BaseDto.scoreBase);
        Stats.SetBase(PlayerStatIds.ScoreMultiplier, BaseDto.scoreMultiplier);
        Stats.SetBase(PlayerStatIds.CriticalChance, BaseDto.critChance);
        Stats.SetBase(PlayerStatIds.CriticalMultiplier, BaseDto.criticalMultiplier);

        BallDeck = new BallDeck();
        if (BaseDto.ballDeck != null)
        {
            foreach (var entry in BaseDto.ballDeck)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id))
                    continue;

                var count = Mathf.Max(0, entry.count);
                if (count <= 0)
                    continue;

                // 유효하지 않은 ballId는 무시
                if (!BallRepository.IsInitialized ||
                    !BallRepository.TryGet(entry.id, out _))
                {
                    Debug.LogWarning($"[PlayerInstance] Unknown ball id in deck: {entry.id}");
                    continue;
                }

                BallDeck.Add(entry.id, count);
            }
        }

        // 시작 통화 셋업
        Currency = Mathf.Max(0, BaseDto.startCurrency);
    }

    public void ResetData()
    {
        // 라운드 단위로 날아가는 임시 버프만 초기화
        Stats.RemoveModifiers(StatLayer.Temporary);
        // BallDeck, Currency 등은 런 단위 자원이므로 여기서는 건드리지 않음.
    }

    public int RollCriticalLevel(System.Random rng)
    {
        if (rng == null)
            rng = new System.Random();

        int criticalLevel = (int)(CriticalChance / 100f);
        float chance = Mathf.Max(0f, CriticalChance - criticalLevel * 100f);

        double roll = rng.NextDouble() * 100.0;

        if (chance >= roll)
            criticalLevel++;

        return criticalLevel;
    }

    public float GetCriticalMultiplier(int criticalLevel)
    {
        return Mathf.Max(1f, criticalLevel * 2f);
    }

    public void AddCurrency(int amount)
    {
        if (amount == 0)
            return;

        var newValue = Currency + amount;
        if (newValue < 0)
            newValue = 0;

        if (newValue == Currency)
            return;

        Currency = newValue;
        OnCurrencyChanged?.Invoke(Currency);
    }

    public bool TrySpendCurrency(int cost)
    {
        if (cost <= 0)
            return true;

        if (Currency < cost)
            return false;

        Currency -= cost;
        OnCurrencyChanged?.Invoke(Currency);
        return true;
    }
}