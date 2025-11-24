using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class FixedDataManager : MonoBehaviour
{
    public static FixedDataManager Instance { get; private set; }
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
    }

    void LoadStage()
    {
        string filePath = Path.Combine("Data", "Stages.json");
        string json = SaCache.ReadText(filePath);

        var root = JsonConvert.DeserializeObject<JObject>(json);
        if (root == null)
        {
            Debug.LogError($"[FixedDataManager] Failed to parse Stages.json: root object missing.");
            Stages.Add(new StageDto());
            return;
        }

        JToken stagesToken = root["stages"];
        if (!(stagesToken is JArray stagesArray))
        {
            Debug.LogError($"[FixedDataManager] Stages.json missing 'stages' array.");
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
}
