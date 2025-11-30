using System.Collections.Generic;
using UnityEngine;

public class PinManager : MonoBehaviour
{
    public static PinManager Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] int rowCount = 5;
    [SerializeField] int columnCount = 5;
    [SerializeField] float pinRadius = 64f;

    [SerializeField] string spawnPinId = "pin.basic";
    string defaultPinId = "pin.basic";

    [Header("World Offset (center of grid in world space)")]
    [SerializeField] Vector2 centerOffset = Vector2.zero;

    readonly List<List<PinController>> pinsByRow = new();
    public IReadOnlyList<List<PinController>> PinsByRow => pinsByRow;

    // 기본핀 id 외부에서 참조용
    public string DefaultPinId => defaultPinId;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PinManager] Multiple instances detected. Overwriting Instance.");
        }

        Instance = this;
        InitializePinsGrid();
    }

    void Start()
    {
        GeneratePins();
    }

    void InitializePinsGrid()
    {
        pinsByRow.Clear();

        if (rowCount <= 0 || columnCount <= 0)
            return;

        for (int row = 0; row < rowCount; row++)
        {
            bool isOddRow = (row % 2) == 1;
            int colsInRow = isOddRow ? columnCount - 1 : columnCount;

            var rowList = new List<PinController>(colsInRow);
            for (int col = 0; col < colsInRow; col++)
                rowList.Add(null);

            pinsByRow.Add(rowList);
        }
    }

    void GeneratePins()
    {
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

        for (int row = 0; row < rowCount; row++)
        {
            bool isOddRow = (row % 2) == 1;
            int colsInRow = isOddRow ? columnCount - 1 : columnCount;

            for (int col = 0; col < colsInRow; col++)
            {
                PinFactory.Instance.SpawnPin(spawnPinId, row, col);
            }
        }
    }

    public Vector2 GetPinWorldPosition(int row, int column)
    {
        float dx = pinRadius * 2f;
        float dy = pinRadius * Mathf.Sqrt(3f);

        int centerRow = rowCount / 2;
        int centerCol = columnCount / 2;

        bool isOddRow = (row % 2) == 1;

        float baseX = (column - centerCol) * dx;
        if (isOddRow)
            baseX += pinRadius;

        float baseY = (centerRow - row) * dy;

        return new Vector2(baseX, baseY) + centerOffset;
    }

    public void RegisterPin(PinController pin, int row, int column)
    {
        if (pin == null)
            return;

        if (row < 0 || row >= pinsByRow.Count)
        {
            Debug.LogWarning($"[PinManager] RegisterPin: row {row} out of range.");
            return;
        }

        var rowList = pinsByRow[row];
        if (rowList == null)
        {
            Debug.LogWarning($"[PinManager] RegisterPin: rowList for row {row} is null.");
            return;
        }

        if (column < 0 || column >= rowList.Count)
        {
            Debug.LogWarning($"[PinManager] RegisterPin: column {column} out of range for row {row}.");
            return;
        }

        var existing = rowList[column];
        if (existing != null && existing != pin)
        {
            Debug.Log($"[PinManager] Replacing existing pin at ({row}, {column}) with a new one.");
            Destroy(existing.gameObject);
        }

        rowList[column] = pin;
    }

    public void UnregisterPin(PinController pin, int row, int column)
    {
        if (pin == null)
            return;

        if (row < 0 || row >= pinsByRow.Count)
            return;

        var rowList = pinsByRow[row];
        if (rowList == null)
            return;

        if (column < 0 || column >= rowList.Count)
            return;

        if (rowList[column] == pin)
            rowList[column] = null;
    }

    public void ResetAllPins()
    {
        for (int row = 0; row < pinsByRow.Count; row++)
        {
            var rowList = pinsByRow[row];
            if (rowList == null)
                continue;

            for (int col = 0; col < rowList.Count; col++)
            {
                var pin = rowList[col];
                if (pin == null)
                    continue;

                if (pin.Instance != null)
                {
                    pin.Instance.ResetData();
                }
            }
        }
    }

    // 기본핀("빈 슬롯")이 하나라도 있는지
    public bool HasBasicPinSlot()
    {
        if (string.IsNullOrEmpty(defaultPinId))
            return false;

        for (int row = 0; row < pinsByRow.Count; row++)
        {
            var rowList = pinsByRow[row];
            if (rowList == null)
                continue;

            for (int col = 0; col < rowList.Count; col++)
            {
                var pin = rowList[col];
                if (pin != null && pin.Instance != null && pin.Instance.Id == defaultPinId)
                    return true;
            }
        }

        return false;
    }

    // 기본핀 하나를 찾아서 구매한 pinId로 교체
    public bool TryReplaceBasicPin(string pinId)
    {
        if (PinFactory.Instance == null)
        {
            Debug.LogError("[PinManager] TryReplaceBasicPin failed. PinFactory.Instance is null.");
            return false;
        }

        if (string.IsNullOrEmpty(pinId) || string.IsNullOrEmpty(defaultPinId))
            return false;

        for (int row = 0; row < pinsByRow.Count; row++)
        {
            var rowList = pinsByRow[row];
            if (rowList == null)
                continue;

            for (int col = 0; col < rowList.Count; col++)
            {
                var pin = rowList[col];
                if (pin != null && pin.Instance != null && pin.Instance.Id == defaultPinId)
                {
                    PinFactory.Instance.SpawnPin(pinId, row, col);
                    return true;
                }
            }
        }

        return false;
    }
}
