using System.Collections.Generic;
using UnityEngine;

public sealed class BallSpawnPointManager : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private BallSpawnPointView spawnPointPrefab;

    readonly List<BallSpawnPointView> points = new();
    bool isActive;

    public System.Action<Vector2> OnPointSelected;

    public void ShowPoints(List<Vector2> positions)
    {
        if (spawnPointPrefab == null || container == null)
        {
            Debug.LogError("[BallSpawnPointManager] Prefab or container not set.");
            return;
        }

        ClearPoints();
        isActive = true;

        foreach (var pos in positions)
        {
            var p = Instantiate(spawnPointPrefab, container);
            p.transform.position = pos;
            p.OnClicked = HandleSelected;
            points.Add(p);
        }
    }

    public void HidePoints()
    {
        isActive = false;
        ClearPoints();
    }

    void ClearPoints()
    {
        for (int i = points.Count - 1; i >= 0; i--)
        {
            if (points[i] != null)
                Destroy(points[i].gameObject);
        }
        points.Clear();
    }

    public void HandleSelected(Vector2 pos)
    {
        if (!isActive)
            return;

        isActive = false;
        OnPointSelected?.Invoke(pos);
        HidePoints();
    }
}
