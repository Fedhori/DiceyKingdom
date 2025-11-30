using System;
using System.Collections.Generic;
using System.Runtime.Serialization;    // ★ OnDeserialized 에 필요
using Newtonsoft.Json;
using UnityEngine;

namespace Data
{
    [Serializable]
    public sealed class PinEffectDto
    {
        public string eventId;
        public string type;
        public string statId;
        public string mode;
        public float value;
        public bool temporary = true;
    }

    [Serializable]
    public sealed class PinDto
    {
        public string id;
        public float scoreMultiplier = 1f;

        /// <summary>
        /// -1: 항상 발동, 1 이상: 해당 히트 수마다 발동
        /// </summary>
        public int hitsToTrigger = -1;

        /// <summary>
        /// true면 상점에서 팔 수 없음
        /// </summary>
        public bool isNotSell = false;

        /// <summary>
        /// 상점 가격. isNotSell == false 인 경우에만 의미 있음.
        /// </summary>
        public int price;

        public List<PinEffectDto> effects;

        /// <summary>
        /// 역직렬화/검증 결과 유효한지 여부
        /// </summary>
        [JsonIgnore]
        public bool isValid = true;

        // ★ Json.NET 역직렬화 완료 후 자동 호출
        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            // 기본 방어
            if (effects == null)
                effects = new List<PinEffectDto>();

            // 1) id 필수
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("[PinDto] id is null or empty.");
                isValid = false;
                return;
            }

            // 2) 판매 가능(isNotSell == false)인데 price <= 0 이면 잘못된 데이터로 간주
            if (!isNotSell && price <= 0)
            {
                Debug.LogError(
                    $"[PinDto] '{id}': isNotSell=false 인데 price <= 0 입니다. (price={price})"
                );
                isValid = false;
            }

            // 3) 팔 수 없는 핀인데 price != 0 이면 경고 + 0으로 정규화 (이건 스킵까진 안 하고 자동보정)
            if (isNotSell && price != 0)
            {
                Debug.LogError(
                    $"[PinDto] '{id}': isNotSell=true 인데 price={price} 입니다. 0으로 강제 설정합니다."
                );
                price = 0;
            }

            // 4) hitsToTrigger 규칙: 0은 의미가 애매하니 금지. -1 또는 1 이상만 허용.
            if (hitsToTrigger == 0)
            {
                Debug.LogError(
                    $"[PinDto] '{id}': hitsToTrigger == 0 은 허용하지 않습니다. -1 또는 1 이상을 사용하세요. -1로 강제 설정합니다."
                );
                hitsToTrigger = -1;
            }

            // 여기서 isValid=false 로 마킹된 경우 Repository 단계에서 스킵됨.
        }
    }

    [Serializable]
    public sealed class PinRoot
    {
        public List<PinDto> pins;
    }

    public static class PinRepository
    {
        static readonly Dictionary<string, PinDto> map = new();
        static bool initialized;

        public static bool IsInitialized => initialized;
        public static IEnumerable<PinDto> All => map.Values;

        public static void InitializeFromJson(string json)
        {
            map.Clear();

            PinRoot root;
            try
            {
                root = JsonConvert.DeserializeObject<PinRoot>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PinRepository] Failed to deserialize Pins.json: {e}");
                initialized = false;
                return;
            }

            if (root?.pins != null)
            {
                foreach (var dto in root.pins)
                {
                    if (dto == null)
                        continue;

                    // OnDeserialized에서 유효하지 않다고 마킹된 데이터는 스킵
                    if (!dto.isValid)
                    {
                        Debug.LogError(
                            $"[PinRepository] Skipping invalid pin definition. id='{dto.id ?? "(null)"}'."
                        );
                        continue;
                    }

                    if (string.IsNullOrEmpty(dto.id))
                    {
                        // 여기까지 왔으면 사실상 isValid=true인데 id가 비었을 일은 거의 없지만, 추가 방어
                        Debug.LogError("[PinRepository] Pin with empty id encountered. Skipped.");
                        continue;
                    }

                    map[dto.id] = dto;
                }
            }

            initialized = true;
            Debug.Log($"[PinRepository] Loaded {map.Count} pin definitions.");
        }

        public static bool TryGet(string id, out PinDto dto)
        {
            if (!initialized)
            {
                Debug.LogError("[PinRepository] Not initialized.");
                dto = null;
                return false;
            }

            return map.TryGetValue(id, out dto);
        }

        public static PinDto GetOrThrow(string id)
        {
            if (!initialized)
                throw new InvalidOperationException("[PinRepository] Not initialized.");

            if (!map.TryGetValue(id, out var dto) || dto == null)
                throw new KeyNotFoundException($"[PinRepository] Pin id not found: {id}");

            return dto;
        }
    }
}
