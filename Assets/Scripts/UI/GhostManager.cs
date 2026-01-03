using System.Collections.Generic;
using Data;
using UnityEngine;

public sealed class GhostManager : MonoBehaviour
{
    [System.Serializable]
    public sealed class GhostPrefabEntry
    {
        public GhostKind kind;
        public GhostView prefab;
    }

    public static GhostManager Instance { get; private set; }

    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private List<GhostPrefabEntry> ghostPrefabs = new();

    readonly Dictionary<GhostKind, GhostView> ghostInstances = new();

    public bool IsVisible => currentGhost != null && currentGhost.gameObject.activeSelf;
    public Vector2 CurrentScreenPosition { get; private set; }
    public GhostKind CurrentKind { get; private set; } = GhostKind.None;

    GhostView currentGhost;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        HideGhost();
    }

    GhostView GetOrCreateGhost(GhostKind kind)
    {
        if (ghostInstances.TryGetValue(kind, out var view) && view != null)
            return view;

        var prefab = ghostPrefabs.Find(e => e != null && e.kind == kind)?.prefab;
        if (prefab == null)
        {
            Debug.LogWarning($"[GhostManager] No prefab configured for ghost kind {kind}");
            return null;
        }

        var parent = rootCanvas != null ? rootCanvas.transform : transform;
        var instance = Instantiate(prefab, parent, false);
        instance.gameObject.SetActive(false);
        ghostInstances[kind] = instance;
        return instance;
    }

    public void ShowGhost(Sprite sprite, Vector2 screenPos, GhostKind kind, ItemRarity rarity)
    {
        var view = GetOrCreateGhost(kind);
        if (view == null)
            return;

        currentGhost = view;
        CurrentKind = kind;

        view.SetIcon(sprite);
        view.SetRarity(rarity);
        view.gameObject.SetActive(true);
        UpdateGhostPosition(screenPos);
    }

    public void UpdateGhostPosition(Vector2 screenPos)
    {
        if (currentGhost == null)
            return;

        CurrentScreenPosition = screenPos;
        currentGhost.SetScreenPosition(screenPos, rootCanvas);
    }

    public void HideGhost(GhostKind kind = GhostKind.None)
    {
        if (currentGhost == null)
            return;

        if (kind != GhostKind.None && kind != CurrentKind)
            return;

        currentGhost.gameObject.SetActive(false);
        currentGhost = null;
        CurrentKind = GhostKind.None;
    }
}
