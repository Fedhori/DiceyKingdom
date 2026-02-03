using System.Collections.Generic;
using System.IO;
using System.Text;
using Data;
using UnityEditor;
using UnityEngine;

public static class ItemUpgradeStatsTool
{
    const string ItemsPath = "Assets/StreamingAssets/Data/Items.json";
    const string UpgradesPath = "Assets/StreamingAssets/Data/Upgrades.json";

    [MenuItem("Tools/Stats/Item Upgrade Stats")]
    static void PrintStats()
    {
        var itemsAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(ItemsPath);
        var upgradesAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(UpgradesPath);
        string itemsFullPath = null;
        string upgradesFullPath = null;
        if (itemsAsset == null)
            itemsAsset = LoadStreamingAssetFallback(ItemsPath, out itemsFullPath);
        if (upgradesAsset == null)
            upgradesAsset = LoadStreamingAssetFallback(UpgradesPath, out upgradesFullPath);
        if (itemsAsset == null || upgradesAsset == null)
        {
            Debug.LogError($"[ItemUpgradeStatsTool] Missing data assets. Items='{ItemsPath}' (resolved='{itemsFullPath ?? "N/A"}'), Upgrades='{UpgradesPath}' (resolved='{upgradesFullPath ?? "N/A"}').");
            return;
        }

        ItemRepository.LoadFromJson(itemsAsset);
        UpgradeRepository.LoadFromJson(upgradesAsset);

        var items = ItemRepository.All.Values;
        var upgrades = UpgradeRepository.All.Values;

        var sb = new StringBuilder(512);
        sb.AppendLine("[Item/Upgrade Stats]");
        AppendItemStats(sb, items);
        AppendUpgradeStats(sb, upgrades);
        Debug.Log(sb.ToString());
    }

    static void AppendItemStats(StringBuilder sb, IEnumerable<ItemDto> items)
    {
        sb.AppendLine("Items:");
        AppendStats(sb, items, dto => dto.rarity, dto => dto.isNotSell);
    }

    static void AppendUpgradeStats(StringBuilder sb, IEnumerable<UpgradeDto> upgrades)
    {
        sb.AppendLine("Upgrades:");
        AppendStats(sb, upgrades, dto => dto.rarity, dto => dto.isNotSell);
    }

    static void AppendStats<T>(
        StringBuilder sb,
        IEnumerable<T> entries,
        System.Func<T, ItemRarity> getRarity,
        System.Func<T, bool> getIsNotSell)
    {
        int total = 0;
        var totalByRarity = CreateRarityCounter();
        var sellableByRarity = CreateRarityCounter();
        int sellableTotal = 0;

        foreach (var entry in entries)
        {
            total++;
            var rarity = getRarity(entry);
            totalByRarity[rarity]++;
            if (!getIsNotSell(entry))
            {
                sellableTotal++;
                sellableByRarity[rarity]++;
            }
        }

        sb.AppendLine($"- Total: {total}");
        sb.AppendLine($"- By Rarity: Common {totalByRarity[ItemRarity.Common]}, Uncommon {totalByRarity[ItemRarity.Uncommon]}, Rare {totalByRarity[ItemRarity.Rare]}");
        sb.AppendLine($"- Sellable Total (exclude isNotSell): {sellableTotal}");
        sb.AppendLine($"- Sellable By Rarity: Common {sellableByRarity[ItemRarity.Common]}, Uncommon {sellableByRarity[ItemRarity.Uncommon]}, Rare {sellableByRarity[ItemRarity.Rare]}");
    }

    static Dictionary<ItemRarity, int> CreateRarityCounter()
    {
        return new Dictionary<ItemRarity, int>
        {
            { ItemRarity.Common, 0 },
            { ItemRarity.Uncommon, 0 },
            { ItemRarity.Rare, 0 }
        };
    }

    static TextAsset LoadStreamingAssetFallback(string assetPath, out string resolvedPath)
    {
        resolvedPath = null;
        if (string.IsNullOrEmpty(assetPath))
            return null;

        string fullPath = Path.GetFullPath(assetPath);
        if (!File.Exists(fullPath))
        {
            string normalized = assetPath.Replace("Assets/", string.Empty).Replace("Assets\\", string.Empty);
            fullPath = Path.Combine(Application.dataPath, normalized);
            if (!File.Exists(fullPath))
                return null;
        }

        resolvedPath = fullPath;
        string text = File.ReadAllText(fullPath);
        return new TextAsset(text);
    }
}
