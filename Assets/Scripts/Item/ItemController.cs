using UnityEngine;
using Data;

public sealed class ItemController : MonoBehaviour
{
    const float BeamTickInterval = 0.1f;

    [SerializeField] private Transform firePoint;
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private int beamOverlapBufferSize = 128;

    ItemInstance item;
    float fireTimer;

    // TODO - 분리해!!!
    GameObject beamObject;
    Transform beamRoot;
    SpriteRenderer beamRenderer;
    BoxCollider2D beamCollider;
    float beamDuration;
    float beamThickness;
    float beamBaseThickness;
    float beamCurrentThickness;
    float beamPulsePhase;
    float beamPulseSpeed;
    float beamFlickerPhase;
    float beamFlickerSpeed;
    float beamElapsed;
    float beamTickTimer;
    float beamLength;
    bool beamActive;
    int blockLayerMask;
    ContactFilter2D beamFilter;
    bool beamFilterInitialized;
    Collider2D[] beamOverlapBuffer;
    Color beamBaseColor;

    void Awake()
    {
        int blockLayer = LayerMask.NameToLayer("Block");
        if (blockLayer >= 0)
            blockLayerMask = 1 << blockLayer;
        else
            Debug.LogWarning("[ItemController] Block layer not found.");

        InitializeBeamFilter();
        EnsureBeamBuffer();
    }

    void OnDisable()
    {
        SetBeamActive(false);
    }

    public void BindItem(ItemInstance item, Transform attachTarget)
    {
        if (item == null || attachTarget == null)
            return;

        this.item = item;
        fireTimer = 0f;
        transform.SetParent(attachTarget, worldPositionStays: true);
        transform.localPosition = Vector3.zero;
        ApplyIcon(item);
        SetupBeam(item);
    }

    void Update()
    {
        if (item == null || firePoint == null)
            return;

        bool isBeam = IsBeamItem();
        if (!isBeam && ProjectileFactory.Instance == null)
            return;

        float interval = 1f / Mathf.Max(0.1f, item.AttackSpeed);
        float delta = Time.deltaTime;
        if (delta <= 0f)
            return;

        if (isBeam)
            UpdateBeam(delta);

        fireTimer += delta;
        if (fireTimer >= interval)
        {
            fireTimer -= interval;
            Fire();
        }
    }

