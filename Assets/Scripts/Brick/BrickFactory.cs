using UnityEngine;

public sealed class BrickFactory : MonoBehaviour
{
    public static BrickFactory Instance { get; private set; }

    [SerializeField] private BrickController brickPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public BrickController CreateBrick(int hp, Vector2Int gridPos, Vector3 worldPos)
    {
        if (brickPrefab == null)
        {
            Debug.LogError("[BrickFactory] brickPrefab not assigned");
            return null;
        }

        var inst = new BrickInstance(hp, gridPos);
        var brick = Instantiate(brickPrefab, worldPos, Quaternion.identity, transform);
        brick.Initialize(inst);
        return brick;
    }
}
