using System;
using System.Collections.Generic;

namespace GameStats
{
    public enum StatOpKind
    {
        Add,
        Mult,
        Override
    }

    public enum StatLayer
    {
        Permanent,
        Temporary,
        Owned,
        Upgrade,
    }
    
    public static class PlayerStatIds
    {
        public const string Power = "power";
        public const string CriticalChance = "criticalChance";
        public const string CriticalMultiplier = "criticalMultiplier";
        public const string MoveSpeed = "moveSpeed";
        public const string ProjectileSizeMultiplier = "projectileSizeMultiplier";
        public const string ProjectileRandomAngleMultiplier = "projectileRandomAngleMultiplier";
        public const string ProjectileDamageMultiplier = "projectileDamageMultiplier";
        public const string PierceBonus = "pierceBonus";
        public const string WallBounceCount = "wallBounceCount";
        public const string IsDryIceEnabled = "isDryIceEnabled";
        public const string BaseIncomeBonus = "baseIncomeBonus";
    }

    public static class ItemStatIds
    {
        public const string DamageMultiplier = "damageMultiplier";
        public const string AttackSpeed = "attackSpeed";
        public const string CriticalChanceMultiplier = "criticalChanceMultiplier";
        public const string ProjectileSizeMultiplier = "projectileSizeMultiplier";
        public const string ProjectileRandomAngle = "projectileRandomAngle";
        public const string ProjectileIsHoming = "projectileIsHoming";
        public const string ProjectileExplosionRadius = "projectileExplosionRadius";
        public const string Pierce = "pierce";
        public const string SellValueBonus = "sellValueBonus";
    }

    public sealed class StatModifier
    {
        public string StatId { get; }
        public StatOpKind OpKind { get; }
        public double Value { get; }
        public StatLayer Layer { get; }
        public string Source { get; }
        public int Priority { get; }

        public StatModifier(
            string statId,
            StatOpKind opKind,
            double value,
            StatLayer layer,
            string source,
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

        double _baseValue;
        bool _dirty = true;
        double _cachedFinal;

        double? _minValue;
        double? _maxValue;

        public string StatId { get; }

        public double? MinValue => _minValue;
        public double? MaxValue => _maxValue;

        public double BaseValue
        {
            get => _baseValue;
            set
            {
                if (_baseValue.Equals(value)) return;
                _baseValue = value;
                _dirty = true;
            }
        }

        public double FinalValue
        {
            get
            {
                if (_dirty) Recalculate();
                return _cachedFinal;
            }
        }

        public IReadOnlyList<StatModifier> Modifiers => _modifiers;

        public StatSlot(string statId, double baseValue = 0f, double? minValue = null, double? maxValue = null)
        {
            StatId = statId;
            _baseValue = baseValue;
            SetBounds(minValue, maxValue);
        }

        public void SetBounds(double? minValue, double? maxValue)
        {
            if (minValue.HasValue && maxValue.HasValue && minValue.Value > maxValue.Value)
                (minValue, maxValue) = (maxValue, minValue);

            if (_minValue == minValue && _maxValue == maxValue)
                return;

            _minValue = minValue;
            _maxValue = maxValue;
            _dirty = true;
        }

        public void AddModifier(StatModifier modifier)
        {
            if (modifier == null) return;
            _modifiers.Add(modifier);
            _dirty = true;
        }

        public int RemoveModifiers(StatLayer? layer = null, string source = null)
        {
            if (layer == null && source == null) return 0;

            int removed = _modifiers.RemoveAll(m =>
                (layer == null || m.Layer == layer.Value) &&
                (source == null || string.Equals(m.Source, source, StringComparison.Ordinal)));

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
            double addSum = 0f;
            double multSum = 0f;
            double? overrideValue = null;
            int overridePriority = int.MinValue;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                var m = _modifiers[i];
                switch (m.OpKind)
                {
                    case StatOpKind.Add:
                        addSum += m.Value;
                        break;
                    case StatOpKind.Mult:
                        multSum += m.Value;
                        break;
                    case StatOpKind.Override:
                        if (!overrideValue.HasValue || m.Priority >= overridePriority)
                        {
                            overrideValue = m.Value;
                            overridePriority = m.Priority;
                        }
                        break;
                }
            }

            double v = overrideValue ?? (_baseValue + addSum) * (1f + multSum);

            if (_minValue.HasValue)
                v = Math.Max(_minValue.Value, v);

            if (_maxValue.HasValue)
                v = Math.Min(_maxValue.Value, v);

            _cachedFinal = v;
            _dirty = false;
        }
    }

    public sealed class StatSet
    {
        readonly Dictionary<string, StatSlot> _slots = new();

        public IEnumerable<StatSlot> AllSlots => _slots.Values;

        public static event Action<StatSet, string> OnAnyStatChanged;
        public event Action<string> OnStatChanged;

        public void SetBase(string statId, double baseValue, double? minValue = null, double? maxValue = null)
        {
            if (!_slots.TryGetValue(statId, out var slot))
            {
                slot = new StatSlot(statId, baseValue, minValue, maxValue);
                _slots.Add(statId, slot);
                NotifyChanged(statId);
                return;
            }

            slot.BaseValue = baseValue;
            slot.SetBounds(minValue, maxValue);
            NotifyChanged(statId);
        }

        public double GetValue(string statId)
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
            NotifyChanged(modifier.StatId);
        }

        public int RemoveModifiers(StatLayer? layer = null, string source = null)
        {
            int totalRemoved = 0;
            foreach (var slot in _slots.Values)
            {
                int removed = slot.RemoveModifiers(layer, source);
                if (removed > 0)
                    NotifyChanged(slot.StatId);
                totalRemoved += removed;
            }
            return totalRemoved;
        }

        public int RemoveModifiers(string statId, StatLayer? layer = null, string source = null)
        {
            if (string.IsNullOrEmpty(statId))
                return 0;

            if (!_slots.TryGetValue(statId, out var slot))
                return 0;

            int removed = slot.RemoveModifiers(layer, source);
            if (removed > 0)
                NotifyChanged(statId);
            return removed;
        }

        void NotifyChanged(string statId)
        {
            if (string.IsNullOrEmpty(statId))
                return;

            OnStatChanged?.Invoke(statId);
            OnAnyStatChanged?.Invoke(this, statId);
        }
    }
}
