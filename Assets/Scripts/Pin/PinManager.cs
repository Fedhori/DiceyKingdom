using System.Collections.Generic;
using UnityEngine;

public sealed class PinManager : MonoBehaviour
{
    public static PinManager Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] int rowCount = 5;
    [SerializeField] int columnCount = 5;
    [SerializeField] float pinGap = 64f;

    [SerializeField] string spawnPinId = GameConfig.BasicPinId;
    [SerializeField] Vector2 centerOffset = Vector2.zero;

    readonly List<List<PinController>> pinsByRow = new();
    public IReadOnlyList<List<PinController>> PinsByRow => pinsByRow;

    public string DefaultPinId => GameConfig.BasicPinId;

    void Awake()
    {
        if (Instance != null && Instance != this)
            Debug.LogWarning("[PinManager] Multiple instances detected. Overwriting Instance.");

        Instance = this;
        BuildGrid();
    }

    void Start()
    {
        GeneratePins();
    }

    void BuildGrid()
    {
        pinsByRow.Clear();

        if (rowCount <= 0 || columnCount <= 0)
            return;

        for (int row = 0; row < rowCount; row++)
        {
            var rowList = new List<PinController>(columnCount);
            for (int col = 0; col < columnCount; col++)
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
            return;

        for (int row = 0; row < rowCount; row++)
        {
            for (int col = 0; col < columnCount; col++)
                PinFactory.Instance.SpawnPin(spawnPinId, row, col, 0);
        }
    }

    public Vector2 GetPinWorldPosition(int row, int col)
    {
        float dx = pinGap;
        float dy = pinGap;

        float centerRow = (rowCount - 1) * 0.5f;
        float centerCol = (columnCount - 1) * 0.5f;

        float x = (col - centerCol) * dx;
        float y = (centerRow - row) * dy;

        return new Vector2(x, y) + centerOffset;
    }

    /// <summary>
    /// 4x4 핀 격자 중심을 기준으로 코너(좌상/우상/좌하/우하) 4개 스폰 지점 좌표를 계산한다.
    /// </summary>
    public List<Vector2> GetBallSpawnPoints()
    {
        var result = new List<Vector2>(4);
        float gap = pinGap;
        result.Add(new Vector2(-gap, gap) + centerOffset); // 좌상
        result.Add(new Vector2(gap, gap) + centerOffset);  // 우상
        result.Add(new Vector2(-gap, -gap) + centerOffset); // 좌하
        result.Add(new Vector2(gap, -gap) + centerOffset);  // 우하
        return result;
    }

    public void RegisterPin(PinController pin, int row, int col)
    {
        if (pin == null)
            return;

        if (!IsValidCell(row, col))
        {
            Debug.LogWarning($"[PinManager] RegisterPin: ({row}, {col}) out of range.");
            return;
        }

        var existing = pinsByRow[row][col];
        if (existing != null && existing != pin)
            Destroy(existing.gameObject);

        pinsByRow[row][col] = pin;
    }

    public void UnregisterPin(PinController pin, int row, int col)
    {
        if (pin == null)
            return;

        if (!IsValidCell(row, col))
            return;

        if (pinsByRow[row][col] == pin)
            pinsByRow[row][col] = null;
    }

    public void ResetAllPins()
    {
        for (int row = 0; row < pinsByRow.Count; row++)
        {
            var rowList = pinsByRow[row];
            for (int col = 0; col < rowList.Count; col++)
            {
                var pin = rowList[col];
                if (pin?.Instance == null)
                    continue;

                pin.Instance.ResetData(0);
            }
        }
    }

    bool IsValidCell(int row, int col)
    {
        return row >= 0 && row < pinsByRow.Count
               && col >= 0 && col < columnCount;
    }

    public bool GetBasicPinSlot(out int row, out int col)
    {
        row = 0;
        col = 0;

        string id = DefaultPinId;
        if (string.IsNullOrEmpty(id))
            return false;

        for (int r = 0; r < pinsByRow.Count; r++)
        {
            var rowList = pinsByRow[r];
            for (int c = 0; c < rowList.Count; c++)
            {
                var pin = rowList[c];
                if (pin?.Instance != null && pin.Instance.Id == id)
                {
                    row = r;
                    col = c;
                    return true;
                }
            }
        }

        return false;
    }

    public bool TryReplace(string pinId, int row, int col)
    {
        if (PinFactory.Instance == null)
        {
            Debug.LogError("[PinManager] TryReplace failed. PinFactory.Instance is null.");
            return false;
        }

        if (string.IsNullOrEmpty(pinId))
            return false;

        if (!IsValidCell(row, col))
            return false;

        var pin = pinsByRow[row][col];
        if (pin?.Instance == null)
            return false;

        PinFactory.Instance.SpawnPin(pinId, row, col, pin.Instance.HitCount);
        return true;
    }

    public void SwapPins(PinController a, PinController b)
    {
        if (a == null || b == null || a == b)
            return;

        int rowA = a.RowIndex;
        int colA = a.ColumnIndex;
        int rowB = b.RowIndex;
        int colB = b.ColumnIndex;

        if (!IsValidCell(rowA, colA) || !IsValidCell(rowB, colB))
        {
            Debug.LogWarning($"[PinManager] SwapPins: invalid cells ({rowA}, {colA}) <-> ({rowB}, {colB})");
            return;
        }

        var posA = GetPinWorldPosition(rowA, colA);
        var posB = GetPinWorldPosition(rowB, colB);

        pinsByRow[rowA][colA] = b;
        pinsByRow[rowB][colB] = a;

        b.transform.position = posA;
        a.transform.position = posB;

        b.SetGridIndices(rowA, colA);
        a.SetGridIndices(rowB, colB);

        if (a.Instance != null && b.Instance != null)
            (b.Instance.HitCount, a.Instance.HitCount) = (a.Instance.HitCount, b.Instance.HitCount);
    }

    public void RequestSellPin(PinController pin)
    {
        if (pin?.Instance == null)
            return;

        int sellPrice = Mathf.CeilToInt(pin.Instance.Price / 2f);

        var args = new Dictionary<string, object>
        {
            ["pinName"] = LocalizationUtil.GetPinName(pin.Instance.Id),
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
        if (pin?.Instance == null)
        {
            Debug.LogError("[PinManager] SellPin pin/instance is null");
            return;
        }

        if (TryReplace(GameConfig.BasicPinId, pin.RowIndex, pin.ColumnIndex))
            CurrencyManager.Instance.AddCurrency(Mathf.CeilToInt(pin.Instance.Price / 2f));
    }

    public void HandleStageFinished()
    {
        for (int row = 0; row < pinsByRow.Count; row++)
        {
            var rowList = pinsByRow[row];
            if (rowList == null)
                continue;

            for (int col = 0; col < rowList.Count; col++)
            {
                var pin = rowList[col];
                if (pin?.Instance != null)
                {
                    pin.Instance.HandleStageFinished();
                }
            }
        }
    }
}
