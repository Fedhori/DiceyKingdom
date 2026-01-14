using UnityEngine;

public sealed class BeamFactory : MonoBehaviour
{
    public static BeamFactory Instance { get; private set; }

    [SerializeField] private GameObject defaultPrefab;
    [SerializeField] private Transform parent;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public GameObject SpawnBeam(Transform attachTarget)
    {
        if (defaultPrefab == null)
        {
            Debug.LogError("[BeamFactory] defaultPrefab not assigned.");
            return null;
        }

        var target = attachTarget != null ? attachTarget : parent;
        if (target == null)
        {
            Debug.LogWarning("[BeamFactory] Missing attach target.");
            return null;
        }

        return Instantiate(defaultPrefab, target.position, Quaternion.identity, target);
    }

    public void ClearAllBeams()
    {
        var target = parent;
        if (target == null)
            return;

        for (int i = target.childCount - 1; i >= 0; i--)
        {
            var child = target.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }
    }
}
