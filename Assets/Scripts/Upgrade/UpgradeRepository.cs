using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Data
{
    public static class UpgradeRepository
    {
        static readonly Dictionary<string, UpgradeDto> dict = new();
        static bool initialized;

        public static bool IsInitialized => initialized;
        public static IReadOnlyDictionary<string, UpgradeDto> All => dict;

        public static void LoadFromJson(TextAsset jsonAsset)
        {
            dict.Clear();
            initialized = false;

            if (jsonAsset == null)
            {
                Debug.LogError("[UpgradeRepository] json asset is null");
                return;
            }

            UpgradeRoot root;
            try
            {
                root = JsonConvert.DeserializeObject<UpgradeRoot>(jsonAsset.text);
            }
            catch (Exception e)
            {
                Debug.LogError($"[UpgradeRepository] json parse error: {e}");
                return;
            }

            if (root?.upgrades == null)
            {
                Debug.LogError("[UpgradeRepository] json has no upgrades array");
                return;
            }

            foreach (var dto in root.upgrades)
            {
                if (dto == null)
                    continue;

                if (!dto.isValid)
                {
                    Debug.LogError($"[UpgradeRepository] Skipping invalid upgrade definition. id='{dto.id ?? "(null)"}'.");
                    continue;
                }

                if (string.IsNullOrEmpty(dto.id))
                {
                    Debug.LogError("[UpgradeRepository] Upgrade with empty id encountered. Skipped.");
                    continue;
                }

                dict[dto.id] = dto;
            }

            initialized = true;
        }

        public static bool TryGet(string id, out UpgradeDto dto)
        {
            if (!initialized || string.IsNullOrEmpty(id))
            {
                dto = null;
                return false;
            }

            return dict.TryGetValue(id, out dto);
        }
    }
}
