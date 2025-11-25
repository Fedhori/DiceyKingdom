using System.Collections.Generic;

namespace GameStats
{
    public enum StatOpKind
    {
        Add,
        Mult
    }

    public enum StatLayer
    {
        Permanent, // 레벨업, 패시브, 영구 성장 등
        Temporary, // 버프/디버프, 오라 등
    }

    // 선택 사항: 공통 스탯 키 상수 정의용
    public static class StatIds
    {
        public const string Score = "score";
        public const string Attack = "attack";

        public const string Defense = "defense";
        // 필요할 때마다 여기 추가해서 코드에서만 사용 (데이터는 그냥 문자열 사용해도 됨)
    }

    public sealed class StatModifier
    {
        public string StatId { get; }
        public StatOpKind OpKind { get; }
        public float Value { get; }
        public StatLayer Layer { get; }
        public object Source { get; }
        public int Priority { get; }

        public StatModifier(
            string statId,
            StatOpKind opKind,
            float value,
            StatLayer layer,
            object source,
            int priority = 0)
        {
            StatId = statId;
            OpKind = opKind;
            Value = value;
            Layer = layer;
            Source = source;
            Priority = priority;
        }
    }

    public sealed class StatSlot
    {
        readonly List<StatModifier> _modifiers = new();

        float _baseValue;
        bool _dirty = true;
        float _cachedFinal;

        public string StatId { get; }

        public float BaseValue
        {
            get => _baseValue;
            set
            {
                if (_baseValue.Equals(value)) return;
                _baseValue = value;
                _dirty = true;
            }
        }

        public float FinalValue
        {
            get
            {
                if (_dirty) Recalculate();
                return _cachedFinal;
            }
        }

        public IReadOnlyList<StatModifier> Modifiers => _modifiers;

        public StatSlot(string statId, float baseValue = 0f)
        {
            StatId = statId;
            _baseValue = baseValue;
        }

        public void AddModifier(StatModifier modifier)
        {
            if (modifier == null) return;
            _modifiers.Add(modifier);
            _dirty = true;
        }

        /// <summary>
        /// layer 혹은 source 기준으로 Modifier 제거. 둘 다 null이면 아무 것도 안 함.
        /// 반환값: 제거된 개수
        /// </summary>
        public int RemoveModifiers(StatLayer? layer = null, object source = null)
        {
            if (layer == null && source == null) return 0;

            int removed = _modifiers.RemoveAll(m =>
                (layer == null || m.Layer == layer.Value) &&
                (source == null || ReferenceEquals(m.Source, source)));

            if (removed > 0) _dirty = true;
            return removed;
        }

        public void ClearAllModifiers()
        {
            if (_modifiers.Count == 0) return;
            _modifiers.Clear();
            _dirty = true;
        }

        void Recalculate()
        {
            float addSum = 0f;
            float multSum = 0f;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                var m = _modifiers[i];
                if (m.OpKind == StatOpKind.Add)
                    addSum += m.Value;
                else
                    multSum += m.Value;
            }

            _cachedFinal = (_baseValue + addSum) * (1f + multSum);
            _dirty = false;
        }
    }

    public sealed class StatSet
    {
        readonly Dictionary<string, StatSlot> _slots = new();

        public IEnumerable<StatSlot> AllSlots => _slots.Values;

        public void SetBase(string statId, float baseValue)
        {
            if (!_slots.TryGetValue(statId, out var slot))
            {
                slot = new StatSlot(statId, baseValue);
                _slots.Add(statId, slot);
            }
            else
            {
                slot.BaseValue = baseValue;
            }
        }

        public float GetValue(string statId)
        {
            return _slots.TryGetValue(statId, out var slot)
                ? slot.FinalValue
                : 0f;
        }

        public StatSlot GetOrCreateSlot(string statId)
        {
            if (!_slots.TryGetValue(statId, out var slot))
            {
                slot = new StatSlot(statId);
                _slots.Add(statId, slot);
            }

            return slot;
        }

        public void AddModifier(StatModifier modifier)
        {
            if (modifier == null) return;
            var slot = GetOrCreateSlot(modifier.StatId);
            slot.AddModifier(modifier);
        }

        /// <summary>
        /// 전체 스탯에서 특정 source / layer 기준으로 Modifier 제거
        /// </summary>
        public int RemoveModifiers(StatLayer? layer = null, object source = null)
        {
            int totalRemoved = 0;
            foreach (var slot in _slots.Values)
                totalRemoved += slot.RemoveModifiers(layer, source);
            return totalRemoved;
        }
    }
}