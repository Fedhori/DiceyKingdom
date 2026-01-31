using UnityEngine;

public sealed class ItemController : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private SpriteRenderer iconRenderer;

    ItemInstance item;
    float fireTimer;

    GameObject beamObject;
    BeamController beamController;

    void OnDisable()
    {
        beamController?.Stop();
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
        float damageScale = item.ConsumeNextProjectileDamageScale();
        var rng = GameManager.Instance != null ? GameManager.Instance.Rng : new System.Random();
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
                float noise = -randomAngle + (float)rng.NextDouble() * (randomAngle * 2f);
                shotDir = Quaternion.Euler(0f, 0f, noise) * shotDir;
            }
            ProjectileFactory.Instance.SpawnProjectile(pos, shotDir, item, damageScale);
        }
    }

    void SetupBeam(ItemInstance source)
    {
        ClearBeam();
        if (source == null)
            return;

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

        beamController = beamObject.GetComponent<BeamController>();
        if (beamController == null)
        {
            Debug.LogWarning("[ItemController] BeamController missing on beam prefab.");
            Destroy(beamObject);
            beamObject = null;
            return;
        }

        beamController.Initialize(source, firePoint);
    }

    bool IsBeamItem()
    {
        return item != null && item.BeamDuration > 0f && item.BeamThickness > 0f;
    }

    void FireBeam()
    {
        if (!IsBeamItem())
            return;

        if (beamController == null)
            SetupBeam(item);

        if (beamController == null)
            return;

        beamController.Fire();
    }

    void ClearBeam()
    {
        if (beamController != null)
            beamController.Stop();

        if (beamObject != null)
            Destroy(beamObject);

        beamObject = null;
        beamController = null;
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
        iconRenderer.enabled = false;
    }
}
