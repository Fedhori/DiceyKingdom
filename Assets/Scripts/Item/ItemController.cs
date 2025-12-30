using System.Collections.Generic;
using UnityEngine;

public sealed class ItemController : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private BulletFactory bulletFactory;

    readonly List<ItemRuntime> runtimes = new();

    public void BindItems(IReadOnlyList<ItemInstance> items, Transform attachTarget)
    {
        runtimes.Clear();
        if (items == null || items.Count == 0)
            return;

        for (int i = 0; i < items.Count; i++)
        {
            var inst = items[i];
            if (inst == null)
                continue;

            var rt = new ItemRuntime(inst);
            runtimes.Add(rt);
        }

        transform.SetParent(attachTarget, worldPositionStays: true);
        transform.localPosition = Vector3.zero;
    }

    void Update()
    {
        if (runtimes.Count == 0 || bulletFactory == null || firePoint == null)
            return;

        float dt = Time.deltaTime;
        for (int i = 0; i < runtimes.Count; i++)
        {
            var rt = runtimes[i];
            rt.Tick(dt, TryFire);
        }
    }

    void TryFire(ItemInstance inst)
    {
        if (inst == null || bulletFactory == null || firePoint == null)
            return;

        Vector3 pos = firePoint.position;
        Vector2 dir = Vector2.up;

        bulletFactory.SpawnBullet(pos, dir, inst);
    }

    sealed class ItemRuntime
    {
        public ItemInstance Inst { get; }
        float timer;

        public ItemRuntime(ItemInstance inst)
        {
            Inst = inst;
            timer = 0f;
        }

        public void Tick(float dt, System.Action<ItemInstance> onFire)
        {
            if (Inst == null || onFire == null)
                return;

            float interval = 1f / Mathf.Max(0.1f, Inst.AttackSpeed);
            timer += dt;
            if (timer >= interval)
            {
                timer -= interval;
                onFire.Invoke(Inst);
            }
        }
    }
}
