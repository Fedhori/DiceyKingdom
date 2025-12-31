using UnityEngine;

public sealed class BlockFactory : MonoBehaviour
{
    public static BlockFactory Instance { get; private set; }

    [SerializeField] private BlockController blockPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public BlockController CreateBlock(int hp, Vector2Int gridPos, Vector3 worldPos)
    {
        if (blockPrefab == null)
        {
            Debug.LogError("[BlockFactory] blockPrefab not assigned");
            return null;
        }

        var inst = new BlockInstance(hp, gridPos);
        var block = Instantiate(blockPrefab, worldPos, Quaternion.identity, transform);
        block.Initialize(inst);
        return block;
    }
}
