using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public sealed class DamageTrackingManager : MonoBehaviour
{
    public static DamageTrackingManager Instance { get; private set; }

    readonly Dictionary<string, ItemDamageRecord> damageByItem = new();

    struct ItemDamageRecord
    {
        public ItemInstance Item;
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
            record = new ItemDamageRecord { Item = item, Damage = 0d };
        else if (!ReferenceEquals(record.Item, item))
            record.Item = item;

        record.Damage += damage;
        damageByItem[uniqueId] = record;
    }

    public IReadOnlyList<ItemDamageSnapshot> GetItemDamageRecords()
    {
        if (damageByItem.Count == 0)
            return Array.Empty<ItemDamageSnapshot>();

        var results = new List<ItemDamageSnapshot>(damageByItem.Count);
        foreach (var record in damageByItem.Values)
        {
            if (record.Item == null)
                continue;

            results.Add(new ItemDamageSnapshot(record.Item, record.Damage));
        }

        return results;
    }

    public readonly struct ItemDamageSnapshot
    {
        public ItemInstance Item { get; }
        public double Damage { get; }

        public ItemDamageSnapshot(ItemInstance item, double damage)
        {
            Item = item;
            Damage = damage;
        }
    }
}
