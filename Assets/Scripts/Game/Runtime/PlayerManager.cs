using System;
using UnityEngine;

public sealed class PlayerManager : MonoBehaviour
{
    int stability;
    int maxStability;
    int gold;
    GameRunState runState;

    public static PlayerManager Instance { get; private set; }

    public event Action<int> StabilityChanged;
    public event Action<int> MaxStabilityChanged;
    public event Action<int> GoldChanged;

    public int Stability
    {
        get => stability;
        private set
        {
            if (stability == value)
                return;

            stability = value;
            if (runState != null)
                runState.stability = value;
            StabilityChanged?.Invoke(value);
        }
    }

    public int MaxStability
    {
        get => maxStability;
        private set
        {
            if (maxStability == value)
                return;

            maxStability = value;
            if (runState != null)
                runState.maxStability = value;
            MaxStabilityChanged?.Invoke(value);
        }
    }

    public int Gold
    {
        get => gold;
        private set
        {
            if (gold == value)
                return;

            gold = value;
            if (runState != null)
                runState.gold = value;
            GoldChanged?.Invoke(value);
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void InitializeForRun(GameRunState state)
    {
        runState = state;

        MaxStability = state != null ? state.maxStability : 0;
        Stability = state != null ? state.stability : 0;
        Gold = state != null ? state.gold : 0;
    }

    public void ClampResources()
    {
        Stability = ClampStability(Stability);
        Gold = ClampGold(Gold);
    }

    public bool ApplyEffectBundle(EffectBundle effectBundle)
    {
        if (effectBundle?.effects == null)
            return true;

        for (int i = 0; i < effectBundle.effects.Count; i++)
        {
            var effect = effectBundle.effects[i];
            if (effect == null || string.IsNullOrWhiteSpace(effect.effectType))
                continue;

            string effectType = effect.effectType.Trim();
            int value = ToIntValue(effect.value);

            if (string.Equals(effectType, "stabilityDelta", StringComparison.Ordinal))
            {
                Stability += value;
                continue;
            }

            if (string.Equals(effectType, "goldDelta", StringComparison.Ordinal))
                Gold += value;
        }

        return true;
    }

    int ClampStability(int value)
    {
        if (value < 0)
            return 0;

        if (value > MaxStability)
            return MaxStability;

        return value;
    }

    static int ClampGold(int value)
    {
        return value < 0 ? 0 : value;
    }

    static int ToIntValue(double? value)
    {
        if (!value.HasValue)
            return 0;

        return (int)Math.Round(value.Value, MidpointRounding.AwayFromZero);
    }
}
