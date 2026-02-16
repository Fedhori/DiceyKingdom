using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public sealed class MissionWorldListPresenter : MonoBehaviour
{
    [SerializeField] MissionWorldCardView cardPrefab;
    [SerializeField] Transform cardRoot;
    [SerializeField] MissionIconRegistry iconRegistry;
    readonly List<MissionWorldCardView> cardPool = new();
    readonly DisposableBag subscriptions = new();
    RunServices boundRun;
    MissionOverlayPresenter overlayPresenter;
    bool setupValid;

    void Awake()
    {
        setupValid = ValidateReferences();
        if (!setupValid)
            enabled = false;
    }

    void OnEnable()
    {
        subscriptions.Clear();
        if (!setupValid)
            return;

        boundRun = GameApp.I?.Run;
        if (boundRun == null)
        {
            Debug.LogError("[MissionWorld] RunServices is null. Enable this UI after BeginRun.", this);
            HideAllCards();
            return;
        }

        subscriptions.Add(boundRun.UiRevision.Subscribe(_ => Rebuild()));
    }

    void OnDisable()
    {
        subscriptions.Clear();
        boundRun = null;
    }

    bool ValidateReferences()
    {
        bool valid = true;
        if (cardPrefab == null)
        {
            Debug.LogError("[MissionWorld] cardPrefab is not assigned.", this);
            valid = false;
        }

        if (cardRoot == null)
        {
            Debug.LogError("[MissionWorld] cardRoot is not assigned.", this);
            valid = false;
        }

        if (iconRegistry == null)
        {
            Debug.LogError("[MissionWorld] iconRegistry is not assigned.", this);
            valid = false;
        }

        return valid;
    }

    void Rebuild()
    {
        RunState state = boundRun?.CurrentRunState;
        if (state?.missions == null || state.missions.Count == 0)
        {
            HideAllCards();
            return;
        }

        var sortedMissions = new List<MissionSortEntry>(state.missions.Count);
        for (int i = 0; i < state.missions.Count; i++)
        {
            MissionInstance mission = state.missions[i];
            if (mission == null)
                continue;

            sortedMissions.Add(new MissionSortEntry
            {
                mission = mission,
                originalIndex = i
            });
        }

        sortedMissions.Sort((left, right) =>
        {
            int deadline = left.mission.remainingDeadlineTurns.CompareTo(right.mission.remainingDeadlineTurns);
            if (deadline != 0)
                return deadline;

            return left.originalIndex.CompareTo(right.originalIndex);
        });

        int visibleIndex = 0;
        for (int i = 0; i < sortedMissions.Count; i++)
        {
            GrowCardPool(visibleIndex + 1);
            MissionWorldCardView view = cardPool[visibleIndex];
            view.gameObject.SetActive(true);
            MissionWorldCardData data = BuildCardData(state, sortedMissions[i].mission);
            view.SetData(data, iconRegistry);
            visibleIndex++;
        }

        for (int i = visibleIndex; i < cardPool.Count; i++)
            cardPool[i].gameObject.SetActive(false);
    }

    MissionWorldCardData BuildCardData(RunState state, MissionInstance mission)
    {
        var data = new MissionWorldCardData
        {
            missionUid = mission.uid,
            missionName = BuildDisplayName(mission.missionId),
            remainingDeadlineTurns = mission.remainingDeadlineTurns,
            displayedPartyLimit = 1,
            isSelected = string.Equals(state.activeMissionUid, mission.uid, StringComparison.Ordinal),
            tests = new List<MissionWorldTestData>()
        };

        if (StaticDataLoader.Current == null)
            return data;

        if (!StaticDataLoader.Current.TryGetMissionDef(mission.missionId, out MissionDef missionDef))
            return data;

        data.missionName = BuildDisplayName(missionDef.id);
        data.displayedPartyLimit = Math.Max(1, missionDef.partyLimit);
        IReadOnlyList<AbilityTestDef> tests = missionDef.abilityTests;
        if (tests == null)
            return data;

        for (int testIndex = 0; testIndex < tests.Count; testIndex++)
        {
            AbilityTestDef test = tests[testIndex];
            if (test == null)
                continue;

            var testData = new MissionWorldTestData
            {
                difficulty = test.difficulty,
                isCleared = IsTestCleared(mission, testIndex),
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

        return data;
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

    static string BuildDisplayName(string missionId)
    {
        if (string.IsNullOrWhiteSpace(missionId))
            return "Mission";

        string display = missionId;
        int separator = display.LastIndexOf('.');
        if (separator >= 0 && separator + 1 < display.Length)
            display = display[(separator + 1)..];

        display = display.Replace('_', ' ').Trim();
        if (display.Length == 0)
            return missionId;

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(display);
    }

    void GrowCardPool(int requiredCount)
    {
        while (cardPool.Count < requiredCount)
        {
            MissionWorldCardView created = Instantiate(cardPrefab, cardRoot);
            created.SetClickHandler(HandleCardClicked);
            created.gameObject.SetActive(true);
            cardPool.Add(created);
        }
    }

    void HideAllCards()
    {
        for (int i = 0; i < cardPool.Count; i++)
            cardPool[i].gameObject.SetActive(false);
    }

    void HandleCardClicked(string missionUid)
    {
        if (boundRun == null)
        {
            Debug.LogError("[MissionWorld] Card click ignored: RunServices is null.", this);
            return;
        }

        if (!boundRun.SetActiveMission(missionUid))
        {
            Debug.LogError($"[MissionWorld] Failed to select mission: {missionUid}", this);
            return;
        }

        if (overlayPresenter == null)
        {
            Debug.LogError("[MissionWorld] overlayPresenter is not bound.", this);
            return;
        }

        overlayPresenter.OpenOrFocus(missionUid);
    }

    public void BindOverlayPresenter(MissionOverlayPresenter presenter)
    {
        overlayPresenter = presenter;
    }

    struct MissionSortEntry
    {
        public MissionInstance mission;
        public int originalIndex;
    }
}
