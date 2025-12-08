using UnityEngine;

public sealed class PinFactory : MonoBehaviour
{
    [SerializeField] GameObject pinPrefab;
    public static PinFactory Instance;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PinFactory] Multiple instances detected. Overwriting Instance.");
        }

        Instance = this;
    }

    public void SpawnPin(string pinId, int row, int column, int hitCount)
    {
        if (PinManager.Instance == null)
        {
            Debug.LogError("[PinFactory] PinManager.Instance is null. Cannot compute pin position.");
            return;
        }

        Vector2 position = PinManager.Instance.GetPinWorldPosition(row, column);

        var obj = Instantiate(pinPrefab, position, Quaternion.identity);
        var controller = obj.GetComponent<PinController>();
        if (controller == null)
        {
            Debug.LogError("[PinFactory] Spawned prefab has no PinController component.");
            Destroy(obj);
            return;
        }

        controller.Initialize(pinId, row, column, hitCount);
    }
}