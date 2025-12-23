using System.Collections.Generic;
using Data;
using UnityEngine;

public sealed class PinManager : MonoBehaviour
{
    public static PinManager Instance { get; private set; }

    [Header("Grid Settings")]
    [SerializeField] int rowCount = 3;
    [SerializeField] int columnCount = 3;
    [SerializeField] float pinGap = 160f;

    [SerializeField] string spawnPinId = GameConfig.BasicPinId;

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

        return new Vector2(x, y);
    }
    
    public List<Vector2> GetBallSpawnPoints()
    {
        var result = new List<Vector2>(4);
        float gap = pinGap;
        result.Add(new Vector2(-gap / 2, gap / 2)); // 좌상
        result.Add(new Vector2(gap / 2, gap / 2));  // 우상
        result.Add(new Vector2(-gap / 2, -gap / 2)); // 좌하
        result.Add(new Vector2(gap / 2, -gap / 2));  // 우하
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
        if (string.IsNullOrEmpty(pinId))
            return false;

        if (!IsValidCell(row, col))
            return false;

        var controller = pinsByRow[row][col];
        if (controller?.Instance == null)
            return false;

        int hitCount = controller.Instance.HitCount;
        controller.Initialize(pinId, row, col, hitCount);
        return true;
    }

    public void SwapPins(PinController a, PinController b, bool moveTransforms = true)
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

        pinsByRow[rowA][colA] = b;
        pinsByRow[rowB][colB] = a;

        a.SetGridIndices(rowB, colB);
        b.SetGridIndices(rowA, colA);

        if (!moveTransforms)
            return;

        // 위치 스왑을 통해 Controller와 Instance를 함께 이동
        Vector3 posA = a.transform.position;
        Vector3 posB = b.transform.position;

        a.transform.position = posB;
        b.transform.position = posA;
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

        var price = pin.Instance.Price;
        if (TryReplace(GameConfig.BasicPinId, pin.RowIndex, pin.ColumnIndex))
            CurrencyManager.Instance.AddCurrency(Mathf.CeilToInt(price / 2f));
    }

    public void TriggerPins(PinTriggerType type)
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
                    pin.Instance.HandleTrigger(
                        type,
                        null,
                        Vector2.zero
                    );
                }
            }
        }
    }
}
