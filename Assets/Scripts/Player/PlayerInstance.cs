using System;
using System.Collections.Generic;
using Data;
using GameStats;
using UnityEngine;

public sealed class PlayerInstance
{
    public PlayerDto BaseDto { get; }
    public string Id => BaseDto.id;

    public StatSet Stats { get; }

    public double ScoreBase => Stats.GetValue(PlayerStatIds.Score);
    public double ScoreMultiplier => Stats.GetValue(PlayerStatIds.ScoreMultiplier);
    public double CriticalChance => Stats.GetValue(PlayerStatIds.CriticalChance);
    public double CriticalMultiplier => Stats.GetValue(PlayerStatIds.CriticalMultiplier);

    // 희귀도 기반 볼 생성 파라미터
    public IReadOnlyList<float> RarityProbabilities => rarityProbabilities;
    public int BallCount { get; private set; }
    public float RarityGrowth { get; private set; }
    public IReadOnlyList<float> RarityMultipliers => rarityMultipliers;

    // TODO - PlayerRunState라는 값을 만들어서,
    // 해당 Run에서 저장되어야 할 값들(영구 보너스, 재화, 볼 구성, 핀 구성)을 통합해서 관리해야 하지 않을까?
    // 지금은 저장되어야 할 값들이 게임 파일들 곳곳에 뿔뿔이 흩어져있다.

    // 상점/보상에 사용하는 통화
    public int Currency { get; private set; }

    public event Action<int> OnCurrencyChanged;

    readonly List<float> rarityProbabilities;
    readonly List<float> rarityMultipliers = new();

    public PlayerInstance(PlayerDto dto)
    {
        BaseDto = dto ?? throw new ArgumentNullException(nameof(dto));

        Stats = new StatSet();
        Stats.SetBase(PlayerStatIds.Score, BaseDto.scoreBase);
        Stats.SetBase(PlayerStatIds.ScoreMultiplier, BaseDto.scoreMultiplier);
        Stats.SetBase(PlayerStatIds.CriticalChance, BaseDto.critChance);
        Stats.SetBase(PlayerStatIds.CriticalMultiplier, BaseDto.criticalMultiplier);

        BallCount = BaseDto.initialBallCount;
        RarityGrowth = BaseDto.rarityGrowth;
        rarityProbabilities = BaseDto.rarityProbabilities != null
            ? new List<float>(BaseDto.rarityProbabilities)
            : new List<float>();
        NormalizeRarityProbabilities(rarityProbabilities);
        RecalculateRarityMultipliers();

        // 시작 통화 셋업
        Currency = Mathf.Max(0, BaseDto.startCurrency);
    }

    void NormalizeRarityProbabilities(List<float> list)
    {
        if (list == null || list.Count == 0)
        {
            Debug.LogError("[PlayerInstance] rarityProbabilities is empty.");
            throw new InvalidOperationException("[PlayerInstance] rarityProbabilities is empty.");
        }

        float sum = 0f;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] < 0f)
            {
                Debug.LogError("[PlayerInstance] rarityProbabilities contains negative value.");
                throw new InvalidOperationException("[PlayerInstance] rarityProbabilities invalid.");
            }
            sum += list[i];
        }

        if (sum <= 0f)
        {
            Debug.LogError("[PlayerInstance] rarityProbabilities sum <= 0.");
            throw new InvalidOperationException("[PlayerInstance] rarityProbabilities sum invalid.");
        }

        if (Mathf.Abs(sum - 100f) > 0.001f)
        {
            Debug.LogError($"[PlayerInstance] rarityProbabilities sum != 100 (sum={sum}). Normalizing.");
            for (int i = 0; i < list.Count; i++)
            {
                list[i] = list[i] / sum * 100f;
            }
        }
    }

    public void SetRarityGrowth(float newGrowth)
    {
        if (newGrowth <= 0f)
        {
            Debug.LogError($"[PlayerInstance] Invalid rarityGrowth: {newGrowth}");
            throw new InvalidOperationException("[PlayerInstance] rarityGrowth must be > 0.");
        }

        RarityGrowth = newGrowth;
        RecalculateRarityMultipliers();
    }

    void RecalculateRarityMultipliers()
    {
        rarityMultipliers.Clear();
        if (rarityProbabilities == null || rarityProbabilities.Count == 0)
            return;

        for (int i = 0; i < rarityProbabilities.Count; i++)
        {
            float multiplier = Mathf.Pow(RarityGrowth, i);
            rarityMultipliers.Add(multiplier);
        }
    }

    public void ResetData()
    {
        // 라운드 단위로 날아가는 임시 버프만 초기화
        Stats.RemoveModifiers(StatLayer.Temporary);
    }

    public int RollCriticalLevel(System.Random rng)
    {
        if (rng == null)
            rng = new System.Random();

        int criticalLevel = (int)(CriticalChance / 100f);
        double chance = Math.Max(0f, CriticalChance - criticalLevel * 100f);

        double roll = rng.NextDouble() * 100.0;

        if (chance >= roll)
            criticalLevel++;

        return criticalLevel;
    }

    public double GetCriticalMultiplier(int criticalLevel)
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
