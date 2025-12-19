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

        isActive = true;
        EnsurePool(positions.Count);

        for (int i = 0; i < points.Count; i++)
            points[i].gameObject.SetActive(false);

        BallSpawnPointView centerView = null;
        BallSpawnPointView firstView = null;

        for (int i = 0; i < positions.Count; i++)
        {
            var pos = positions[i];
            BallSpawnPointView p = points[i];

            p.transform.position = pos;
            p.SetSelected(false);
            p.OnClicked = HandleSelected;
            p.gameObject.SetActive(true);

            if (firstView == null)
                firstView = p;
            if (centerView == null && pos == Vector2.zero)
                centerView = p;
        }

        // 기본 선택 처리: 기존 선택 유지, 없으면 중앙, 없으면 첫 포인트
        if (selectedView != null)
        {
            selectedView.SetSelected(true);
            if (isActive)
                OnPointSelected?.Invoke(selectedView.transform.position);
        }
        else if (centerView != null)
        {
            SetSelectedView(centerView, notify: true);
        }
        else if (firstView != null)
        {
            SetSelectedView(firstView, notify: true);
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
                points[i].gameObject.SetActive(false);
        }
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

    void EnsurePool(int count)
    {
        if (count < 0)
            count = 0;

        while (points.Count < count)
        {
            var p = Instantiate(spawnPointPrefab, container);
            p.gameObject.SetActive(false);
            p.OnClicked = HandleSelected;
            points.Add(p);
        }
    }

    public bool HasSelection => selectedView != null;
    public Vector2 SelectedPosition => selectedView != null ? (Vector2)selectedView.transform.position : Vector2.zero;
}
