using UnityEngine;

public sealed class PinFactory : MonoBehaviour
{
    [SerializeField] GameObject pinPrefab;
    [SerializeField] Transform pinParent;
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

        Vector2 localPos = PinManager.Instance.GetPinWorldPosition(row, column);

        var obj = Instantiate(pinPrefab, pinParent, false);
        obj.transform.localPosition = localPos;
        obj.transform.localRotation = Quaternion.identity;

        var controller = obj.GetComponent<PinController>();
        if (controller == null)
        {
            Debug.LogError("[PinFactory] Spawned prefab has no PinController component.");
            Destroy(obj);
            return;
        }

        controller.Initialize(pinId, row, column, hitCount);
    }

    public void BindPin(PinController controller, string pinId, int hitCount)
    {
        if (controller == null)
        {
            Debug.LogError("[PinFactory] BindPin called with null controller.");
            return;
        }

        controller.Initialize(pinId, controller.RowIndex, controller.ColumnIndex, hitCount);
    }
}
