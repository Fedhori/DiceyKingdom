using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public sealed class MissionAdventurerListPresenter : MonoBehaviour
{
    [SerializeField] Transform rowRoot;
    [SerializeField] MissionAdventurerRowView rowPrefab;
    [SerializeField] MissionIconRegistry iconRegistry;
    [SerializeField] MissionOverlayPresenter overlayPresenter;

    readonly List<MissionAdventurerRowView> rowPool = new();
    readonly DisposableBag subscriptions = new();
    readonly List<MissionAdventurerRowData> rowBuffer = new();

    RunServices boundRun;
    bool runRevisionSubscribed;
    bool setupValid;
    bool lastOverlayVisible;
    string lastActiveMissionUid = string.Empty;
    Action<string> detailRequestHandler;

    void Awake()
    {
        setupValid = ValidateReferences();
        if (!setupValid)
            enabled = false;
    }

    void OnEnable()
    {
        subscriptions.Clear();
        runRevisionSubscribed = false;
        if (!setupValid)
            return;

        TryBindRun(logError: true);
        Rebuild();
    }

    void OnDisable()
    {
        subscriptions.Clear();
        runRevisionSubscribed = false;
        boundRun = null;
        lastOverlayVisible = false;
        lastActiveMissionUid = string.Empty;
    }

    void Update()
    {
        if (!setupValid)
            return;

        if (!TryBindRun(logError: false))
            return;

        bool overlayVisible = IsOverlayVisible();
        string activeMissionUid = boundRun?.CurrentRunState?.activeMissionUid ?? string.Empty;
        if (overlayVisible == lastOverlayVisible &&
            string.Equals(activeMissionUid, lastActiveMissionUid, StringComparison.Ordinal))
            return;

        Rebuild();
    }

    public void BindOverlayPresenter(MissionOverlayPresenter presenter)
    {
        overlayPresenter = presenter;
        if (isActiveAndEnabled)
            Rebuild();
    }

    public void BindDetailRequestHandler(Action<string> handler)
    {
        detailRequestHandler = handler;
    }

    bool TryBindRun(bool logError)
    {
        if (boundRun == null)
            boundRun = GameApp.I?.Run;

        if (boundRun == null)
        {
            if (logError)
                Debug.LogError("[AdventurerList] RunServices is null. Enable this UI after BeginRun.", this);
            HideAllRows();
            return false;
        }

        if (!runRevisionSubscribed)
        {
            subscriptions.Add(boundRun.UiRevision.Subscribe(_ => Rebuild(), pushCurrent: false));
            runRevisionSubscribed = true;
        }

        return true;
    }

    void Rebuild()
    {
        if (!TryBindRun(logError: true))
            return;

        RunState state = boundRun.CurrentRunState;
        if (state?.adventurers == null || state.adventurers.Count <= 0)
        {
            HideAllRows();
            lastOverlayVisible = IsOverlayVisible();
            lastActiveMissionUid = state?.activeMissionUid ?? string.Empty;
            return;
        }

        rowBuffer.Clear();
        for (int i = 0; i < state.adventurers.Count; i++)
        {
            AdventurerInstance adventurer = state.adventurers[i];
            if (adventurer == null || adventurer.hp <= 0)
                continue;

            rowBuffer.Add(BuildRowData(state, adventurer));
        }

        rowBuffer.Sort(CompareRows);
        GrowRowPool(rowBuffer.Count);

        for (int i = 0; i < rowPool.Count; i++)
        {
            bool active = i < rowBuffer.Count;
            MissionAdventurerRowView row = rowPool[i];
            row.gameObject.SetActive(active);
            if (!active)
                continue;

            row.SetData(rowBuffer[i], iconRegistry, HandleRowLeftClick, HandleRowRightClick);
        }

        lastOverlayVisible = IsOverlayVisible();
        lastActiveMissionUid = state.activeMissionUid ?? string.Empty;
    }

    MissionAdventurerRowData BuildRowData(RunState state, AdventurerInstance adventurer)
    {
        int maxHp = Math.Max(0, boundRun.GetAdventurerStat(adventurer.uid, StatId.MaxHp));
        int hp = Math.Max(0, boundRun.GetAdventurerStat(adventurer.uid, StatId.Hp));
        if (maxHp > 0)
            hp = Mathf.Clamp(hp, 0, maxHp);

        int maxStamina = Math.Max(0, boundRun.GetAdventurerStat(adventurer.uid, StatId.MaxStamina));
        int stamina = Math.Max(0, boundRun.GetAdventurerStat(adventurer.uid, StatId.Stamina));
        if (maxStamina > 0)
            stamina = Mathf.Clamp(stamina, 0, maxStamina);

        return new MissionAdventurerRowData
        {
            adventurerUid = adventurer.uid ?? string.Empty,
            displayName = ResolveDisplayName(adventurer),
            level = Math.Max(1, adventurer.level),
            hp = hp,
            maxHp = maxHp,
            stamina = stamina,
            maxStamina = maxStamina,
            strength = Math.Max(0, boundRun.GetAdventurerStat(adventurer.uid, StatId.Strength)),
            agility = Math.Max(0, boundRun.GetAdventurerStat(adventurer.uid, StatId.Agility)),
            intelligence = Math.Max(0, boundRun.GetAdventurerStat(adventurer.uid, StatId.Intelligence)),
            isAssignable = CanAssignToActiveMission(adventurer.uid),
            portraitSprite = AdventurerPortraitCatalog.ResolvePortrait(adventurer.portraitIndex)
        };
    }

    bool CanAssignToActiveMission(string adventurerUid)
    {
        if (overlayPresenter == null)
            return false;

        if (!overlayPresenter.IsOverlayVisible)
            return false;

        return overlayPresenter.CanAssignAdventurerFromList(adventurerUid);
    }

    void HandleRowLeftClick(string adventurerUid)
    {
        if (string.IsNullOrWhiteSpace(adventurerUid))
            return;

        detailRequestHandler?.Invoke(adventurerUid);
    }

    void HandleRowRightClick(string adventurerUid)
    {
        if (string.IsNullOrWhiteSpace(adventurerUid))
            return;

        if (overlayPresenter == null || !overlayPresenter.IsOverlayVisible)
            return;

        if (!overlayPresenter.TryAssignAdventurerToFirstAvailableSlot(adventurerUid, out _))
            return;

        Rebuild();
    }

    static int CompareRows(MissionAdventurerRowData left, MissionAdventurerRowData right)
    {
        int assignable = right.isAssignable.CompareTo(left.isAssignable);
        if (assignable != 0)
            return assignable;

        int level = right.level.CompareTo(left.level);
        if (level != 0)
            return level;

        string leftName = left.displayName ?? string.Empty;
        string rightName = right.displayName ?? string.Empty;
        return string.Compare(leftName, rightName, StringComparison.Ordinal);
    }

    static string ResolveDisplayName(AdventurerInstance adventurer)
    {
        if (adventurer == null)
            return "모험가";

        if (!string.IsNullOrWhiteSpace(adventurer.displayName))
            return adventurer.displayName.Trim();

        string id = adventurer.adventurerId;
        if (string.IsNullOrWhiteSpace(id))
            return "모험가";

        string display = id;
        int separator = display.LastIndexOf('.');
        if (separator >= 0 && separator + 1 < display.Length)
            display = display[(separator + 1)..];

        display = display.Replace('_', ' ').Trim();
        if (display.Length <= 0)
            return "모험가";

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(display);
    }

    bool IsOverlayVisible()
    {
        return overlayPresenter != null && overlayPresenter.IsOverlayVisible;
    }

    void GrowRowPool(int requiredCount)
    {
        while (rowPool.Count < requiredCount)
        {
            MissionAdventurerRowView created = Instantiate(rowPrefab, rowRoot);
            created.gameObject.SetActive(true);
            rowPool.Add(created);
        }
    }

    void HideAllRows()
    {
        for (int i = 0; i < rowPool.Count; i++)
            rowPool[i].gameObject.SetActive(false);
    }

    bool ValidateReferences()
    {
        bool valid = true;
        if (rowRoot == null)
        {
            Debug.LogError("[AdventurerList] rowRoot is not assigned.", this);
            valid = false;
        }

        if (rowPrefab == null)
        {
            Debug.LogError("[AdventurerList] rowPrefab is not assigned.", this);
            valid = false;
        }

        if (iconRegistry == null)
        {
            Debug.LogError("[AdventurerList] iconRegistry is not assigned.", this);
            valid = false;
        }

        if (overlayPresenter == null)
        {
            Debug.LogError("[AdventurerList] overlayPresenter is not assigned.", this);
            valid = false;
        }

        return valid;
    }
}
