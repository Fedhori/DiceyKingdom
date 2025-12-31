using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using Newtonsoft.Json;

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
        LoadPlayers();
        LoadItems();
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

    void LoadItems()
    {
        string filePath = Path.Combine("Data", "Items.json");
        string json = SaCache.ReadText(filePath);

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError($"[StaticDataManager] Items.json not found or empty at: {filePath}");
            return;
        }

        try
        {
            var asset = new TextAsset(json);
            Data.ItemRepository.LoadFromJson(asset);
        }
        catch (Exception e)
        {
            Debug.LogError($"[StaticDataManager] Failed to initialize ItemRepository from Items.json: {e}");
        }
    }
}
