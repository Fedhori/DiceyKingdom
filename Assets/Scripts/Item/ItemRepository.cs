using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Data
{
    public static class ItemRepository
    {
        static readonly Dictionary<string, ItemDto> dict = new();
        static bool initialized;

        public static bool IsInitialized => initialized;

        public static IReadOnlyDictionary<string, ItemDto> All => dict;

        public static void LoadFromJson(TextAsset jsonAsset)
        {
            dict.Clear();
            initialized = false;

            if (jsonAsset == null)
            {
                Debug.LogError("[ItemRepository] json asset is null");
                return;
            }

            ItemRoot root;
            try
            {
                root = JsonConvert.DeserializeObject<ItemRoot>(jsonAsset.text);
            }
            catch (Exception e)
            {
                Debug.LogError($"[ItemRepository] json parse error: {e}");
                return;
            }

            if (root?.items == null)
            {
                Debug.LogError("[ItemRepository] json has no items array");
                return;
            }

            foreach (var dto in root.items)
            {
                if (dto == null)
                    continue;

                if (!dto.isValid)
                {
                    Debug.LogError($"[ItemRepository] Skipping invalid item definition. id='{dto.id ?? "(null)"}'.");
                    continue;
                }

                if (string.IsNullOrEmpty(dto.id))
                {
                    Debug.LogError("[ItemRepository] Item with empty id encountered. Skipped.");
                    continue;
                }

                dict[dto.id] = dto;
            }

            initialized = true;
        }

        public static bool TryGet(string id, out ItemDto dto)
        {
            if (!initialized || string.IsNullOrEmpty(id))
            {
                dto = null;
                return false;
            }

            return dict.TryGetValue(id, out dto);
        }

        [System.Serializable]
        class ItemRoot
        {
            public List<ItemDto> items;
        }
    }
}
