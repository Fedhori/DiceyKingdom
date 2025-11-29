using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class StaticDataManager : MonoBehaviour
{
    public static StaticDataManager Instance { get; private set; }

    public List<StageDto> Stages { get; private set; } = new();
    private static readonly StringComparer SpecKeyComparer = StringComparer.OrdinalIgnoreCase;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadAndProcessData();
    }

    void LoadAndProcessData()
    {
        LoadStage();
        LoadBalls();
        LoadPins();
        LoadPlayers();
    }

    void LoadStage()
    {
        string filePath = Path.Combine("Data", "Stages.json");
        string json = SaCache.ReadText(filePath);

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError($"[StaticDataManager] Stages.json not found or empty at: {filePath}");
            return;
        }

        try
        {
            var root = JsonConvert.DeserializeObject<StageRoot>(json);
            StageRepository.Initialize(root);
        }
        catch (Exception e)
        {
            Debug.LogError($"[StaticDataManager] Failed to initialize PlayerRepository from Players.json: {e}");
        }
    }

    void LoadPlayers()
    {
        string filePath = Path.Combine("Data", "Players.json");
        string json = SaCache.ReadText(filePath);

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError($"[StaticDataManager] Players.json not found or empty at: {filePath}");
            return;
        }

        try
        {
            Data.PlayerRepository.InitializeFromJson(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[StaticDataManager] Failed to initialize PlayerRepository from Players.json: {e}");
        }
    }

    void LoadBalls()
    {
        string filePath = Path.Combine("Data", "Balls.json");
        string json = SaCache.ReadText(filePath);

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError($"[StaticDataManager] Balls.json not found or empty at: {filePath}");
            return;
        }

        try
        {
            Data.BallRepository.InitializeFromJson(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[StaticDataManager] Failed to initialize BallRepository from Balls.json: {e}");
        }
    }

    void LoadPins()
    {
        string filePath = Path.Combine("Data", "Pins.json");
        string json = SaCache.ReadText(filePath);

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError($"[StaticDataManager] Pins.json not found or empty at: {filePath}");
            return;
        }

        try
        {
            Data.PinRepository.InitializeFromJson(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[StaticDataManager] Failed to initialize PinRepository from Pins.json: {e}");
        }
    }
}