    void Fire()
    {
        if (IsBeamItem())
        {
            FireBeam();
            return;
        }

        if (ProjectileFactory.Instance == null || string.IsNullOrEmpty(item.ProjectileKey))
            return;

        Vector3 pos = firePoint.position;
        Vector2 dir = Vector2.up;
        int count = Mathf.Max(1, item.PelletCount);
        float spread = Mathf.Max(0f, item.SpreadAngle);
        float randomAngle = Mathf.Max(0f, item.ProjectileRandomAngle);
        var player = PlayerManager.Instance?.Current;
        if (player != null)
            randomAngle *= Mathf.Max(0f, (float)player.ProjectileRandomAngleMultiplier);

        float total = spread * (count - 1);
        float start = -total * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float angle = start + spread * i;
            Vector2 shotDir = Quaternion.Euler(0f, 0f, angle) * dir;
            if (randomAngle > 0f)
            {
                float noise = Random.Range(-randomAngle, randomAngle);
                shotDir = Quaternion.Euler(0f, 0f, noise) * shotDir;
            }
            ProjectileFactory.Instance.SpawnProjectile(pos, shotDir, item);
        }
    }

    void SetupBeam(ItemInstance source)
    {
        ClearBeam();
        if (source == null)
            return;

        beamDuration = Mathf.Max(0f, source.BeamDuration);
        beamThickness = Mathf.Max(0f, source.BeamThickness);
        beamBaseThickness = beamThickness;
        beamCurrentThickness = beamBaseThickness;

        if (!IsBeamItem())
            return;

        var factory = BeamFactory.Instance;
        if (factory == null)
        {
            Debug.LogWarning("[ItemController] BeamFactory not found.");
            return;
        }

        var parent = firePoint != null ? firePoint : transform;
        beamObject = factory.SpawnBeam(parent);
        if (beamObject == null)
            return;

        beamRoot = beamObject.transform;
        beamRenderer = beamObject.GetComponent<SpriteRenderer>();
        beamCollider = beamObject.GetComponent<BoxCollider2D>();

        if (beamRenderer == null)
            Debug.LogWarning("[ItemController] Beam SpriteRenderer missing.");
        if (beamCollider == null)
            Debug.LogWarning("[ItemController] Beam BoxCollider2D missing.");

        beamBaseColor = beamRenderer != null ? beamRenderer.color : Color.white;
        InitializeBeamAnimation();

        SetBeamActive(false);
    }

    bool IsBeamItem()
    {
        return item != null && item.BeamDuration > 0f && item.BeamThickness > 0f;
    }

    void FireBeam()
    {
        if (!IsBeamItem())
            return;

        if (beamObject == null)
            SetupBeam(item);

        if (beamObject == null)
            return;

        beamElapsed = 0f;
        beamTickTimer = 0f;
        beamActive = true;
        UpdateBeamGeometry();
        SetBeamActive(true);
        ApplyBeamDamage();
    }

    void UpdateBeam(float delta)
    {
        if (!beamActive || beamObject == null)
            return;

        beamElapsed += delta;
        beamTickTimer += delta;

        UpdateBeamGeometry();

        while (beamTickTimer >= BeamTickInterval)
        {
            beamTickTimer -= BeamTickInterval;
            ApplyBeamDamage();
        }

        if (beamElapsed >= beamDuration)
        {
            beamActive = false;
            SetBeamActive(false);
        }
    }

    void UpdateBeamGeometry()
    {
        if (beamRoot == null || firePoint == null)
            return;

        beamLength = GetBeamLength();
        if (beamLength <= 0f)
        {
            beamActive = false;
            SetBeamActive(false);
            return;
        }

        float pulse = GetBeamPulseScale();
        float thickness = beamBaseThickness * pulse;
        beamCurrentThickness = thickness;

        beamRoot.localPosition = new Vector3(0f, beamLength * 0.5f, 0f);
        beamRoot.localRotation = Quaternion.identity;

        if (beamRenderer != null)
        {
            beamRenderer.size = new Vector2(thickness, beamLength);
            beamRenderer.color = GetBeamFlickerColor();
        }

        if (beamCollider != null)
        {
            beamCollider.size = new Vector2(thickness, beamLength);
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
        if (beamRoot == null || beamLength <= 0f || beamBaseThickness <= 0f)
            return;

        if (blockLayerMask == 0 || beamCollider == null)
            return;

        var damageManager = DamageManager.Instance;
        if (damageManager == null)
            return;

        EnsureBeamBuffer();
        InitializeBeamFilter();

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
                baseDamage: null,
                sourceItem: item,
                sourceType: DamageSourceType.ItemEffect,
                hitPosition: block.transform.position,
                allowOverflow: true,
                applyStatusFromItem: true,
                sourceOwner: this,
                damageScale: 0.1f,
                allowZeroDamage: true);

            damageManager.ApplyDamage(context);
        }
    }

    void InitializeBeamFilter()
    {
        if (beamFilterInitialized || blockLayerMask == 0)
            return;

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
        beamPulsePhase = Random.Range(0f, Mathf.PI * 2f);
        beamFlickerPhase = Random.Range(0f, 10f);

        float pulseMin = Mathf.Max(0f, GameConfig.BeamPulseHzMin);
        float pulseMax = Mathf.Max(pulseMin, GameConfig.BeamPulseHzMax);
        beamPulseSpeed = Random.Range(pulseMin, pulseMax);

        float flickerMin = Mathf.Max(0f, GameConfig.BeamFlickerHzMin);
        float flickerMax = Mathf.Max(flickerMin, GameConfig.BeamFlickerHzMax);
        beamFlickerSpeed = Random.Range(flickerMin, flickerMax);
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

    void SetBeamActive(bool active)
    {
        if (beamRenderer != null)
            beamRenderer.enabled = active;

        if (beamCollider != null)
            beamCollider.enabled = active;
    }

    void ClearBeam()
    {
        if (beamObject != null)
            Destroy(beamObject);

        beamObject = null;
        beamRoot = null;
        beamRenderer = null;
        beamCollider = null;
        beamActive = false;
        beamElapsed = 0f;
        beamTickTimer = 0f;
        beamLength = 0f;
        beamBaseThickness = 0f;
        beamCurrentThickness = 0f;
    }

    void EnsureBeamBuffer()
    {
        int size = Mathf.Max(1, beamOverlapBufferSize);
        if (beamOverlapBuffer == null || beamOverlapBuffer.Length != size)
            beamOverlapBuffer = new Collider2D[size];
    }

    void ApplyIcon(ItemInstance source)
    {
        if (source == null)
            return;

        if (iconRenderer == null)
            iconRenderer = GetComponent<SpriteRenderer>();

        if (iconRenderer == null)
            return;

        var sprite = SpriteCache.GetItemSprite(source.Id);
        iconRenderer.sprite = sprite;
        iconRenderer.enabled = sprite != null;
    }
}
