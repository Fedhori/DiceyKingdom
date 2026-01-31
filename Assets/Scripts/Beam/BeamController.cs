using UnityEngine;
using Data;

public sealed class BeamController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer beamRenderer;
    [SerializeField] private BoxCollider2D beamCollider;
    [SerializeField] private int overlapBufferSize = 128;

    ItemInstance item;
    Transform firePoint;
    float beamDuration;
    float beamBaseThickness;
    float beamCurrentThickness;
    float beamElapsed;
    float beamTickTimer;
    float beamLength;
    bool beamActive;
    float beamPulsePhase;
    float beamPulseSpeed;
    float beamFlickerPhase;
    float beamFlickerSpeed;
    Color beamBaseColor;
    readonly System.Random vfxRng = new System.Random();

    ContactFilter2D beamFilter;
    bool beamFilterInitialized;
    int blockLayerMask;
    Collider2D[] beamOverlapBuffer;

    void Awake()
    {
        InitializeFilter();
        EnsureBeamBuffer();
    }

    void OnDisable()
    {
        Stop();
    }

    public void Initialize(ItemInstance sourceItem, Transform sourceFirePoint)
    {
        item = sourceItem;
        firePoint = sourceFirePoint;
        beamDuration = item != null ? Mathf.Max(0f, item.BeamDuration) : 0f;
        beamBaseThickness = item != null ? Mathf.Max(0f, item.BeamThickness) : 0f;
        beamCurrentThickness = beamBaseThickness;

        beamBaseColor = beamRenderer != null ? beamRenderer.color : Color.white;
        InitializeBeamAnimation();

        SetBeamActive(false);
    }

    public void Fire()
    {
        if (!IsValid())
            return;

        beamElapsed = 0f;
        beamTickTimer = 0f;
        beamActive = true;
        UpdateBeamGeometry();
        SetBeamActive(true);
        ApplyBeamDamage();
    }

    public void Stop()
    {
        beamActive = false;
        SetBeamActive(false);
    }

    void Update()
    {
        if (!beamActive)
            return;

        if (!IsValid())
        {
            Stop();
            return;
        }

        float delta = Time.deltaTime;
        if (delta <= 0f)
            return;

        beamElapsed += delta;
        beamTickTimer += delta;

        UpdateBeamGeometry();

        float tickInterval = Mathf.Max(0f, GameConfig.DamageTickIntervalSeconds);
        if (tickInterval > 0f)
        {
            while (beamTickTimer >= tickInterval)
            {
                beamTickTimer -= tickInterval;
                ApplyBeamDamage();
            }
        }

        if (beamElapsed >= beamDuration)
            Stop();
    }

    bool IsValid()
    {
        if (item == null || firePoint == null)
            return false;

        if (beamDuration <= 0f || beamBaseThickness <= 0f)
            return false;

        return true;
    }

    void UpdateBeamGeometry()
    {
        if (firePoint == null)
            return;

        beamLength = GetBeamLength();
        if (beamLength <= 0f)
        {
            Stop();
            return;
        }

        float pulse = GetBeamPulseScale();
        beamCurrentThickness = beamBaseThickness * pulse;

        transform.position = firePoint.position + Vector3.up * (beamLength * 0.5f);
        transform.rotation = Quaternion.identity;

        if (beamRenderer != null)
        {
            beamRenderer.size = new Vector2(beamCurrentThickness, beamLength);
            beamRenderer.color = GetBeamFlickerColor();
        }

        if (beamCollider != null)
        {
            beamCollider.size = new Vector2(beamCurrentThickness, beamLength);
            beamCollider.offset = Vector2.zero;
        }
    }

    float GetBeamLength()
    {
        var blockManager = BlockManager.Instance;
        if (blockManager == null)
            return 0f;

        if (!blockManager.TryGetPlayAreaBounds(out var bounds))
            return 0f;

        return Mathf.Max(0f, bounds.max.y - firePoint.position.y);
    }

    void ApplyBeamDamage()
    {
        if (beamCollider == null || beamLength <= 0f || beamBaseThickness <= 0f)
            return;

        if (!beamFilterInitialized)
            return;

        var damageManager = DamageManager.Instance;
        if (damageManager == null)
            return;

        EnsureBeamBuffer();

        int hitCount = beamCollider.Overlap(beamFilter, beamOverlapBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            var hit = beamOverlapBuffer[i];
            if (hit == null)
                continue;

            var block = hit.GetComponent<BlockController>();
            if (block == null || block.Instance == null)
                continue;

            var context = new DamageContext(
                block,
                sourceItem: item,
                sourceType: DamageSourceType.ItemEffect,
                hitPosition: block.transform.position,
                applyStatusFromItem: true,
                sourceOwner: this,
                damageScale: Mathf.Max(0f, GameConfig.DamageTickIntervalSeconds),
                allowZeroDamage: true);

            damageManager.ApplyDamage(context);
        }
    }

    void InitializeFilter()
    {
        int blockLayer = LayerMask.NameToLayer("Block");
        if (blockLayer < 0)
        {
            Debug.LogWarning("[BeamController] Block layer not found.");
            return;
        }

        blockLayerMask = 1 << blockLayer;
        beamFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = blockLayerMask,
            useTriggers = true
        };

        beamFilterInitialized = true;
    }

    void InitializeBeamAnimation()
    {
        beamPulsePhase = NextRange(0f, Mathf.PI * 2f);
        beamFlickerPhase = NextRange(0f, 10f);

        float pulseMin = Mathf.Max(0f, GameConfig.BeamPulseHzMin);
        float pulseMax = Mathf.Max(pulseMin, GameConfig.BeamPulseHzMax);
        beamPulseSpeed = NextRange(pulseMin, pulseMax);

        float flickerMin = Mathf.Max(0f, GameConfig.BeamFlickerHzMin);
        float flickerMax = Mathf.Max(flickerMin, GameConfig.BeamFlickerHzMax);
        beamFlickerSpeed = NextRange(flickerMin, flickerMax);
    }

    float GetBeamPulseScale()
    {
        float percent = Mathf.Max(0f, GameConfig.BeamPulsePercent);
        if (percent <= 0f)
            return 1f;

        float t = Time.time * beamPulseSpeed * Mathf.PI * 2f + beamPulsePhase;
        float wave = Mathf.Sin(t);
        return Mathf.Max(0.01f, 1f + wave * percent);
    }

    Color GetBeamFlickerColor()
    {
        float minAlpha = Mathf.Clamp01(GameConfig.BeamFlickerMinAlpha);
        if (minAlpha > 1f)
            minAlpha = 1f;

        if (beamFlickerSpeed <= 0f)
        {
            var steady = beamBaseColor;
            steady.a *= minAlpha;
            return steady;
        }

        float noise = Mathf.PerlinNoise(beamFlickerPhase, Time.time * beamFlickerSpeed);
        float alphaScale = Mathf.Lerp(minAlpha, 1f, noise);
        var color = beamBaseColor;
        color.a *= alphaScale;
        return color;
    }

    void EnsureBeamBuffer()
    {
        int size = Mathf.Max(1, overlapBufferSize);
        if (beamOverlapBuffer == null || beamOverlapBuffer.Length != size)
            beamOverlapBuffer = new Collider2D[size];
    }

    void SetBeamActive(bool active)
    {
        if (beamRenderer != null)
            beamRenderer.enabled = active;

        if (beamCollider != null)
            beamCollider.enabled = active;
    }

    float NextRange(float min, float max)
    {
        if (max <= min)
            return min;
        return min + (float)vfxRng.NextDouble() * (max - min);
    }
}
