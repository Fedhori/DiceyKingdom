using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class MissionOverlayPresenter : MonoBehaviour
{
    const int MaxVisibleSlots = 4;
    const string StrengthAbilityId = "strength";
    const string AgilityAbilityId = "agility";
    const string IntelligenceAbilityId = "intelligence";

    [SerializeField] GameObject panelRoot;
    [SerializeField] Button closeButton;
    [SerializeField] Button startExpeditionButton;
    [SerializeField] TMP_Text missionNameText;
    [SerializeField] TMP_Text deadlineText;
    [SerializeField] Transform tagRoot;
    [SerializeField] TMP_Text tagChipPrefab;
    [SerializeField] Transform testDiceRoot;
    [SerializeField] MissionOverlayTestDiceView testDicePrefab;
    [SerializeField] Transform partyStatDiceRoot;
    [SerializeField] MissionOverlayStatDiceView partyStatDicePrefab;
    [SerializeField] Transform slotCellRoot;
    [SerializeField] MissionOverlaySlotCellView slotCellPrefab;
    [SerializeField] TMP_Text rewardSummaryText;
    [SerializeField] TMP_Text failureSummaryText;
    [SerializeField] MissionIconRegistry iconRegistry;

    readonly DisposableBag subscriptions = new();
    readonly MissionEffectSummaryBuilder summaryBuilder = new();
    readonly List<TMP_Text> tagPool = new();
    readonly List<MissionOverlayTestDiceView> testDicePool = new();
    readonly List<MissionOverlayStatDiceView> statDicePool = new();
    readonly List<MissionOverlaySlotCellView> slotCellPool = new();
    readonly List<string> draftSlots = new();

    RunServices boundRun;
    bool runRevisionSubscribed;
    bool setupValid;
    string draftMissionUid = string.Empty;

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

        subscriptions.Add(EventSubscription.Subscribe(closeButton, HandleCloseClicked));
        subscriptions.Add(EventSubscription.Subscribe(startExpeditionButton, HandleStartExpeditionClicked));

        TryBindRun(logError: true);
    }

    void OnDisable()
    {
        subscriptions.Clear();
        boundRun = null;
        runRevisionSubscribed = false;
        ResetDraftState();
    }

    void Update()
    {
        if (!gameObject.activeSelf)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            return;

        CloseOverlay();
    }

    public void OpenOrFocus(string missionUid)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (!setupValid)
        {
            Debug.LogError("[MissionOverlay] OpenOrFocus aborted: setup validation failed.", this);
            return;
        }

        if (!TryBindRun(logError: true))
        {
            Debug.LogError("[MissionOverlay] OpenOrFocus aborted: RunServices binding failed.", this);
            return;
        }

        if (!boundRun.SetActiveMission(missionUid))
        {
            Debug.LogError($"[MissionOverlay] Failed to select mission: {missionUid}", this);
            return;
        }

        if (panelRoot != null && !panelRoot.activeSelf)
            panelRoot.SetActive(true);

        Rebuild();
    }

    public bool TryAssignAdventurerToFirstAvailableSlot(string adventurerUid, out string reason)
    {
        reason = string.Empty;
        if (!PrepareDraftMutation(out RunState state, out MissionInstance mission, out MissionDef missionDef, out int effectiveLimit, out reason))
            return false;

        int targetIndex = -1;
        for (int i = 0; i < effectiveLimit; i++)
        {
            if (!string.IsNullOrWhiteSpace(draftSlots[i]))
                continue;

            targetIndex = i;
            break;
        }

        if (targetIndex < 0)
        {
            reason = "No empty draft slot is available.";
            return false;
        }

        return TryAssignAdventurerToSlotInternal(targetIndex, adventurerUid, state, mission, out reason);
    }

    public bool TryAssignAdventurerToSlot(int slotIndex, string adventurerUid, out string reason)
    {
        reason = string.Empty;
        if (!PrepareDraftMutation(out RunState state, out MissionInstance mission, out MissionDef missionDef, out int effectiveLimit, out reason))
            return false;

        if (slotIndex < 0 || slotIndex >= MaxVisibleSlots)
        {
            reason = $"slotIndex is out of range: {slotIndex}";
            return false;
        }

        if (slotIndex >= effectiveLimit)
        {
            reason = $"slotIndex exceeds party limit: {slotIndex}/{effectiveLimit - 1}";
            return false;
        }

        return TryAssignAdventurerToSlotInternal(slotIndex, adventurerUid, state, mission, out reason);
    }

    public bool TryClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MaxVisibleSlots)
            return false;

        EnsureDraftSlotCapacity();
        if (string.IsNullOrWhiteSpace(draftSlots[slotIndex]))
            return true;

        draftSlots[slotIndex] = string.Empty;
        Rebuild();
        return true;
    }

    public IReadOnlyList<string> GetDraftAssignedUids()
    {
        EnsureDraftSlotCapacity();
        return BuildCommitList(MaxVisibleSlots);
    }

    void HandleCloseClicked()
    {
        CloseOverlay();
    }

    void HandleStartExpeditionClicked()
    {
        if (!TryBindRun(logError: true))
            return;

        if (!TryGetActiveMission(boundRun.CurrentRunState, out MissionInstance mission, out MissionDef missionDef))
        {
            Debug.LogError("[MissionOverlay] Cannot start expedition. Active mission is invalid.", this);
            return;
        }

        if (mission.isPartyLocked || mission.isExpeditionInProgress)
            return;

        int effectiveLimit = GetEffectivePartyLimit(missionDef);
        List<string> commitList = BuildCommitList(effectiveLimit);
        if (commitList.Count <= 0)
            return;

        if (!boundRun.TryCommitMissionDraft(mission.uid, commitList, out string failureReason))
        {
            Debug.LogError($"[MissionOverlay] Start expedition failed: {failureReason}", this);
            Rebuild();
            return;
        }

        CloseOverlay();
    }

    void HandleRunUiRevision()
    {
        if (!gameObject.activeSelf)
            return;

        Rebuild();
    }

    void CloseOverlay()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        gameObject.SetActive(false);
    }

    void Rebuild()
    {
        if (!TryBindRun(logError: true))
            return;

        RunState state = boundRun.CurrentRunState;
        if (!TryGetActiveMission(state, out MissionInstance mission, out MissionDef missionDef))
        {
            Debug.LogError("[MissionOverlay] Active mission is missing or invalid.", this);
            CloseOverlay();
            return;
        }

        int effectiveLimit = GetEffectivePartyLimit(missionDef);
        EnsureDraftState(mission, effectiveLimit);

        missionNameText.text = BuildDisplayName(missionDef.id);
        deadlineText.text = $"기한 {Math.Max(0, mission.remainingDeadlineTurns)}T";
        rewardSummaryText.text = summaryBuilder.BuildSuccessSummary(missionDef);
        failureSummaryText.text = summaryBuilder.BuildDeadlineFailSummary(missionDef);

        RenderTags(missionDef.tags);
        RenderTestDice(mission, missionDef);
        RenderPartyStats(state, effectiveLimit);
        RenderSlotCells(state, effectiveLimit);
        RenderStartButton(mission, effectiveLimit);
    }

    bool PrepareDraftMutation(
        out RunState state,
        out MissionInstance mission,
        out MissionDef missionDef,
        out int effectiveLimit,
        out string reason)
    {
        state = null;
        mission = null;
        missionDef = null;
        effectiveLimit = 0;
        reason = string.Empty;

        if (!TryBindRun(logError: true))
        {
            reason = "RunServices is null.";
            return false;
        }

        state = boundRun.CurrentRunState;
        if (!TryGetActiveMission(state, out mission, out missionDef))
        {
            reason = "Active mission is invalid.";
            return false;
        }

        effectiveLimit = GetEffectivePartyLimit(missionDef);
        EnsureDraftState(mission, effectiveLimit);
        return true;
    }

    bool TryAssignAdventurerToSlotInternal(
        int slotIndex,
        string adventurerUid,
        RunState state,
        MissionInstance mission,
        out string reason)
    {
        reason = string.Empty;
        EnsureDraftSlotCapacity();

        if (!ValidateDraftAdventurer(state, mission, adventurerUid, out reason))
            return false;

        int existingIndex = FindDraftIndex(adventurerUid);
        if (existingIndex >= 0)
            draftSlots[existingIndex] = string.Empty;

        draftSlots[slotIndex] = adventurerUid;
        Rebuild();
        return true;
    }

    bool ValidateDraftAdventurer(RunState state, MissionInstance mission, string adventurerUid, out string reason)
    {
        reason = string.Empty;
        if (string.IsNullOrWhiteSpace(adventurerUid))
        {
            reason = "adventurerUid is empty.";
            return false;
        }

        if (!TryGetAdventurer(state, adventurerUid, out AdventurerInstance adventurer))
        {
            reason = $"Adventurer not found: {adventurerUid}";
            return false;
        }

        if (adventurer.hp <= 0)
        {
            reason = $"Adventurer is dead: {adventurerUid}";
            return false;
        }

        if (adventurer.assignedThisTurn)
        {
            reason = $"Adventurer is unavailable this turn: {adventurerUid}";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(adventurer.assignedMissionUid) &&
            !string.Equals(adventurer.assignedMissionUid, mission.uid, StringComparison.Ordinal))
        {
            if (!TryGetMission(state, adventurer.assignedMissionUid, out MissionInstance previousMission))
            {
                reason = $"Previous mission not found for adventurer: {adventurerUid}";
                return false;
            }

            if (previousMission.isPartyLocked || previousMission.isExpeditionInProgress)
            {
                reason = $"Adventurer is locked by another mission: {adventurerUid}";
                return false;
            }
        }

        return true;
    }

    bool TryBindRun(bool logError)
    {
        if (boundRun == null)
            boundRun = GameApp.I?.Run;

        if (boundRun == null)
        {
            if (logError)
                Debug.LogError("[MissionOverlay] RunServices is null.", this);
            return false;
        }

        if (!runRevisionSubscribed)
        {
            subscriptions.Add(boundRun.UiRevision.Subscribe(_ => HandleRunUiRevision(), pushCurrent: false));
            runRevisionSubscribed = true;
        }

        return true;
    }

    void RenderTags(IReadOnlyList<string> tags)
    {
        int validCount = 0;
        if (tags != null)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(tags[i]))
                    continue;
                validCount++;
            }
        }

        GrowTagPool(validCount);
        int renderIndex = 0;
        if (tags != null)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                string tag = tags[i];
                if (string.IsNullOrWhiteSpace(tag))
                    continue;

                TMP_Text chip = tagPool[renderIndex];
                chip.gameObject.SetActive(true);
                chip.text = tag.Trim();
                chip.color = Colors.Semantic.TextPrimary;
                renderIndex++;
            }
        }

        for (int i = renderIndex; i < tagPool.Count; i++)
            tagPool[i].gameObject.SetActive(false);

        if (tagRoot != null)
            tagRoot.gameObject.SetActive(validCount > 0);
    }

    void RenderTestDice(MissionInstance mission, MissionDef missionDef)
    {
        int count = missionDef?.abilityTests?.Count ?? 0;
        GrowTestDicePool(count);

        for (int i = 0; i < testDicePool.Count; i++)
        {
            bool active = i < count;
            MissionOverlayTestDiceView view = testDicePool[i];
            view.gameObject.SetActive(active);
            if (!active)
                continue;

            AbilityTestDef test = missionDef.abilityTests[i];
            var data = new MissionOverlayTestDiceData
            {
                value = Math.Max(0, test?.difficulty ?? 0),
                isCleared = IsTestCleared(mission, i),
                requiredAbilities = new List<string>()
            };

            if (test?.requiredAbilities != null)
            {
                for (int abilityIndex = 0; abilityIndex < test.requiredAbilities.Count; abilityIndex++)
                {
                    string abilityId = test.requiredAbilities[abilityIndex];
                    if (string.IsNullOrWhiteSpace(abilityId))
                        continue;
                    data.requiredAbilities.Add(abilityId);
                }
            }

            view.SetData(data, iconRegistry);
        }
    }

    void RenderPartyStats(RunState state, int effectiveLimit)
    {
        MissionPartyTotalsData totals = BuildPartyTotals(state, effectiveLimit);

        GrowStatDicePool(3);
        for (int i = 0; i < statDicePool.Count; i++)
            statDicePool[i].gameObject.SetActive(i < 3);

        statDicePool[0].SetData(new MissionOverlayStatDiceData { abilityId = StrengthAbilityId, value = totals.strength }, iconRegistry);
        statDicePool[1].SetData(new MissionOverlayStatDiceData { abilityId = AgilityAbilityId, value = totals.agility }, iconRegistry);
        statDicePool[2].SetData(new MissionOverlayStatDiceData { abilityId = IntelligenceAbilityId, value = totals.intelligence }, iconRegistry);
    }

    void RenderSlotCells(RunState state, int effectiveLimit)
    {
        GrowSlotCellPool(MaxVisibleSlots);
        for (int i = 0; i < slotCellPool.Count; i++)
        {
            bool usable = i < effectiveLimit;
            string adventurerUid = usable ? draftSlots[i] : string.Empty;
            bool hasAssigned = usable && !string.IsNullOrWhiteSpace(adventurerUid);
            Sprite portrait = ResolvePortraitSprite(state, adventurerUid);

            var data = new MissionOverlaySlotCellData
            {
                slotIndex = i,
                isUsable = usable,
                hasAssigned = hasAssigned,
                assignedAdventurerUid = adventurerUid,
                portraitSprite = portrait
            };

            slotCellPool[i].gameObject.SetActive(true);
            slotCellPool[i].SetData(data);
        }
    }

    void RenderStartButton(MissionInstance mission, int effectiveLimit)
    {
        bool canStart = !mission.isPartyLocked && !mission.isExpeditionInProgress && CountAssignedInDraft(effectiveLimit) > 0;
        startExpeditionButton.interactable = canStart;
    }

    void EnsureDraftState(MissionInstance mission, int effectiveLimit)
    {
        EnsureDraftSlotCapacity();
        bool missionChanged = !string.Equals(draftMissionUid, mission.uid, StringComparison.Ordinal);
        if (missionChanged)
        {
            draftMissionUid = mission.uid;
            for (int i = 0; i < draftSlots.Count; i++)
                draftSlots[i] = string.Empty;

            if (mission.assignedAdventurerUids != null)
            {
                int copyCount = Math.Min(Math.Min(effectiveLimit, MaxVisibleSlots), mission.assignedAdventurerUids.Count);
                for (int i = 0; i < copyCount; i++)
                    draftSlots[i] = mission.assignedAdventurerUids[i];
            }
        }

        for (int i = effectiveLimit; i < draftSlots.Count; i++)
            draftSlots[i] = string.Empty;
    }

    void EnsureDraftSlotCapacity()
    {
        if (draftSlots.Count == MaxVisibleSlots)
            return;

        draftSlots.Clear();
        for (int i = 0; i < MaxVisibleSlots; i++)
            draftSlots.Add(string.Empty);
    }

    void ResetDraftState()
    {
        draftMissionUid = string.Empty;
        draftSlots.Clear();
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

    int CountAssignedInDraft(int effectiveLimit)
    {
        int count = 0;
        int max = Mathf.Clamp(effectiveLimit, 0, Math.Min(MaxVisibleSlots, draftSlots.Count));
        for (int i = 0; i < max; i++)
        {
            if (!string.IsNullOrWhiteSpace(draftSlots[i]))
                count++;
        }

        return count;
    }

    List<string> BuildCommitList(int effectiveLimit)
    {
        var list = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        int max = Mathf.Clamp(effectiveLimit, 0, Math.Min(MaxVisibleSlots, draftSlots.Count));
        for (int i = 0; i < max; i++)
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

    MissionPartyTotalsData BuildPartyTotals(RunState state, int effectiveLimit)
    {
        var totals = new MissionPartyTotalsData();
        if (state == null)
            return totals;

        int max = Mathf.Clamp(effectiveLimit, 0, Math.Min(MaxVisibleSlots, draftSlots.Count));
        for (int i = 0; i < max; i++)
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

    static int GetEffectivePartyLimit(MissionDef missionDef)
    {
        int configured = missionDef == null ? 1 : missionDef.partyLimit;
        return Mathf.Clamp(Math.Max(1, configured), 1, MaxVisibleSlots);
    }

    Sprite ResolvePortraitSprite(RunState state, string adventurerUid)
    {
        return null;
    }

    bool TryGetActiveMission(RunState state, out MissionInstance mission, out MissionDef missionDef)
    {
        mission = null;
        missionDef = null;
        if (state?.missions == null)
            return false;

        if (string.IsNullOrWhiteSpace(state.activeMissionUid))
            return false;

        if (!TryGetMission(state, state.activeMissionUid, out mission))
            return false;

        if (StaticDataLoader.Current == null)
            return false;

        return StaticDataLoader.Current.TryGetMissionDef(mission.missionId, out missionDef);
    }

    static bool TryGetMission(RunState state, string missionUid, out MissionInstance mission)
    {
        mission = null;
        if (state?.missions == null || string.IsNullOrWhiteSpace(missionUid))
            return false;

        for (int i = 0; i < state.missions.Count; i++)
        {
            MissionInstance candidate = state.missions[i];
            if (candidate == null)
                continue;

            if (!string.Equals(candidate.uid, missionUid, StringComparison.Ordinal))
                continue;

            mission = candidate;
            return true;
        }

        return false;
    }

    static bool TryGetAdventurer(RunState state, string adventurerUid, out AdventurerInstance adventurer)
    {
        adventurer = null;
        if (state?.adventurers == null || string.IsNullOrWhiteSpace(adventurerUid))
            return false;

        for (int i = 0; i < state.adventurers.Count; i++)
        {
            AdventurerInstance candidate = state.adventurers[i];
            if (candidate == null)
                continue;

            if (!string.Equals(candidate.uid, adventurerUid, StringComparison.Ordinal))
                continue;

            adventurer = candidate;
            return true;
        }

        return false;
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

    void GrowTestDicePool(int requiredCount)
    {
        while (testDicePool.Count < requiredCount)
        {
            MissionOverlayTestDiceView created = Instantiate(testDicePrefab, testDiceRoot);
            created.gameObject.SetActive(true);
            testDicePool.Add(created);
        }
    }

    void GrowStatDicePool(int requiredCount)
    {
        while (statDicePool.Count < requiredCount)
        {
            MissionOverlayStatDiceView created = Instantiate(partyStatDicePrefab, partyStatDiceRoot);
            created.gameObject.SetActive(true);
            statDicePool.Add(created);
        }
    }

    void GrowSlotCellPool(int requiredCount)
    {
        while (slotCellPool.Count < requiredCount)
        {
            MissionOverlaySlotCellView created = Instantiate(slotCellPrefab, slotCellRoot);
            created.gameObject.SetActive(true);
            slotCellPool.Add(created);
        }
    }

    bool ValidateReferences()
    {
        bool valid = true;

        if (panelRoot == null)
        {
            Debug.LogError("[MissionOverlay] panelRoot is not assigned.", this);
            valid = false;
        }

        if (closeButton == null)
        {
            Debug.LogError("[MissionOverlay] closeButton is not assigned.", this);
            valid = false;
        }

        if (startExpeditionButton == null)
        {
            Debug.LogError("[MissionOverlay] startExpeditionButton is not assigned.", this);
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

        if (testDiceRoot == null)
        {
            Debug.LogError("[MissionOverlay] testDiceRoot is not assigned.", this);
            valid = false;
        }

        if (testDicePrefab == null)
        {
            Debug.LogError("[MissionOverlay] testDicePrefab is not assigned.", this);
            valid = false;
        }

        if (partyStatDiceRoot == null)
        {
            Debug.LogError("[MissionOverlay] partyStatDiceRoot is not assigned.", this);
            valid = false;
        }

        if (partyStatDicePrefab == null)
        {
            Debug.LogError("[MissionOverlay] partyStatDicePrefab is not assigned.", this);
            valid = false;
        }

        if (slotCellRoot == null)
        {
            Debug.LogError("[MissionOverlay] slotCellRoot is not assigned.", this);
            valid = false;
        }

        if (slotCellPrefab == null)
        {
            Debug.LogError("[MissionOverlay] slotCellPrefab is not assigned.", this);
            valid = false;
        }

        if (rewardSummaryText == null)
        {
            Debug.LogError("[MissionOverlay] rewardSummaryText is not assigned.", this);
            valid = false;
        }

        if (failureSummaryText == null)
        {
            Debug.LogError("[MissionOverlay] failureSummaryText is not assigned.", this);
            valid = false;
        }

        if (iconRegistry == null)
        {
            Debug.LogError("[MissionOverlay] iconRegistry is not assigned.", this);
            valid = false;
        }

        return valid;
    }
}
