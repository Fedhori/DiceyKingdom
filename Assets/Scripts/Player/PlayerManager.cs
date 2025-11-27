using System;
using Data;
using UnityEngine;

public sealed class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [SerializeField] string defaultPlayerId = "player.default";

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
            var dto = PlayerRepository.GetOrThrow(defaultPlayerId);
            Current = new PlayerInstance(dto);
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
}