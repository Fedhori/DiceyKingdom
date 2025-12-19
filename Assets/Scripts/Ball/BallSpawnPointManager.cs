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

        if (selectedView != null)
        {
            // 이미 선택된 포인트가 있으면 다시 선택 표시만 갱신
            selectedView.SetSelected(true);
            if (isActive)
                OnPointSelected?.Invoke(selectedView.transform.position);
        }
        else if (centerView != null)
        {
            SetSelectedView(centerView, notify: true);
        }
        else if (points.Count > 0)
        {
            SetSelectedView(points[0], notify: true);
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

        SetSelectedView(view, notify: true);
    }

    void SetSelectedView(BallSpawnPointView view, bool notify = false)
    {
        if (selectedView == view)
            return;

        if (selectedView != null)
            selectedView.SetSelected(false);

        selectedView = view;

        if (selectedView != null)
        {
            selectedView.SetSelected(true);
            if (notify)
                OnPointSelected?.Invoke(selectedView.transform.position);
        }
    }
}
