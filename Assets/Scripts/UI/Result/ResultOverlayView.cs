using System.Collections.Generic;
using UnityEngine;

public sealed class ResultOverlayView : MonoBehaviour
{
    [SerializeField] private Transform contentRoot;
    [SerializeField] private ResultDamageRow rowPrefab;

    readonly List<ResultDamageRow> rows = new();

    public void ClearRows()
    {
        for (int i = rows.Count - 1; i >= 0; i--)
        {
            var row = rows[i];
            if (row != null)
                Destroy(row.gameObject);
        }
        rows.Clear();
    }

    public void BuildRows(IReadOnlyList<DamageTrackingManager.ItemDamageSnapshot> records, double maxDamage)
    {
        ClearRows();

        if (records == null || records.Count == 0 || rowPrefab == null || contentRoot == null)
            return;

        for (int i = 0; i < records.Count; i++)
        {
            var record = records[i];
            if (record.Item == null || record.Damage <= 0d)
                continue;

            var row = Instantiate(rowPrefab, contentRoot);
            row.Bind(record.Item, record.Damage, maxDamage);
            rows.Add(row);
        }
    }
}
