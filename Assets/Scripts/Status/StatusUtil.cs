using System;
using System.Collections.Generic;
using UnityEngine;
using Data;

public static class StatusUtil
{
    static readonly StatusEntry[] Entries =
    {
        new StatusEntry("freeze", BlockStatusType.Freeze, "freeze", dto => dto.freeze)
    };

    static readonly string[] KeysCache;

    static StatusUtil()
    {
        KeysCache = new string[Entries.Length];
        for (int i = 0; i < Entries.Length; i++)
            KeysCache[i] = Entries[i].Key;
    }

    public static IReadOnlyList<string> Keys => KeysCache;

    public static bool IsStatus(string statId)
    {
        if (string.IsNullOrEmpty(statId))
            return false;

        for (int i = 0; i < Entries.Length; i++)
        {
            if (Entries[i].Key == statId)
                return true;
        }

        return false;
    }

    public static bool TryGetStatusType(string statId, out BlockStatusType type)
    {
        for (int i = 0; i < Entries.Length; i++)
        {
            if (Entries[i].Key == statId)
            {
                type = Entries[i].Type;
                return true;
            }
        }

        type = BlockStatusType.Unknown;
        return false;
    }

    public static bool TryGetStatusKey(BlockStatusType type, out string statId)
    {
        for (int i = 0; i < Entries.Length; i++)
        {
            if (Entries[i].Type == type)
            {
                statId = Entries[i].Key;
                return true;
            }
        }

        statId = null;
        return false;
    }

    public static string GetKeywordId(string statId)
    {
        for (int i = 0; i < Entries.Length; i++)
        {
            if (Entries[i].Key == statId)
                return Entries[i].KeywordId;
        }

        return null;
    }

    public static int GetItemStatusBaseValue(ItemDto dto, string statId)
    {
        if (dto == null)
            return 0;

        for (int i = 0; i < Entries.Length; i++)
        {
            if (Entries[i].Key == statId)
                return Entries[i].GetBaseValue(dto);
        }

        return 0;
    }

    public static int GetItemStatusValue(ItemInstance item, string statId)
    {
        if (item == null || string.IsNullOrEmpty(statId))
            return 0;

        double raw = item.Stats.GetValue(statId);
        return Mathf.Max(0, Mathf.FloorToInt((float)raw));
    }

    struct StatusEntry
    {
        public readonly string Key;
        public readonly BlockStatusType Type;
        public readonly string KeywordId;
        public readonly Func<ItemDto, int> GetBaseValue;

        public StatusEntry(string key, BlockStatusType type, string keywordId, Func<ItemDto, int> getBaseValue)
        {
            Key = key;
            Type = type;
            KeywordId = keywordId;
            GetBaseValue = getBaseValue;
        }
    }
}
