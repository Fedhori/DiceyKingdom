using System;
using Data;
using TMPro;
using UnityEngine;

public sealed class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [SerializeField] string defaultPlayerId = "player.default";

    [SerializeField] private TMP_Text playerStatText;
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
        playerStatText.text =
            $"기본 점수: {Current.ScoreBase}\n크리확률: {Current.CriticalChance}\n크리배율: {Current.CriticalMultiplier}";
    }
}