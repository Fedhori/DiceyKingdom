using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public sealed class DamageTrackingManager : MonoBehaviour
{
    public static DamageTrackingManager Instance { get; private set; }

    readonly Dictionary<string, ItemDamageRecord> damageByItem = new();

    struct ItemDamageRecord
    {
        public string ItemId;
        public double Damage;
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

    public void ResetForStage()
    {
        damageByItem.Clear();
    }

    public void RecordDamage(ItemInstance item, double damage)
    {
        if (item == null || damage <= 0d)
            return;

        string uniqueId = item.UniqueId;
        if (string.IsNullOrEmpty(uniqueId))
            return;

        if (!damageByItem.TryGetValue(uniqueId, out var record))
            record = new ItemDamageRecord { ItemId = item.Id, Damage = 0d };

        record.Damage += damage;
        damageByItem[uniqueId] = record;
    }

    public void LogStageDamage()
    {
        if (damageByItem.Count == 0)
        {
            Debug.Log("[DamageTracking] No damage recorded.");
            return;
        }

        var totalsByItemId = new Dictionary<string, double>();
        foreach (var record in damageByItem.Values)
        {
            if (string.IsNullOrEmpty(record.ItemId))
                continue;

            if (totalsByItemId.TryGetValue(record.ItemId, out var total))
                totalsByItemId[record.ItemId] = total + record.Damage;
            else
                totalsByItemId.Add(record.ItemId, record.Damage);
        }

        if (totalsByItemId.Count == 0)
        {
            Debug.Log("[DamageTracking] No item damage recorded.");
            return;
        }

        var sorted = new List<KeyValuePair<string, double>>(totalsByItemId);
        sorted.Sort((a, b) =>
        {
            int cmp = b.Value.CompareTo(a.Value);
            return cmp != 0 ? cmp : string.CompareOrdinal(a.Key, b.Key);
        });

        for (int i = 0; i < sorted.Count; i++)
        {
            var entry = sorted[i];
            string damageText = entry.Value.ToString("0.##", CultureInfo.InvariantCulture);
            Debug.Log($"{entry.Key}: {damageText} dmg");
        }
    }
}
