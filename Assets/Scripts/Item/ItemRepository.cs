using System.Collections.Generic;
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

            ItemListWrapper wrapper;
            try
            {
                wrapper = JsonUtility.FromJson<ItemListWrapper>(jsonAsset.text);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ItemRepository] json parse error: {e}");
                return;
            }

            if (wrapper?.items == null)
            {
                Debug.LogError("[ItemRepository] json has no items array");
                return;
            }

            foreach (var dto in wrapper.items)
            {
                if (dto == null || string.IsNullOrEmpty(dto.id))
                    continue;

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
        class ItemListWrapper
        {
            public List<ItemDto> items;
        }
    }
}
