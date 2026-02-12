using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class AgentController : MonoBehaviour
{
    [SerializeField] RectTransform rootRect;
    [SerializeField] Image backgroundImage;
    [SerializeField] AgentDragHandle dragHandle;
    [SerializeField] RectTransform diceRowRoot;
    [SerializeField] TextMeshProUGUI slotText;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI infoText;
    [SerializeField] TextMeshProUGUI expectedDamageText;
    [SerializeField] TextMeshProUGUI statusText;
    [SerializeField] TextMeshProUGUI rollButtonText;
    [SerializeField] Button rollButton;
    [SerializeField] AgentRollButton rollButtonHandler;
    GameObject dicePrefab;
    GameTurnOrchestrator orchestrator;
    string agentInstanceId = string.Empty;
    readonly List<DiceFaceWidgets> diceFaces = new();

    public RectTransform RootRect => rootRect;

    public void SetDicePrefab(GameObject prefab)
    {
        dicePrefab = prefab;
    }

    public void BindOrchestrator(GameTurnOrchestrator orchestrator)
    {
        this.orchestrator = orchestrator;

        if (dragHandle != null)
            dragHandle.SetOrchestrator(orchestrator);
        if (rollButtonHandler != null)
            rollButtonHandler.SetOrchestrator(orchestrator);
        if (rollButtonHandler != null)
            rollButtonHandler.SetButton(rollButton);
        if (rollButton != null && rollButtonHandler != null)
        {
            rollButton.onClick.RemoveListener(rollButtonHandler.OnRollPressed);
            rollButton.onClick.AddListener(rollButtonHandler.OnRollPressed);
        }
    }

    public void BindAgent(string agentInstanceId)
    {
        this.agentInstanceId = agentInstanceId ?? string.Empty;

        if (dragHandle != null)
            dragHandle.SetAgentInstanceId(agentInstanceId);
        if (rollButtonHandler != null)
            rollButtonHandler.SetAgentInstanceId(agentInstanceId);
    }

    public void OnDiceFacePressed(int dieIndex)
    {
        if (dieIndex < 0)
            return;
        if (AssignmentDragSession.IsActive)
            return;
        if (!SkillTargetingSession.IsActive)
            return;
        if (orchestrator == null)
            return;
        if (!SkillTargetingSession.IsFor(orchestrator))
            return;
        if (string.IsNullOrWhiteSpace(agentInstanceId))
            return;

        SkillTargetingSession.TryConsumeAgentDieTarget(agentInstanceId, dieIndex);
    }

    public void Render(
        string slotLabel,
        string nameLabel,
        string infoLabel,
        string expectedDamageLabel,
        string statusLabel,
        string rollLabel,
        Color statusColor,
        Color backgroundColor,
        IReadOnlyList<int> rolledDiceValues,
        int diceCount)
    {
        if (slotText != null)
            slotText.text = slotLabel ?? string.Empty;
        if (nameText != null)
            nameText.text = nameLabel ?? string.Empty;
        if (infoText != null)
            infoText.text = infoLabel ?? string.Empty;
        if (expectedDamageText != null)
            expectedDamageText.text = expectedDamageLabel ?? string.Empty;
        if (statusText != null)
        {
            statusText.text = statusLabel ?? string.Empty;
            statusText.color = statusColor;
        }

        if (rollButtonText != null)
            rollButtonText.text = rollLabel ?? string.Empty;
        if (backgroundImage != null)
            backgroundImage.color = backgroundColor;

        RefreshDiceFaces(rolledDiceValues, diceCount);
    }

    void RefreshDiceFaces(IReadOnlyList<int> rolledDiceValues, int diceCount)
    {
        if (diceRowRoot == null)
            return;
        if (diceCount < 1)
            diceCount = 1;

        EnsureDiceFaceCount(diceCount);

        for (int index = 0; index < diceFaces.Count; index++)
        {
            var face = diceFaces[index];
            if (face.valueText == null)
                continue;

            string display = "-";
            if (rolledDiceValues != null && index < rolledDiceValues.Count)
                display = rolledDiceValues[index].ToString();

            face.valueText.text = display;
        }
    }

    void EnsureDiceFaceCount(int targetCount)
    {
        while (diceFaces.Count > targetCount)
        {
            int lastIndex = diceFaces.Count - 1;
            var face = diceFaces[lastIndex];
            if (face.root != null)
            {
                if (Application.isPlaying)
                    Destroy(face.root.gameObject);
                else
                    DestroyImmediate(face.root.gameObject);
            }

            diceFaces.RemoveAt(lastIndex);
        }

        while (diceFaces.Count < targetCount)
        {
            var face = CreateDiceFace(diceFaces.Count);
            if (face == null)
                return;

            diceFaces.Add(face);
        }
    }

    DiceFaceWidgets CreateDiceFace(int index)
    {
        if (dicePrefab == null)
        {
            Debug.LogWarning("[AgentController] dicePrefab is not assigned.", this);
            return null;
        }
        if (diceRowRoot == null)
        {
            Debug.LogWarning("[AgentController] diceRowRoot is not assigned.", this);
            return null;
        }

        var root = Instantiate(dicePrefab, diceRowRoot, false);
        root.name = $"Dice_{index + 1}";

        var rootTransform = root.GetComponent<RectTransform>();
        if (rootTransform == null)
        {
            Debug.LogWarning("[AgentController] Dice prefab must include RectTransform.", root);
            if (Application.isPlaying)
                Destroy(root);
            else
                DestroyImmediate(root);
            return null;
        }

        var valueText = FindTextByName(rootTransform, "ValueText");
        if (valueText == null)
            valueText = root.GetComponentInChildren<TextMeshProUGUI>(true);
        if (valueText == null)
            Debug.LogWarning("[AgentController] Dice prefab is missing a TMP_Text for value.", root);

        var clickTarget = root.GetComponent<AgentDiceFaceClickTarget>();
        if (clickTarget == null)
            clickTarget = root.AddComponent<AgentDiceFaceClickTarget>();
        clickTarget.Bind(this, index);

        return new DiceFaceWidgets
        {
            root = rootTransform,
            valueText = valueText
        };
    }

    static TextMeshProUGUI FindTextByName(RectTransform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name))
            return null;

        var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int index = 0; index < texts.Length; index++)
        {
            var text = texts[index];
            if (text == null)
                continue;
            if (!string.Equals(text.gameObject.name, name, StringComparison.Ordinal))
                continue;

            return text;
        }

        return null;
    }

    sealed class DiceFaceWidgets
    {
        public RectTransform root;
        public TextMeshProUGUI valueText;
    }
}

