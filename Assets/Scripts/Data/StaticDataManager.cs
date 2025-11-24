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
        LoadNails();
    }

    void LoadStage()
    {
        string filePath = Path.Combine("Data", "Stages.json");
        string json = SaCache.ReadText(filePath);

        var root = JsonConvert.DeserializeObject<JObject>(json);
        if (root == null)
        {
            Debug.LogError("[StaticDataManager] Failed to parse Stages.json: root object missing.");
            Stages.Add(new StageDto());
            return;
        }

        JToken stagesToken = root["stages"];
        if (!(stagesToken is JArray stagesArray))
        {
            Debug.LogError("[StaticDataManager] Stages.json missing 'stages' array.");
            Stages.Add(new StageDto());
            return;
        }

        foreach (var jToken in stagesArray)
        {
            var stageJson = (JObject)jToken;
            var stage = stageJson.ToObject<StageDto>() ?? new StageDto();
            Stages.Add(stage);
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

    void LoadNails()
    {
        string filePath = Path.Combine("Data", "Nails.json");
        string json = SaCache.ReadText(filePath);

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError($"[StaticDataManager] Nails.json not found or empty at: {filePath}");
            return;
        }

        try
        {
            Data.NailRepository.InitializeFromJson(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[StaticDataManager] Failed to initialize NailRepository from Nails.json: {e}");
        }
    }
}
