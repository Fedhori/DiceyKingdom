using System.Collections.Generic;
using UnityEngine;

public sealed class BallSpawnPointManager : MonoBehaviour
{
    public static BallSpawnPointManager Instance { get; private set; }

    [SerializeField] private Transform container;
    [SerializeField] private BallSpawnPointView spawnPointPrefab;

    readonly List<BallSpawnPointView> points = new();
    BallSpawnPointView selectedView;
    bool isActive;

    public System.Action<Vector2> OnPointSelected;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ShowPoints(List<Vector2> positions)
    {
        if (spawnPointPrefab == null || container == null)
        {
            Debug.LogError("[BallSpawnPointManager] Prefab or container not set.");
            return;
        }

        ClearPoints();
        isActive = true;
        selectedView = null;
        BallSpawnPointView centerView = null;

        foreach (var pos in positions)
        {
            var p = Instantiate(spawnPointPrefab, container);
            p.transform.position = pos;
            p.SetSelected(false);
            p.OnClicked = HandleSelected;
            points.Add(p);

            if (centerView == null && pos == Vector2.zero)
                centerView = p;
        }

        if (centerView != null)
        {
            SetSelectedView(centerView);
            OnPointSelected?.Invoke(centerView.transform.position);
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
        selectedView = null;
    }

    public void HandleSelected(BallSpawnPointView view)
    {
        if (!isActive)
            return;

        if (view == null)
            return;

        SetSelectedView(view);
        OnPointSelected?.Invoke(view.transform.position);
    }

    public void SetSelectedPoint(Vector2 position)
    {
        if (points.Count == 0)
            return;

        BallSpawnPointView closest = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < points.Count; i++)
        {
            var p = points[i];
            if (p == null)
                continue;

            float sqr = (p.transform.position - (Vector3)position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                closest = p;
            }
        }

        if (closest != null)
            SetSelectedView(closest);
    }

    void SetSelectedView(BallSpawnPointView view)
    {
        if (selectedView == view)
            return;

        if (selectedView != null)
            selectedView.SetSelected(false);

        selectedView = view;

        if (selectedView != null)
            selectedView.SetSelected(true);
    }
}
