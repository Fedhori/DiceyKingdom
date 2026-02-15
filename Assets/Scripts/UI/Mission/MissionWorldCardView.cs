using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MissionWorldCardView : MonoBehaviour
{
    [SerializeField] Button cardButton;
    [SerializeField] Image selectedHighlight;
    [SerializeField] TMP_Text missionNameText;
    [SerializeField] TMP_Text deadlineText;
    [SerializeField] TMP_Text partyLimitText;
    [SerializeField] Transform testRowRoot;
    [SerializeField] MissionWorldTestRowView testRowPrefab;
    readonly List<MissionWorldTestRowView> rowPool = new();
    Action<string> onCardClicked;
    string missionUid = string.Empty;
    bool setupValid;

    void Awake()
    {
        setupValid = ValidateReferences();
        if (!setupValid)
        {
            enabled = false;
            return;
        }

        cardButton.onClick.AddListener(HandleClick);
    }

    public void SetData(MissionWorldCardData data, MissionIconRegistry iconRegistry, Action<string> clickHandler)
    {
        if (!setupValid || data == null || iconRegistry == null)
            return;

        onCardClicked = clickHandler;
        missionUid = data.missionUid ?? string.Empty;

        missionNameText.overflowMode = TextOverflowModes.Ellipsis;
        missionNameText.text = string.IsNullOrWhiteSpace(data.missionName) ? "Mission" : data.missionName;
        deadlineText.text = $"기한: {Mathf.Max(0, data.remainingDeadlineTurns)}T";
        partyLimitText.text = $"배치 인원: {Mathf.Max(0, data.displayedPartyLimit)}";

        if (selectedHighlight != null)
            selectedHighlight.enabled = data.isSelected;

        int testCount = data.tests?.Count ?? 0;
        GrowRowPool(testCount);

        for (int i = 0; i < rowPool.Count; i++)
        {
            bool active = i < testCount;
            MissionWorldTestRowView row = rowPool[i];
            row.gameObject.SetActive(active);
            if (active)
                row.SetData(data.tests[i], iconRegistry);
        }
    }

    bool ValidateReferences()
    {
        bool valid = true;
        if (cardButton == null)
        {
            Debug.LogError("[MissionWorld] cardButton is not assigned.", this);
            valid = false;
        }

        if (missionNameText == null)
        {
            Debug.LogError("[MissionWorld] missionNameText is not assigned.", this);
            valid = false;
        }

        if (deadlineText == null)
        {
            Debug.LogError("[MissionWorld] deadlineText is not assigned.", this);
            valid = false;
        }

        if (partyLimitText == null)
        {
            Debug.LogError("[MissionWorld] partyLimitText is not assigned.", this);
            valid = false;
        }

        if (testRowRoot == null)
        {
            Debug.LogError("[MissionWorld] testRowRoot is not assigned.", this);
            valid = false;
        }

        if (testRowPrefab == null)
        {
            Debug.LogError("[MissionWorld] testRowPrefab is not assigned.", this);
            valid = false;
        }

        return valid;
    }

    void GrowRowPool(int requiredCount)
    {
        while (rowPool.Count < requiredCount)
        {
            MissionWorldTestRowView created = Instantiate(testRowPrefab, testRowRoot);
            created.gameObject.SetActive(true);
            rowPool.Add(created);
        }
    }

    void HandleClick()
    {
        if (string.IsNullOrWhiteSpace(missionUid))
            return;

        onCardClicked?.Invoke(missionUid);
    }
}
