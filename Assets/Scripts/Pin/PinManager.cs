using System.Collections.Generic;
using UnityEngine;

public class PinManager : MonoBehaviour
{
    public static PinManager Instance { get; private set; }

    [Header("Grid Settings")] [SerializeField]
    int rowCount = 5;

    [SerializeField] int columnCount = 5;
    [SerializeField] float pinRadius = 64f;

    [SerializeField] private string spawnPinId = GameConfig.BasicPinId;
    string defaultPinId = GameConfig.BasicPinId;

    [Header("World Offset (center of grid in world space)")] [SerializeField]
    Vector2 centerOffset = Vector2.zero;

    readonly List<List<PinController>> pinsByRow = new();
    public IReadOnlyList<List<PinController>> PinsByRow => pinsByRow;

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
                PinFactory.Instance.SpawnPin(spawnPinId, row, col, 0);
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

        if (!IsValidCell(row, column))
        {
            Debug.LogWarning($"[PinManager] RegisterPin: ({row}, {column}) out of range.");
            return;
        }

        var rowList = pinsByRow[row];
        var existing = rowList[column];
        if (existing != null && existing != pin)
            Destroy(existing.gameObject);

        rowList[column] = pin;
    }

    public void UnregisterPin(PinController pin, int row, int column)
    {
        if (pin == null)
            return;

        if (!IsValidCell(row, column))
            return;

        var rowList = pinsByRow[row];
        if (rowList == null)
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
                    pin.Instance.ResetData(0);
            }
        }
    }

    bool IsValidCell(int row, int col)
    {
        if (row < 0 || row >= pinsByRow.Count)
            return false;

        var rowList = pinsByRow[row];
        if (rowList == null)
            return false;

        if (col < 0 || col >= rowList.Count)
            return false;

        return true;
    }

    public bool GetBasicPinSlot(out int x, out int y)
    {
        x = 0;
        y = 0;

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
                {
                    x = row;
                    y = col;
                    return true;
                }
            }
        }

        return false;
    }

    public bool TryReplace(string pinId, int x, int y)
    {
        if (PinFactory.Instance == null)
        {
            Debug.LogError("[PinManager] TryReplace failed. PinFactory.Instance is null.");
            return false;
        }

        if (string.IsNullOrEmpty(pinId) || string.IsNullOrEmpty(defaultPinId))
            return false;

        var rowList = pinsByRow[x];
        if (rowList == null)
            return false;

        var pin = rowList[y];
        if (pin != null && pin.Instance != null)
        {
            PinFactory.Instance.SpawnPin(pinId, x, y, pin.Instance.HitCount);
            return true;
        }

        return false;
    }

    public void SwapPins(PinController dragging, PinController target)
    {
        if (dragging == null || target == null || dragging == target)
            return;

        int rowA = dragging.RowIndex;
        int colA = dragging.ColumnIndex;
        int rowB = target.RowIndex;
        int colB = target.ColumnIndex;

        if (!IsValidCell(rowA, colA) || !IsValidCell(rowB, colB))
        {
            Debug.LogWarning($"[PinManager] SwapPins: invalid cells ({rowA}, {colA}) <-> ({rowB}, {colB})");
            return;
        }

        var rowListA = pinsByRow[rowA];
        var rowListB = pinsByRow[rowB];

        if (rowListA == null || rowListB == null)
        {
            Debug.LogWarning("[PinManager] SwapPins: rowList null");
            return;
        }

        if (rowListA[colA] != dragging || rowListB[colB] != target)
        {
            Debug.LogWarning("[PinManager] SwapPins: grid and controller indices mismatch.");
        }

        var posA = GetPinWorldPosition(rowA, colA);
        var posB = GetPinWorldPosition(rowB, colB);

        rowListA[colA] = target;
        rowListB[colB] = dragging;

        target.transform.position = posA;
        dragging.transform.position = posB;

        target.SetGridIndices(rowA, colA);
        dragging.SetGridIndices(rowB, colB);

        (target.Instance.HitCount, dragging.Instance.HitCount) = (dragging.Instance.HitCount, target.Instance.HitCount);
    }

    public void RequestSellPin(PinController pin)
    {
        var sellPrice = Mathf.CeilToInt(pin.Instance.Price / 2f);
        var args = new Dictionary<string, object>
        {
            ["value"] = sellPrice
        };

        ModalManager.Instance.ShowConfirmation(
            "modal",
            "modal.sellpin.title",
            "modal",
            "modal.sellpin.message",
            () => SellPin(pin),
            () => { },
            args);
    }

    public void SellPin(PinController pin)
    {
        if (pin == null)
        {
            Debug.LogError("[PinManager] SellPin is null");
            return;
        }

        // 모달 띄워야함
        if (TryReplace(GameConfig.BasicPinId, pin.RowIndex, pin.ColumnIndex))
        {
            CurrencyManager.Instance.AddCurrency(Mathf.CeilToInt(pin.Instance.Price / 2f));
        }
    }
}