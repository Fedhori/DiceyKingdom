using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GameStats;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

/* 핀 추가 체크리스트
 1. Pins.json에 값 추가
 2. pin 테이블 - {id}.name (핀 이름), {id}.effect{index} (핀 효과) 스트링키 추가
 */

/* {id}.effect{index} 작성 체크리스트
 1. condition은 하나이므로, 할당한 변수명을 arguments로 그대로 작성 - e.g) hits
 2. effect는 여러개일 수 있으므로, 할당한 변수명{index} - e.g) value0 형태로 넣을 것
 3. argument로 쓰이는 신규 파라미터가 추가되었을 경우에는 BuildRuleArgs() 함수에 대응해둘 것
 */

namespace Data
{
    // enum 첫 값은 항상 Unknown → 잘못된 입력 검증용
    public enum PinTriggerType
    {
        Unknown = 0,
        OnBallHit,
        OnBallDestroyed,
        OnRoundFinished,
    }

    public enum PinConditionKind
    {
        Unknown = 0,
        Always,
        Charge,
        RoundCount,
    }

    public enum PinEffectType
    {
        Unknown = 0,
        ModifyPlayerStat,
        ModifySelfStat,
        AddVelocity,
        IncreaseSize,
        AddScore,
        AddCurrency,
        DestroySelf,
        ReviveBall,
    }

    [Serializable]
    public sealed class PinConditionDto
    {
        // JSON: "conditionKind": "Always" | "Charge"
        [JsonConverter(typeof(StringEnumConverter))]
        public PinConditionKind conditionKind;

        // Charge 용: N회마다 발동. Always일 땐 무시.
        public int hits;
        public int round;
    }

    [Serializable]
    public sealed class PinEffectDto
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public PinEffectType effectType;

        public string statId;

        [JsonConverter(typeof(StringEnumConverter))]
        public StatOpKind effectMode;

        public float value;
        public bool temporary = true;
    }

    [Serializable]
    public sealed class PinRuleDto
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public PinTriggerType triggerType;

        // 1 트리거 - 1 조건 - N 이펙트
        public PinConditionDto condition;
        public List<PinEffectDto> effects;
        public int maxPerRound;
    }

    [Serializable]
    public sealed class PinDto
    {
        public string id;
        public float scoreMultiplier = 1f;

        public bool isNotSell = false;
        public int price;

        public List<PinRuleDto> rules;

        [JsonIgnore] public bool isValid = true;

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            if (rules == null)
                rules = new List<PinRuleDto>();

            // id 필수
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("[PinDto] id is null or empty.");
                isValid = false;
                return;
            }

            // 판매 가능인데 price <= 0 이면 에러
            if (!isNotSell && price <= 0)
            {
                Debug.LogError(
                    $"[PinDto] '{id}': isNotSell=false 인데 price <= 0 입니다. (price={price})"
                );
                isValid = false;
            }

            // isNotSell인데 price != 0이면 0으로 강제
            if (isNotSell && price != 0)
            {
                Debug.LogError(
                    $"[PinDto] '{id}': isNotSell=true 인데 price={price} 입니다. 0으로 강제 설정합니다."
                );
                price = 0;
            }

            int chargeConditionCount = 0;

            if (rules.Count > GameConfig.MaxRuleCount)
            {
                Debug.LogError("[PinDto] rules.Count > GameConfig.MaxRuleCount");
                isValid = false;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule == null)
                {
                    Debug.LogError($"[PinDto] '{id}': rules[{i}] 가 null 입니다.");
                    isValid = false;
                    continue;
                }

                // 트리거 검증
                if (rule.triggerType == PinTriggerType.Unknown)
                {
                    Debug.LogError(
                        $"[PinDto] '{id}': rules[{i}].triggerType 가 Unknown 입니다. OnBallHit/OnBallDestroyed 중 하나를 사용하세요."
                    );
                    isValid = false;
                }

                // effects 필수
                if (rule.effects == null || rule.effects.Count == 0)
                {
                    Debug.LogError(
                        $"[PinDto] '{id}': rules[{i}] 에 effects가 비어 있습니다. 최소 1개 이상 필요합니다."
                    );
                    isValid = false;
                }

                // condition 필수
                var cond = rule.condition;
                if (cond == null)
                {
                    Debug.LogError(
                        $"[PinDto] '{id}': rules[{i}].condition 이 null 입니다."
                    );
                    isValid = false;
                    continue;
                }

                if (cond.conditionKind == PinConditionKind.Unknown)
                {
                    Debug.LogError(
                        $"[PinDto] '{id}': rules[{i}].condition.conditionKind 가 Unknown 입니다. Always/Charge 중 하나를 사용하세요."
                    );
                    isValid = false;
                    continue;
                }

                if (cond.conditionKind == PinConditionKind.Charge)
                {
                    if (cond.hits <= 0)
                    {
                        Debug.LogError(
                            $"[PinDto] '{id}': rules[{i}].condition.hits <= 0 입니다. Charge 조건에는 1 이상이 필요합니다."
                        );
                        isValid = false;
                    }

                    if (rule.triggerType != PinTriggerType.OnBallHit)
                    {
                        Debug.LogError(
                            $"[PinDto] '{id}': Charge 조건은 OnBallHit 트리거에서만 사용할 수 있습니다. pin='{id}', rule[{i}].triggerType='{rule.triggerType}'."
                        );
                        isValid = false;
                    }

                    chargeConditionCount++;
                }
            }

            if (chargeConditionCount > 1)
            {
                Debug.LogError(
                    $"[PinDto] '{id}': Charge 조건이 1개를 초과했습니다. 핀당 Charge 1개만 허용됩니다."
                );
                isValid = false;
            }
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

                    if (!dto.isValid)
                    {
                        Debug.LogError(
                            $"[PinRepository] Skipping invalid pin definition. id='{dto.id ?? "(null)"}'."
                        );
                        continue;
                    }

                    if (string.IsNullOrEmpty(dto.id))
                    {
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