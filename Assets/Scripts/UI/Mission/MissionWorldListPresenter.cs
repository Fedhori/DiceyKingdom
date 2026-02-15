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
            Debug.LogError("[MissionWorldListPresenter] RunServices is null. Enable this UI after BeginRun.", this);
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
            Debug.LogError("[MissionWorldListPresenter] cardPrefab is not assigned.", this);
            valid = false;
        }

        if (cardRoot == null)
        {
            Debug.LogError("[MissionWorldListPresenter] cardRoot is not assigned.", this);
            valid = false;
        }

        if (iconRegistry == null)
        {
            Debug.LogError("[MissionWorldListPresenter] iconRegistry is not assigned.", this);
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

        int visibleIndex = 0;
        for (int i = 0; i < state.missions.Count; i++)
        {
            MissionInstance mission = state.missions[i];
            if (mission == null)
                continue;

            GrowCardPool(visibleIndex + 1);
            MissionWorldCardView view = cardPool[visibleIndex];
            view.gameObject.SetActive(true);
            MissionWorldCardData data = BuildCardData(state, mission);
            view.SetData(data, iconRegistry, HandleCardClicked);
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
            displayedPartyLimit = 2,
            isSelected = string.Equals(state.activeMissionUid, mission.uid, StringComparison.Ordinal),
            tests = new List<MissionWorldTestData>()
        };

        if (StaticDataLoader.Current == null)
            return data;

        if (!StaticDataLoader.Current.TryGetMissionDef(mission.missionId, out MissionDef missionDef))
            return data;

        data.missionName = BuildDisplayName(missionDef.id);
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
            return;

        boundRun.SetActiveMission(missionUid);
    }
}
