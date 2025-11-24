using System.Collections.Generic;
using UnityEngine;

public class NailManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] int rowCount = 5;
    [SerializeField] int columnCount = 5;
    [SerializeField] float nailRadius = 64f;

    [Header("Nail Settings")]
    [SerializeField] string defaultNailId = "nail.basic";

    [Header("World Offset (center of grid in world space)")]
    [SerializeField] Vector2 centerOffset = Vector2.zero;

    readonly List<List<NailController>> nailsByRow = new();
    public IReadOnlyList<List<NailController>> NailsByRow => nailsByRow;

    void Start()
    {
        GenerateNails();
    }

    void GenerateNails()
    {
        nailsByRow.Clear();

        if (NailFactory.Instance == null)
        {
            Debug.LogError("[NailManager] NailFactory.Instance is null. Cannot spawn nails.");
            return;
        }

        if (rowCount <= 0 || columnCount <= 0)
        {
            Debug.LogWarning("[NailManager] rowCount or columnCount <= 0. Nothing to generate.");
            return;
        }

        if (rowCount % 2 == 0 || columnCount % 2 == 0)
        {
            Debug.LogWarning(
                $"[NailManager] rowCount ({rowCount}) and columnCount ({columnCount}) are assumed to be odd for perfect centering.");
        }

        float dx = nailRadius * 2f;
        float dy = nailRadius * Mathf.Sqrt(3f);

        int centerRow = rowCount / 2;
        int centerCol = columnCount / 2;

        for (int row = 0; row < rowCount; row++)
        {
            bool isOddRow = (row % 2) == 1;
            int colsInRow = isOddRow ? columnCount - 1 : columnCount;

            var rowList = new List<NailController>(colsInRow);

            for (int col = 0; col < colsInRow; col++)
            {
                float baseX = (col - centerCol) * dx;
                if (isOddRow)
                    baseX += nailRadius;

                float baseY = (centerRow - row) * dy;

                Vector2 worldPos = new Vector2(baseX, baseY) + centerOffset;

                NailController nail = NailFactory.Instance.SpawnNail(defaultNailId, worldPos);
                rowList.Add(nail);
            }

            nailsByRow.Add(rowList);
        }
    }
}
