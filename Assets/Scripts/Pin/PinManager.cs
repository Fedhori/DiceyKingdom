using System.Collections.Generic;
using UnityEngine;

public class PinManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] int rowCount = 5;
    [SerializeField] int columnCount = 5;
    [SerializeField] float pinRadius = 64f;

    [SerializeField] string defaultPinId = "pin.basic";

    [Header("World Offset (center of grid in world space)")]
    [SerializeField] Vector2 centerOffset = Vector2.zero;

    readonly List<List<PinController>> pinsByRow = new();
    public IReadOnlyList<List<PinController>> PinsByRow => pinsByRow;

    void Start()
    {
        GeneratePins();
    }

    void GeneratePins()
    {
        pinsByRow.Clear();

        if (PinFactory.Instance == null)
        {
            Debug.LogError("[PinManager] PinFactory.Instance is null. Cannot spawn pins.");
            return;
        }

        if (rowCount <= 0 || columnCount <= 0)
        {
            Debug.LogWarning("[PinManager] rowCount or columnCount <= 0. Nothing to generate.");
            return;
        }

        if (rowCount % 2 == 0 || columnCount % 2 == 0)
        {
            Debug.LogWarning(
                $"[PinManager] rowCount ({rowCount}) and columnCount ({columnCount}) are assumed to be odd for perfect centering.");
        }

        float dx = pinRadius * 2f;
        float dy = pinRadius * Mathf.Sqrt(3f);

        int centerRow = rowCount / 2;
        int centerCol = columnCount / 2;

        for (int row = 0; row < rowCount; row++)
        {
            bool isOddRow = (row % 2) == 1;
            int colsInRow = isOddRow ? columnCount - 1 : columnCount;

            var rowList = new List<PinController>(colsInRow);

            for (int col = 0; col < colsInRow; col++)
            {
                float baseX = (col - centerCol) * dx;
                if (isOddRow)
                    baseX += pinRadius;

                float baseY = (centerRow - row) * dy;

                Vector2 worldPos = new Vector2(baseX, baseY) + centerOffset;

                PinController pin = PinFactory.Instance.SpawnPin(defaultPinId, worldPos);
                rowList.Add(pin);
            }

            pinsByRow.Add(rowList);
        }
    }
}
