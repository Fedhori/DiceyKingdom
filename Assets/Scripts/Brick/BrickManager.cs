using System.Collections.Generic;
using UnityEngine;

public sealed class BrickManager : MonoBehaviour
{
    public static BrickManager Instance { get; private set; }

    [SerializeField] private BrickFactory brickFactory;
    [SerializeField] private Transform playArea;
    [SerializeField] private Vector2Int gridSize = new Vector2Int(8, 16);
    [SerializeField] private Vector2 brickSize = new Vector2(128f, 64f);
    [SerializeField] private int newRowsPerStage = 2;
    [SerializeField] private int defaultHp = 100;
    private int currentHp = 100;
    [SerializeField] private float fallSpeed = 40f;

    private readonly List<List<BrickController>> grid = new();
    private Vector2 originTopLeft;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitGrid();
        ComputeOrigin();
    }

    void Update()
    {
        // Play 중에만 하강
        var flow = FlowManager.Instance;
        if (flow == null || flow.CurrentPhase != FlowPhase.Play)
            return;

        float dy = fallSpeed * Time.deltaTime;
        if (dy <= 0f)
            return;

        MoveAllBricksDown(dy);
    }

    void InitGrid()
    {
        grid.Clear();
        for (int y = 0; y < gridSize.y; y++)
        {
            var row = new List<BrickController>(gridSize.x);
            for (int x = 0; x < gridSize.x; x++)
                row.Add(null);
            grid.Add(row);
        }
    }

    void ComputeOrigin()
    {
        if (playArea == null)
        {
            originTopLeft = Vector2.zero;
            return;
        }

        var sr = playArea.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            var size = sr.bounds.size;
            var center = sr.bounds.center;
            originTopLeft = new Vector2(center.x - size.x * 0.5f, center.y + size.y * 0.5f);
        }
        else
        {
            originTopLeft = playArea.position;
        }
    }

    Vector3 GridToWorld(Vector2Int gridPos)
    {
        float x = originTopLeft.x + brickSize.x * (gridPos.x + 0.5f);
        float y = originTopLeft.y - brickSize.y * (gridPos.y + 0.5f);
        return new Vector3(x, y, 0f);
    }

    public void ClearAll()
    {
        for (int y = 0; y < grid.Count; y++)
        {
            var row = grid[y];
            if (row == null) continue;
            for (int x = 0; x < row.Count; x++)
            {
                if (row[x] != null)
                    Destroy(row[x].gameObject);
                row[x] = null;
            }
        }
    }

    public void ShiftDownAndSpawn()
    {
        currentHp = StageManager.Instance != null && StageManager.Instance.CurrentStage != null
            ? StageManager.Instance.CurrentStage.BlockHealth
            : defaultHp;

        ShiftDown(newRowsPerStage);
        SpawnRows(newRowsPerStage);
    }

    void ShiftDown(int rows)
    {
        if (rows <= 0) return;

        for (int y = gridSize.y - 1; y >= 0; y--)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                int fromY = y - rows;
                BrickController brick = fromY >= 0 ? grid[fromY][x] : null;

                grid[y][x] = brick;
                if (brick != null)
                {
                    brick.Instance.SetGridPos(new Vector2Int(x, y));
                    brick.SetGridPosition(GridToWorld(new Vector2Int(x, y)));
                }
            }
        }

        // 상단 비운 칸 null 처리
        for (int y = 0; y < rows && y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
                grid[y][x] = null;
        }
    }

    void SpawnRows(int count)
    {
        if (count <= 0) return;

        var rng = GameManager.Instance != null ? GameManager.Instance.Rng : new System.Random();

        int width = gridSize.x;
        int maxEmpties = 2;

        for (int i = 0; i < count; i++)
        {
            if (i == 0)
                continue;
            
            if (i >= gridSize.y)
                break;

            int emptyCount = rng.Next(1, maxEmpties + 1);

            var emptyXs = new HashSet<int>();
            while (emptyXs.Count < emptyCount)
                emptyXs.Add(rng.Next(0, width));

            for (int x = 0; x < width; x++)
            {
                if (emptyXs.Contains(x))
                    continue;

                SpawnBrick(new Vector2Int(x, i));
            }
        }
    }

    void SpawnBrick(Vector2Int gridPos)
    {
        if (brickFactory == null)
        {
            Debug.LogError("[BrickManager] brickFactory not assigned");
            return;
        }

        if (!IsInsideGrid(gridPos))
            return;

        var worldPos = GridToWorld(gridPos);
        var brick = brickFactory.CreateBrick(currentHp, gridPos, worldPos);
        grid[gridPos.y][gridPos.x] = brick;
    }

    public bool IsOverflow(int incomingRows)
    {
        if (incomingRows <= 0)
            return false;

        int thresholdRow = gridSize.y - incomingRows;
        if (thresholdRow < 0)
            return true;

        for (int y = thresholdRow; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                if (grid[y][x] != null)
                    return true;
            }
        }

        return false;
    }

    bool IsInsideGrid(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridSize.x && pos.y >= 0 && pos.y < gridSize.y;
    }

    void MoveAllBricksDown(float distance)
    {
        if (distance <= 0f)
            return;

        float delta = distance;
        for (int y = 0; y < grid.Count; y++)
        {
            var row = grid[y];
            if (row == null) continue;

            for (int x = 0; x < row.Count; x++)
            {
                var brick = row[x];
                if (brick == null) continue;

                var t = brick.transform;
                t.position += new Vector3(0f, -delta, 0f);
            }
        }
    }
}
