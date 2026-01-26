using System;
using System.Collections.Generic;
using GameStats;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[Serializable]
public sealed class SaveData
{
    public SaveMeta meta = new();
    public SaveRun run = new();
    public SavePlayer player = new();
    public SaveInventory inventory = new();
    public SaveUpgradeInventory upgradeInventory = new();
}

[Serializable]
public sealed class SaveMeta
{
    public const int CurrentSchemaVersion = 1;

    public int schemaVersion = CurrentSchemaVersion;
    public string appVersion = string.Empty;
    public long timestampUtc;
    public int runSeed;
    public string checksum = string.Empty;
}

[Serializable]
public sealed class SaveRun
{
    public int stageIndex = -1;
}

[Serializable]
public sealed class SavePlayer
{
    public string playerId = string.Empty;
    public int currency;
    public List<SaveStatModifier> permanentStatModifiers = new();
}

[Serializable]
public sealed class SaveInventory
{
    public List<SaveItemSlot> slots = new();
}

[Serializable]
public sealed class SaveItemSlot
{
    public string itemId = string.Empty;
    public string itemUniqueId = string.Empty;
    public List<SaveStatModifier> permanentStatModifiers = new();
    public List<SaveUpgrade> upgrades = new();
}

[Serializable]
public sealed class SaveUpgradeInventory
{
    public List<SaveUpgrade> upgrades = new();
}

[Serializable]
public sealed class SaveUpgrade
{
    public string upgradeId = string.Empty;
    public string upgradeUniqueId = string.Empty;
}

[Serializable]
public sealed class SaveStatModifier
{
    public string statId = string.Empty;

    [JsonConverter(typeof(StringEnumConverter))]
    public StatOpKind op;

    public double value;

    [JsonConverter(typeof(StringEnumConverter))]
    public StatLayer layer;

    public string source = string.Empty;
    public int priority;
}
