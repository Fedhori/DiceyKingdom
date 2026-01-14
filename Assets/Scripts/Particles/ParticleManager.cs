using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ParticleManager : MonoBehaviour
{
    public static ParticleManager Instance { get; private set; }

    [Serializable]
    public sealed class Entry
    {
        public string key;
        public ParticleSystem prefab;
    }

    public const string BlockDestroyKey = "block.destroy";
    public const string ExplosionKey = "projectile.explosion";

    [SerializeField] private List<Entry> entries = new();

    readonly Dictionary<string, ParticleSystem> map = new(StringComparer.OrdinalIgnoreCase);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Rebuild();
    }

    public void Rebuild()
    {
        map.Clear();
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.key) || entry.prefab == null)
                continue;

            map[entry.key] = entry.prefab;
        }
    }

    public void PlayBlockDestroy(Vector3 position)
    {
        Play(BlockDestroyKey, position);
    }

    public void PlayExplosion(Vector3 position, float radius)
    {
        float speed = Mathf.Max(0f, radius * GameConfig.ExplosionSpeedPerRadius);
        float size = Mathf.Max(0f, radius * GameConfig.ExplosionSizePerRadius);
        Play(ExplosionKey, position, null, speed, size);
    }

    public void PlayExplosion(Vector3 position)
    {
        Play(ExplosionKey, position);
    }

    public void Play(string key, Vector3 position, Color? tint = null, float? startSpeed = null, float? startSize = null)
    {
        if (string.IsNullOrEmpty(key))
            return;

        if (!map.TryGetValue(key, out var prefab) || prefab == null)
        {
            Debug.LogWarning($"[ParticleManager] Missing particle prefab: {key}");
            return;
        }

        var instance = Instantiate(prefab, position, Quaternion.identity);
        var systems = instance.GetComponentsInChildren<ParticleSystem>(true);
        if (systems == null || systems.Length == 0)
        {
            Destroy(instance.gameObject);
            return;
        }

        if (tint.HasValue)
            ApplyTint(systems, tint.Value);

        if (startSpeed.HasValue || startSize.HasValue)
            ApplyOverrides(systems, startSpeed, startSize);

        float maxDuration = 0f;
        for (int i = 0; i < systems.Length; i++)
        {
            var system = systems[i];
            if (system == null)
                continue;

            system.Play(true);
            float duration = GetTotalDuration(system);
            if (duration > maxDuration)
                maxDuration = duration;
        }

        if (maxDuration <= 0f)
            maxDuration = 1f;

        Destroy(instance.gameObject, maxDuration);
    }

    static void ApplyTint(ParticleSystem[] systems, Color color)
    {
        for (int i = 0; i < systems.Length; i++)
        {
            var system = systems[i];
            if (system == null)
                continue;

            var main = system.main;
            main.startColor = color;
        }
    }

    static void ApplyOverrides(ParticleSystem[] systems, float? startSpeed, float? startSize)
    {
        for (int i = 0; i < systems.Length; i++)
        {
            var system = systems[i];
            if (system == null)
                continue;

            var main = system.main;
            if (startSpeed.HasValue)
                main.startSpeed = startSpeed.Value;
            if (startSize.HasValue)
                main.startSize = startSize.Value;
        }
    }

    static float GetTotalDuration(ParticleSystem system)
    {
        var main = system.main;
        float startDelay = GetMax(main.startDelay);
        float lifetime = GetMax(main.startLifetime);
        float duration = main.duration;
        return startDelay + duration + lifetime;
    }

    static float GetMax(ParticleSystem.MinMaxCurve curve)
    {
        switch (curve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return curve.constant;
            case ParticleSystemCurveMode.TwoConstants:
                return curve.constantMax;
            case ParticleSystemCurveMode.Curve:
                return curve.curve != null ? curve.curve.Evaluate(1f) : 0f;
            case ParticleSystemCurveMode.TwoCurves:
                return curve.curveMax != null ? curve.curveMax.Evaluate(1f) : 0f;
            default:
                return 0f;
        }
    }
}
