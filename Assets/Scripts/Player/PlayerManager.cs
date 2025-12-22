using System;
using Data;
using TMPro;
using UnityEngine;

public sealed class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [SerializeField] string defaultPlayerId = "player.default";

    [SerializeField] private TMP_Text baseScoreText;
    [SerializeField] private TMP_Text scoreMultiplierText;
    [SerializeField] private TMP_Text criticalChanceText;
    [SerializeField] private TMP_Text criticalMultiplierText;
    private float statUIUpdateCycle = 0.1f;
    private float currentStatUIUpdateCycle = 0f;

    public PlayerInstance Current { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PlayerManager] Multiple instances detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (!PlayerRepository.IsInitialized)
        {
            Debug.LogWarning("[PlayerManager] PlayerRepository not initialized. " +
                             "Call PlayerRepository.InitializeFromJson before creating a player.");
            return;
        }

        try
        {
            CreatePlayer(defaultPlayerId);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerManager] Failed to create default player '{defaultPlayerId}': {e}");
        }
    }

    public void CreatePlayer(string playerId)
    {
        if (!PlayerRepository.IsInitialized)
        {
            Debug.LogError("[PlayerManager] Cannot create player. PlayerRepository not initialized.");
            return;
        }

        try
        {
            var dto = PlayerRepository.GetOrThrow(playerId);
            Current = new PlayerInstance(dto);

            // 통화 HUD 등이 있으면 여기서 알림
            CurrencyManager.Instance?.OnPlayerCreated(Current);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PlayerManager] Failed to create player '{playerId}': {e}");
        }
    }

    void Update()
    {
        currentStatUIUpdateCycle += Time.deltaTime;
        if (currentStatUIUpdateCycle >= statUIUpdateCycle)
        {
            currentStatUIUpdateCycle = 0f;
            UpdateStatUI();
        }
    }

    void UpdateStatUI()
    {
        baseScoreText.text = $"{Current.ScoreBase}";
        scoreMultiplierText.text = $"x{Current.ScoreMultiplier:N1}";
        criticalChanceText.text = $"{Current.CriticalChance}%";
        criticalMultiplierText.text = $"x{Current.CriticalMultiplier:N1}";
    }

    public void ResetPlayer()
    {
        Current?.ResetData();
    }
}
