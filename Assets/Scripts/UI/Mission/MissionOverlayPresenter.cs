using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MissionOverlayPresenter : MonoBehaviour
{
    [SerializeField] OverlayFader overlayFader;
    [SerializeField] Button backgroundCloseButton;
    [SerializeField] Button closeButton;
    [SerializeField] Button confirmButton;
    [SerializeField] TMP_Text missionNameText;
    [SerializeField] TMP_Text deadlineText;
    [SerializeField] TMP_Text successSummaryText;
    [SerializeField] TMP_Text deadlineFailSummaryText;
    [SerializeField] Transform tagRoot;
    [SerializeField] TMP_Text tagChipPrefab;
    [SerializeField] Transform testRowRoot;
    [SerializeField] MissionWorldTestRowView testRowPrefab;
    [SerializeField] Image partyStrengthIcon;
    [SerializeField] TMP_Text partyStrengthText;
    [SerializeField] Image partyAgilityIcon;
    [SerializeField] TMP_Text partyAgilityText;
    [SerializeField] Image partyIntelligenceIcon;
    [SerializeField] TMP_Text partyIntelligenceText;
    [SerializeField] Transform adventurerRowRoot;
    [SerializeField] MissionAdventurerRowView adventurerRowPrefab;
    [SerializeField] Transform draftSlotRoot;
    [SerializeField] MissionDraftSlotView draftSlotPrefab;
    [SerializeField] MissionIconRegistry iconRegistry;
    [SerializeField] GameObject lockStateRoot;

    readonly DisposableBag subscriptions = new();
    readonly MissionEffectSummaryBuilder summaryBuilder = new();
    readonly List<TMP_Text> tagPool = new();
    readonly List<MissionWorldTestRowView> testRowPool = new();
    readonly List<MissionAdventurerRowView> adventurerRowPool = new();
    readonly List<MissionDraftSlotView> draftSlotPool = new();
    readonly List<string> draftSlots = new();

    RunServices boundRun;
    string draftMissionUid = string.Empty;
    bool setupValid;
    bool isOpen;

    void Awake()
    {
        setupValid = ValidateReferences();
        if (!setupValid)
        {
            enabled = false;
            return;
        }

        overlayFader.SetVisibleInstant(false);
        isOpen = false;
    }

    void OnEnable()
    {
        subscriptions.Clear();
        if (!setupValid)
            return;

        boundRun = GameApp.I?.Run;
        if (boundRun == null)
        {
            Debug.LogError("[MissionOverlay] RunServices is null. Enable this UI after BeginRun.", this);
            return;
        }

        subscriptions.Add(boundRun.UiRevision.Subscribe(_ => HandleRunUiRevision(), pushCurrent: false));
        subscriptions.Add(EventSubscription.Subscribe(closeButton, HandleCloseClicked));
        subscriptions.Add(EventSubscription.Subscribe(confirmButton, HandleConfirmClicked));
        subscriptions.Add(EventSubscription.Subscribe(backgroundCloseButton, HandleBackgroundClicked));
    }

    void OnDisable()
    {
        subscriptions.Clear();
        boundRun = null;
        isOpen = false;
        ResetDraftState();
    }

    void Update()
    {
        if (!isOpen)
            return;

        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        TryCloseOverlay();
    }

    public void OpenOrFocus(string missionUid)
    {
        if (!setupValid)
            return;

        if (boundRun == null)
        {
            Debug.LogError("[MissionOverlay] Cannot open overlay. RunServices is null.", this);
            return;
        }

        if (!boundRun.SetActiveMission(missionUid))
        {
            Debug.LogError($"[MissionOverlay] Failed to open mission: {missionUid}", this);
            return;
        }

        if (!isOpen)
        {
            isOpen = true;
            overlayFader.Show();
        }

        overlayFader.transform.SetAsLastSibling();
        Rebuild();
    }

    bool ValidateReferences()
    {
        bool valid = true;
        if (overlayFader == null)
        {
            Debug.LogError("[MissionOverlay] overlayFader is not assigned.", this);
            valid = false;
        }

        if (backgroundCloseButton == null)
        {
            Debug.LogError("[MissionOverlay] backgroundCloseButton is not assigned.", this);
            valid = false;
        }

        if (closeButton == null)
        {
            Debug.LogError("[MissionOverlay] closeButton is not assigned.", this);
            valid = false;
        }

        if (confirmButton == null)
        {
            Debug.LogError("[MissionOverlay] confirmButton is not assigned.", this);
            valid = false;
        }

        if (missionNameText == null)
        {
            Debug.LogError("[MissionOverlay] missionNameText is not assigned.", this);
            valid = false;
        }

        if (deadlineText == null)
        {
            Debug.LogError("[MissionOverlay] deadlineText is not assigned.", this);
            valid = false;
        }

        if (successSummaryText == null)
        {
            Debug.LogError("[MissionOverlay] successSummaryText is not assigned.", this);
            valid = false;
        }

        if (deadlineFailSummaryText == null)
        {
            Debug.LogError("[MissionOverlay] deadlineFailSummaryText is not assigned.", this);
            valid = false;
        }

        if (tagRoot == null)
        {
            Debug.LogError("[MissionOverlay] tagRoot is not assigned.", this);
            valid = false;
        }

        if (tagChipPrefab == null)
        {
            Debug.LogError("[MissionOverlay] tagChipPrefab is not assigned.", this);
            valid = false;
        }

        if (testRowRoot == null)
        {
            Debug.LogError("[MissionOverlay] testRowRoot is not assigned.", this);
            valid = false;
        }

        if (testRowPrefab == null)
        {
            Debug.LogError("[MissionOverlay] testRowPrefab is not assigned.", this);
            valid = false;
        }

        if (partyStrengthIcon == null || partyStrengthText == null ||
            partyAgilityIcon == null || partyAgilityText == null ||
            partyIntelligenceIcon == null || partyIntelligenceText == null)
        {
            Debug.LogError("[MissionOverlay] party total icon/text references are not assigned.", this);
            valid = false;
        }

        if (adventurerRowRoot == null)
        {
            Debug.LogError("[MissionOverlay] adventurerRowRoot is not assigned.", this);
            valid = false;
        }

        if (adventurerRowPrefab == null)
        {
            Debug.LogError("[MissionOverlay] adventurerRowPrefab is not assigned.", this);
            valid = false;
        }

        if (draftSlotRoot == null)
        {
            Debug.LogError("[MissionOverlay] draftSlotRoot is not assigned.", this);
            valid = false;
        }

        if (draftSlotPrefab == null)
        {
            Debug.LogError("[MissionOverlay] draftSlotPrefab is not assigned.", this);
            valid = false;
        }

        if (iconRegistry == null)
        {
            Debug.LogError("[MissionOverlay] iconRegistry is not assigned.", this);
            valid = false;
        }

        if (lockStateRoot == null)
        {
            Debug.LogError("[MissionOverlay] lockStateRoot is not assigned.", this);
            valid = false;
        }

        return valid;
    }

    void HandleRunUiRevision()
    {
        if (!isOpen)
            return;

        Rebuild();
    }

    void HandleBackgroundClicked()
    {
        TryCloseOverlay();
    }

    void HandleCloseClicked()
    {
        TryCloseOverlay();
    }

    void TryCloseOverlay()
    {
        if (!isOpen)
            return;

        if (IsExpeditionLocked())
            return;

        ResetDraftState();
        overlayFader.Hide(() => { isOpen = false; });
    }

    void HandleConfirmClicked()
    {
        if (boundRun == null)
            return;

        RunState state = boundRun.CurrentRunState;
        if (!TryGetActiveMission(state, out MissionInstance mission, out MissionDef missionDef))
        {
            Debug.LogError("[MissionOverlay] Confirm failed. Active mission is invalid.", this);
            return;
        }

        if (mission.isPartyLocked || mission.isExpeditionInProgress)
        {
            Debug.LogError("[MissionOverlay] Confirm ignored. Mission is already locked/in progress.", this);
            Rebuild();
            return;
        }

        List<string> commitList = BuildCommitList();
        if (commitList.Count <= 0)
        {
            Debug.LogError("[MissionOverlay] Confirm requires at least one assigned adventurer.", this);
            return;
        }

        if (commitList.Count > Math.Max(1, missionDef.partyLimit))
        {
            Debug.LogError("[MissionOverlay] Confirm failed. Draft exceeds party limit.", this);
            return;
        }

        if (!boundRun.TryCommitMissionDraft(mission.uid, commitList, out string failureReason))
        {
            Debug.LogError($"[MissionOverlay] Confirm failed: {failureReason}", this);
            return;
        }

        Rebuild();
    }

    void Rebuild()
    {
        if (boundRun == null)
            return;

        RunState state = boundRun.CurrentRunState;
        if (!TryGetActiveMission(state, out MissionInstance mission, out MissionDef missionDef))
        {
            ResetDraftState();
            overlayFader.Hide(() => { isOpen = false; });
            return;
        }

        int partyLimit = Math.Max(1, missionDef.partyLimit);
        PrepareDraftState(mission, partyLimit);

        MissionOverlayData data = BuildOverlayData(state, mission, missionDef, partyLimit);
        RenderCore(data);
        RenderTags(data.tags);
        RenderTests(data.tests);
        RenderDraftSlots(state, partyLimit, data.isLocked);
        RenderAdventurerRows(state, mission, data.isLocked);
        RenderPartyTotals(state);
        RenderControls(data.isLocked);
        ApplyPartyIcons();
    }

    MissionOverlayData BuildOverlayData(RunState state, MissionInstance mission, MissionDef missionDef, int partyLimit)
    {
        var data = new MissionOverlayData
        {
            missionUid = mission.uid,
            missionName = BuildDisplayName(missionDef.id),
            remainingDeadlineTurns = mission.remainingDeadlineTurns,
            partyLimit = partyLimit,
            isSelected = string.Equals(state.activeMissionUid, mission.uid, StringComparison.Ordinal),
            isLocked = mission.isPartyLocked || mission.isExpeditionInProgress,
            successSummary = summaryBuilder.BuildSuccessSummary(missionDef),
            deadlineFailSummary = summaryBuilder.BuildDeadlineFailSummary(missionDef),
            tags = new List<string>(),
            tests = new List<MissionOverlayTestData>()
        };

        if (missionDef.tags != null)
        {
            for (int i = 0; i < missionDef.tags.Count; i++)
            {
                string tag = missionDef.tags[i];
                if (string.IsNullOrWhiteSpace(tag))
                    continue;

                data.tags.Add(tag.Trim());
            }
        }

        if (missionDef.abilityTests != null)
        {
            for (int i = 0; i < missionDef.abilityTests.Count; i++)
            {
                AbilityTestDef test = missionDef.abilityTests[i];
                if (test == null)
                    continue;

                var testData = new MissionOverlayTestData
                {
                    difficulty = Math.Max(0, test.difficulty),
                    isCleared = IsTestCleared(mission, i),
                    requiredAbilities = new List<string>()
                };

                if (test.requiredAbilities != null)
                {
                    for (int abilityIndex = 0; abilityIndex < test.requiredAbilities.Count; abilityIndex++)
                    {
                        string abilityId = test.requiredAbilities[abilityIndex];
                        if (string.IsNullOrWhiteSpace(abilityId))
                            continue;

                        testData.requiredAbilities.Add(abilityId);
                    }
                }

                data.tests.Add(testData);
            }
        }

        return data;
    }

    void RenderCore(MissionOverlayData data)
    {
        missionNameText.text = data.missionName;
        deadlineText.text = $"기한: {Math.Max(0, data.remainingDeadlineTurns)}T";
        successSummaryText.text = data.successSummary;
        deadlineFailSummaryText.text = data.deadlineFailSummary;
    }

    void RenderTags(List<string> tags)
    {
        int count = tags?.Count ?? 0;
        GrowTagPool(count);
        for (int i = 0; i < tagPool.Count; i++)
        {
            bool active = i < count;
            TMP_Text chip = tagPool[i];
            chip.gameObject.SetActive(active);
            if (!active)
                continue;

            chip.text = tags[i];
            chip.color = Colors.Semantic.TextPrimary;
        }

        tagRoot.gameObject.SetActive(count > 0);
    }

    void RenderTests(List<MissionOverlayTestData> tests)
    {
        int count = tests?.Count ?? 0;
        GrowTestRowPool(count);
        for (int i = 0; i < testRowPool.Count; i++)
        {
            bool active = i < count;
            MissionWorldTestRowView row = testRowPool[i];
            row.gameObject.SetActive(active);
            if (!active)
                continue;

            MissionOverlayTestData source = tests[i];
            var viewData = new MissionWorldTestData
            {
                difficulty = source.difficulty,
                isCleared = source.isCleared,
                requiredAbilities = source.requiredAbilities ?? new List<string>()
            };
            row.SetData(viewData, iconRegistry);
        }
    }

    void RenderDraftSlots(RunState state, int partyLimit, bool locked)
    {
        GrowDraftSlotPool(partyLimit);
        for (int i = 0; i < draftSlotPool.Count; i++)
        {
            bool active = i < partyLimit;
            MissionDraftSlotView slot = draftSlotPool[i];
            slot.gameObject.SetActive(active);
            if (!active)
                continue;

            string assignedUid = draftSlots[i];
            string assignedName = ResolveAdventurerDisplayName(state, assignedUid);
            var data = new MissionDraftSlotData
            {
                slotIndex = i,
                assignedAdventurerUid = assignedUid,
                assignedDisplayName = assignedName,
                canInteract = !locked,
                hasAssigned = !string.IsNullOrWhiteSpace(assignedUid)
            };
            slot.SetData(data, HandleSlotClear, HandleSlotDropAdventurer);
        }
    }

    void RenderAdventurerRows(RunState state, MissionInstance mission, bool locked)
    {
        var list = new List<MissionAdventurerRowData>();
        if (state?.adventurers != null)
        {
            for (int i = 0; i < state.adventurers.Count; i++)
            {
                AdventurerInstance adventurer = state.adventurers[i];
                if (adventurer == null || string.IsNullOrWhiteSpace(adventurer.uid))
                    continue;

                bool selected = IsDraftAssigned(adventurer.uid);
                bool assignable = !locked && CanAssignToCurrentMission(adventurer, mission);
                list.Add(new MissionAdventurerRowData
                {
                    adventurerUid = adventurer.uid,
                    displayName = BuildDisplayName(adventurer.adventurerId),
                    strength = boundRun.GetAdventurerStat(adventurer.uid, StatId.Strength),
                    agility = boundRun.GetAdventurerStat(adventurer.uid, StatId.Agility),
                    intelligence = boundRun.GetAdventurerStat(adventurer.uid, StatId.Intelligence),
                    isAssignable = assignable,
                    isSelected = selected
                });
            }
        }

        list.Sort((left, right) =>
        {
            int assignableCompare = right.isAssignable.CompareTo(left.isAssignable);
            if (assignableCompare != 0)
                return assignableCompare;

            return string.CompareOrdinal(left.adventurerUid, right.adventurerUid);
        });

        GrowAdventurerRowPool(list.Count);
        for (int i = 0; i < adventurerRowPool.Count; i++)
        {
            bool active = i < list.Count;
            MissionAdventurerRowView row = adventurerRowPool[i];
            row.gameObject.SetActive(active);
            if (!active)
                continue;

            row.SetData(list[i], HandleAdventurerRowClicked);
        }
    }

    void RenderPartyTotals(RunState state)
    {
        MissionPartyTotalsData totals = BuildPartyTotals(state);
        partyStrengthText.text = totals.strength.ToString();
        partyAgilityText.text = totals.agility.ToString();
        partyIntelligenceText.text = totals.intelligence.ToString();
    }

    void RenderControls(bool locked)
    {
        int assignedCount = CountAssignedInDraft();
        bool canConfirm = !locked && assignedCount > 0;
        confirmButton.interactable = canConfirm;
        closeButton.interactable = !locked;
        backgroundCloseButton.interactable = !locked;
        lockStateRoot.SetActive(locked);
    }

    void ApplyPartyIcons()
    {
        SetAbilityIcon(partyStrengthIcon, "strength");
        SetAbilityIcon(partyAgilityIcon, "agility");
        SetAbilityIcon(partyIntelligenceIcon, "intelligence");
    }

    void SetAbilityIcon(Image target, string abilityId)
    {
        if (target == null)
            return;

        if (!iconRegistry.TryResolveAbilityIcon(abilityId, out Sprite sprite, out Color color))
        {
            target.enabled = false;
            return;
        }

        target.enabled = true;
        target.sprite = sprite;
        target.color = color;
    }

    void HandleAdventurerRowClicked(string adventurerUid)
    {
        if (string.IsNullOrWhiteSpace(adventurerUid))
            return;

        if (IsExpeditionLocked())
            return;

        if (RemoveFromDraft(adventurerUid))
        {
            Rebuild();
            return;
        }

        int emptyIndex = FindFirstEmptyDraftSlot();
        if (emptyIndex < 0)
            return;

        draftSlots[emptyIndex] = adventurerUid;
        Rebuild();
    }

    void HandleSlotClear(int slotIndex)
    {
        if (IsExpeditionLocked())
            return;

        if (slotIndex < 0 || slotIndex >= draftSlots.Count)
            return;

        draftSlots[slotIndex] = string.Empty;
        Rebuild();
    }

    void HandleSlotDropAdventurer(int slotIndex, string adventurerUid)
    {
        if (IsExpeditionLocked())
            return;

        if (slotIndex < 0 || slotIndex >= draftSlots.Count || string.IsNullOrWhiteSpace(adventurerUid))
            return;

        int existing = FindDraftIndex(adventurerUid);
        if (existing >= 0)
            draftSlots[existing] = string.Empty;

        draftSlots[slotIndex] = adventurerUid;
        Rebuild();
    }

    void PrepareDraftState(MissionInstance mission, int partyLimit)
    {
        bool missionChanged = !string.Equals(draftMissionUid, mission.uid, StringComparison.Ordinal);
        if (missionChanged)
        {
            draftMissionUid = mission.uid;
            draftSlots.Clear();
            for (int i = 0; i < partyLimit; i++)
                draftSlots.Add(string.Empty);

            if (mission.assignedAdventurerUids != null)
            {
                for (int i = 0; i < mission.assignedAdventurerUids.Count && i < draftSlots.Count; i++)
                    draftSlots[i] = mission.assignedAdventurerUids[i];
            }

            return;
        }

        if (draftSlots.Count == partyLimit)
            return;

        if (draftSlots.Count < partyLimit)
        {
            while (draftSlots.Count < partyLimit)
                draftSlots.Add(string.Empty);
            return;
        }

        while (draftSlots.Count > partyLimit)
            draftSlots.RemoveAt(draftSlots.Count - 1);
    }

    void ResetDraftState()
    {
        draftMissionUid = string.Empty;
        draftSlots.Clear();
    }

    bool CanAssignToCurrentMission(AdventurerInstance adventurer, MissionInstance mission)
    {
        if (adventurer == null || mission == null)
            return false;

        if (adventurer.hp <= 0)
            return false;

        if (adventurer.assignedThisTurn && !IsDraftAssigned(adventurer.uid))
            return false;

        if (string.IsNullOrWhiteSpace(adventurer.assignedMissionUid))
            return true;

        return string.Equals(adventurer.assignedMissionUid, mission.uid, StringComparison.Ordinal);
    }

    MissionPartyTotalsData BuildPartyTotals(RunState state)
    {
        var totals = new MissionPartyTotalsData();
        if (state == null)
            return totals;

        for (int i = 0; i < draftSlots.Count; i++)
        {
            string uid = draftSlots[i];
            if (string.IsNullOrWhiteSpace(uid))
                continue;

            totals.strength += Math.Max(0, boundRun.GetAdventurerStat(uid, StatId.Strength));
            totals.agility += Math.Max(0, boundRun.GetAdventurerStat(uid, StatId.Agility));
            totals.intelligence += Math.Max(0, boundRun.GetAdventurerStat(uid, StatId.Intelligence));
        }

        return totals;
    }

    List<string> BuildCommitList()
    {
        var list = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < draftSlots.Count; i++)
        {
            string uid = draftSlots[i];
            if (string.IsNullOrWhiteSpace(uid))
                continue;

            if (!seen.Add(uid))
                continue;

            list.Add(uid);
        }

        return list;
    }

    int CountAssignedInDraft()
    {
        int count = 0;
        for (int i = 0; i < draftSlots.Count; i++)
        {
            if (!string.IsNullOrWhiteSpace(draftSlots[i]))
                count++;
        }

        return count;
    }

    int FindFirstEmptyDraftSlot()
    {
        for (int i = 0; i < draftSlots.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(draftSlots[i]))
                return i;
        }

        return -1;
    }

    int FindDraftIndex(string adventurerUid)
    {
        for (int i = 0; i < draftSlots.Count; i++)
        {
            if (string.Equals(draftSlots[i], adventurerUid, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    bool RemoveFromDraft(string adventurerUid)
    {
        int index = FindDraftIndex(adventurerUid);
        if (index < 0)
            return false;

        draftSlots[index] = string.Empty;
        return true;
    }

    bool IsDraftAssigned(string adventurerUid)
    {
        return FindDraftIndex(adventurerUid) >= 0;
    }

    bool IsExpeditionLocked()
    {
        if (!isOpen || boundRun == null)
            return false;

        RunState state = boundRun.CurrentRunState;
        if (!TryGetActiveMission(state, out MissionInstance mission, out _))
            return false;

        return mission.isPartyLocked || mission.isExpeditionInProgress;
    }

    string ResolveAdventurerDisplayName(RunState state, string adventurerUid)
    {
        if (state?.adventurers == null || string.IsNullOrWhiteSpace(adventurerUid))
            return "비어 있음";

        for (int i = 0; i < state.adventurers.Count; i++)
        {
            AdventurerInstance adventurer = state.adventurers[i];
            if (adventurer == null)
                continue;

            if (!string.Equals(adventurer.uid, adventurerUid, StringComparison.Ordinal))
                continue;

            return BuildDisplayName(adventurer.adventurerId);
        }

        return "비어 있음";
    }

    bool TryGetActiveMission(RunState state, out MissionInstance mission, out MissionDef missionDef)
    {
        mission = null;
        missionDef = null;
        if (state?.missions == null)
            return false;

        if (string.IsNullOrWhiteSpace(state.activeMissionUid))
            return false;

        for (int i = 0; i < state.missions.Count; i++)
        {
            MissionInstance entry = state.missions[i];
            if (entry == null)
                continue;

            if (!string.Equals(entry.uid, state.activeMissionUid, StringComparison.Ordinal))
                continue;

            mission = entry;
            break;
        }

        if (mission == null || StaticDataLoader.Current == null)
            return false;

        return StaticDataLoader.Current.TryGetMissionDef(mission.missionId, out missionDef);
    }

    static bool IsTestCleared(MissionInstance mission, int testIndex)
    {
        if (mission?.abilityTestProgresses == null)
            return false;

        if (testIndex < 0 || testIndex >= mission.abilityTestProgresses.Count)
            return false;

        AbilityTestProgressInstance progress = mission.abilityTestProgresses[testIndex];
        return progress != null && progress.isCleared;
    }

    static string BuildDisplayName(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return "Unknown";

        string display = id;
        int separator = display.LastIndexOf('.');
        if (separator >= 0 && separator + 1 < display.Length)
            display = display[(separator + 1)..];

        display = display.Replace('_', ' ').Trim();
        if (display.Length == 0)
            return id;

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(display);
    }

    void GrowTagPool(int requiredCount)
    {
        while (tagPool.Count < requiredCount)
        {
            TMP_Text created = Instantiate(tagChipPrefab, tagRoot);
            created.gameObject.SetActive(true);
            tagPool.Add(created);
        }
    }

    void GrowTestRowPool(int requiredCount)
    {
        while (testRowPool.Count < requiredCount)
        {
            MissionWorldTestRowView created = Instantiate(testRowPrefab, testRowRoot);
            created.gameObject.SetActive(true);
            testRowPool.Add(created);
        }
    }

    void GrowAdventurerRowPool(int requiredCount)
    {
        while (adventurerRowPool.Count < requiredCount)
        {
            MissionAdventurerRowView created = Instantiate(adventurerRowPrefab, adventurerRowRoot);
            created.gameObject.SetActive(true);
            adventurerRowPool.Add(created);
        }
    }

    void GrowDraftSlotPool(int requiredCount)
    {
        while (draftSlotPool.Count < requiredCount)
        {
            MissionDraftSlotView created = Instantiate(draftSlotPrefab, draftSlotRoot);
            created.gameObject.SetActive(true);
            draftSlotPool.Add(created);
        }
    }
}
