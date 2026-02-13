using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BottomActionBarController : MonoBehaviour
{
    static readonly string[] SkillHotkeys = { "Q", "W", "E", "R" };

    [SerializeField] RectTransform contentRoot;
    [SerializeField] int visibleSkillSlotCount = 4;
    [SerializeField] string titleTable = "UI";
    [SerializeField] string titleKey = "assignment.unassigned.title";
    [SerializeField] string messageTable = "UI";
    [SerializeField] string messageKey = "assignment.unassigned.message";
    [SerializeField] Color barBackgroundColor = new(0.10f, 0.12f, 0.15f, 0.90f);
    [SerializeField] Color slotReadyColor = new(0.20f, 0.38f, 0.64f, 0.96f);
    [SerializeField] Color slotCooldownColor = new(0.20f, 0.23f, 0.29f, 0.96f);
    [SerializeField] Color slotBlockedColor = new(0.24f, 0.19f, 0.17f, 0.96f);
    [SerializeField] Color slotEmptyColor = new(0.16f, 0.18f, 0.22f, 0.82f);
    [SerializeField] Color commitReadyColor = new(0.18f, 0.48f, 0.30f, 0.95f);
    [SerializeField] Color commitBlockedColor = new(0.24f, 0.26f, 0.30f, 0.86f);
    [SerializeField] Color labelColor = new(0.94f, 0.97f, 1.00f, 1.00f);
    [SerializeField] Color subtleLabelColor = new(0.76f, 0.82f, 0.90f, 1.00f);

    readonly List<SkillSlotWidgets> skillSlots = new();
    readonly Dictionary<string, SkillDef> skillDefById = new(StringComparer.Ordinal);

    RectTransform bodyRoot;
    RectTransform skillRowRoot;
    Button commitButton;
    Image commitButtonImage;
    TextMeshProUGUI commitButtonText;
    bool loadedSkillDefs;

    void Awake()
    {
        TryResolveContentRoot();
        LoadSkillDefsIfNeeded();
        BindVisualTree();
        WireButtons();
    }

    void Start()
    {
        SubscribeEvents();
        RefreshView();
    }

    void OnDestroy()
    {
        UnsubscribeEvents();

        if (commitButton != null)
            commitButton.onClick.RemoveListener(OnCommitButtonPressed);

        for (int i = 0; i < skillSlots.Count; i++)
        {
            var slot = skillSlots[i];
            if (slot?.button == null)
                continue;

            slot.button.onClick.RemoveAllListeners();
        }

        if (SkillTargetingSession.IsFor(GameManager.Instance))
            SkillTargetingSession.Cancel();
    }

    void OnRunStarted(GameRunState _)
    {
        RefreshView();
    }

    void OnPhaseChanged(TurnPhase _)
    {
        RefreshView();
    }

    void OnRunEnded(GameRunState _)
    {
        RefreshView();
    }

    void OnTargetingSessionChanged()
    {
        RefreshView();
    }

    void SubscribeEvents()
    {
        GameManager.Instance.RunStarted -= OnRunStarted;
        GameManager.Instance.RunEnded -= OnRunEnded;
        PhaseManager.Instance.PhaseChanged -= OnPhaseChanged;
        SkillTargetingSession.SessionChanged -= OnTargetingSessionChanged;

        GameManager.Instance.RunStarted += OnRunStarted;
        GameManager.Instance.RunEnded += OnRunEnded;
        PhaseManager.Instance.PhaseChanged += OnPhaseChanged;
        SkillTargetingSession.SessionChanged += OnTargetingSessionChanged;
    }

    void UnsubscribeEvents()
    {
        GameManager.Instance.RunStarted -= OnRunStarted;
        GameManager.Instance.RunEnded -= OnRunEnded;
        PhaseManager.Instance.PhaseChanged -= OnPhaseChanged;
        SkillTargetingSession.SessionChanged -= OnTargetingSessionChanged;
    }

    void RefreshView()
    {
        if (contentRoot == null)
            return;

        EnsureTargetingSessionValidity();
        RefreshSkillSlots();
        RefreshCommitButton();
    }

    void RefreshSkillSlots()
    {
        int requestedSlotCount = Mathf.Clamp(visibleSkillSlotCount, 1, SkillHotkeys.Length);
        int renderedSlotCount = Mathf.Min(requestedSlotCount, skillSlots.Count);

        for (int index = 0; index < skillSlots.Count; index++)
        {
            var slot = skillSlots[index];
            if (slot == null)
                continue;

            bool visible = index < renderedSlotCount;
            if (slot.root != null)
                slot.root.gameObject.SetActive(visible);
            if (!visible)
                continue;

            if (slot.hotkeyText != null)
                slot.hotkeyText.text = $"[{SkillHotkeys[index]}]";

            var cooldown = GetCooldownBySlotIndex(index);
            if (cooldown == null || string.IsNullOrWhiteSpace(cooldown.skillDefId))
            {
                if (slot.nameText != null)
                    slot.nameText.text = "Empty";
                if (slot.statusText != null)
                    slot.statusText.text = "-";
                if (slot.background != null)
                    slot.background.color = slotEmptyColor;
                if (slot.button != null)
                    slot.button.interactable = false;
                continue;
            }

            if (slot.nameText != null)
                slot.nameText.text = ResolveSkillDisplayName(cooldown.skillDefId);

            bool baseUsable = GameManager.Instance.CanUseSkillBySlotIndex(index);
            bool canQuickCast = baseUsable && CanQuickCastWithoutAdditionalTarget(cooldown.skillDefId);
            bool isTargetingThisSlot = SkillTargetingSession.IsFor(GameManager.Instance, index);

            if (slot.button != null)
                slot.button.interactable = baseUsable;
            if (slot.statusText != null)
                slot.statusText.text = BuildSkillStatusText(cooldown, baseUsable, canQuickCast, isTargetingThisSlot);
            if (slot.background != null)
                slot.background.color = ResolveSkillSlotColor(cooldown, baseUsable, canQuickCast, isTargetingThisSlot);
        }
    }

    void RefreshCommitButton()
    {
        if (commitButton == null || commitButtonText == null || commitButtonImage == null)
            return;

        bool canCommit = CanRequestCommitByPhase();
        int pendingCount = AgentManager.Instance.GetUnassignedAgentCount();

        commitButton.interactable = canCommit;
        commitButtonImage.color = canCommit ? commitReadyColor : commitBlockedColor;

        if (!canCommit)
        {
            commitButtonText.text = "End Turn [Space]";
            return;
        }

        if (pendingCount > 0)
            commitButtonText.text = $"End Turn [Space] ({pendingCount} pending)";
        else
            commitButtonText.text = "End Turn [Space]";
    }

    void OnSkillSlotPressed(int skillSlotIndex)
    {
        var cooldown = GetCooldownBySlotIndex(skillSlotIndex);
        if (cooldown == null || string.IsNullOrWhiteSpace(cooldown.skillDefId))
            return;
        if (!GameManager.Instance.CanUseSkillBySlotIndex(skillSlotIndex))
            return;

        bool canQuickCast = CanQuickCastWithoutAdditionalTarget(cooldown.skillDefId);
        if (canQuickCast)
        {
            bool used = GameManager.Instance.TryUseSkillBySlotIndex(skillSlotIndex);
            if (used && SkillTargetingSession.IsFor(GameManager.Instance, skillSlotIndex))
                SkillTargetingSession.Cancel();
            return;
        }

        if (SkillTargetingSession.IsFor(GameManager.Instance, skillSlotIndex))
        {
            SkillTargetingSession.Cancel();
            return;
        }

        SkillTargetingSession.Begin(GameManager.Instance, skillSlotIndex);
    }

    void OnCommitButtonPressed()
    {
        RequestCommitWithConfirmation();
    }

    string ResolveSkillDisplayName(string skillDefId)
    {
        if (string.IsNullOrWhiteSpace(skillDefId))
            return "Unknown Skill";
        if (!skillDefById.TryGetValue(skillDefId, out var skillDef))
            return ToDisplayTitle(skillDefId);
        if (!string.IsNullOrWhiteSpace(skillDef.skillId))
            return ToDisplayTitle(skillDef.skillId);
        if (!string.IsNullOrWhiteSpace(skillDef.nameKey))
            return ToDisplayTitle(skillDef.nameKey);
        return ToDisplayTitle(skillDefId);
    }

    SkillCooldownState GetCooldownBySlotIndex(int slotIndex)
    {
        if (GameManager.Instance?.CurrentRunState?.skillCooldowns == null)
            return null;
        if (slotIndex < 0 || slotIndex >= GameManager.Instance.CurrentRunState.skillCooldowns.Count)
            return null;

        return GameManager.Instance.CurrentRunState.skillCooldowns[slotIndex];
    }

    bool CanQuickCastWithoutAdditionalTarget(string skillDefId)
    {
        if (string.IsNullOrWhiteSpace(skillDefId))
            return false;
        if (!skillDefById.TryGetValue(skillDefId, out var skillDef))
            return false;
        if (skillDef.effectBundle?.effects == null)
            return false;

        for (int index = 0; index < skillDef.effectBundle.effects.Count; index++)
        {
            var effect = skillDef.effectBundle.effects[index];
            if (effect == null || string.IsNullOrWhiteSpace(effect.effectType))
                return false;

            string effectType = effect.effectType.Trim();
            if (effectType == "situationRequirementDelta")
                return false;

            if (effectType == "dieFaceDelta")
            {
                string diePickRule = GetParamString(effect.effectParams, "diePickRule");
                if (diePickRule == "selected")
                    return false;
            }

            if (effectType == "rerollAgentDice")
            {
                string rerollRule = GetParamString(effect.effectParams, "rerollRule");
                if (rerollRule == "single")
                    return false;
            }
        }

        return true;
    }

    bool SkillRequiresSituationTarget(string skillDefId)
    {
        if (string.IsNullOrWhiteSpace(skillDefId))
            return false;
        if (!skillDefById.TryGetValue(skillDefId, out var skillDef))
            return false;
        if (skillDef.effectBundle?.effects == null)
            return false;

        for (int index = 0; index < skillDef.effectBundle.effects.Count; index++)
        {
            var effect = skillDef.effectBundle.effects[index];
            if (effect == null || string.IsNullOrWhiteSpace(effect.effectType))
                continue;
            if (string.Equals(effect.effectType.Trim(), "situationRequirementDelta", StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    bool SkillRequiresSelectedDieTarget(string skillDefId)
    {
        if (string.IsNullOrWhiteSpace(skillDefId))
            return false;
        if (!skillDefById.TryGetValue(skillDefId, out var skillDef))
            return false;
        if (skillDef.effectBundle?.effects == null)
            return false;

        for (int index = 0; index < skillDef.effectBundle.effects.Count; index++)
        {
            var effect = skillDef.effectBundle.effects[index];
            if (effect == null || string.IsNullOrWhiteSpace(effect.effectType))
                continue;

            string effectType = effect.effectType.Trim();
            if (effectType == "dieFaceDelta")
            {
                string diePickRule = GetParamString(effect.effectParams, "diePickRule");
                if (diePickRule == "selected")
                    return true;
            }

            if (effectType == "rerollAgentDice")
            {
                string rerollRule = GetParamString(effect.effectParams, "rerollRule");
                if (rerollRule == "single")
                    return true;
            }
        }

        return false;
    }

    string BuildSelectTargetStatus(string skillDefId)
    {
        bool needsSituation = SkillRequiresSituationTarget(skillDefId);
        bool needsDie = SkillRequiresSelectedDieTarget(skillDefId);
        if (needsSituation && needsDie)
            return "Select Target";
        if (needsSituation)
            return "Select Situation";
        if (needsDie)
            return "Select Die";
        return "Select Target";
    }

    string BuildNeedTargetStatus(string skillDefId)
    {
        bool needsSituation = SkillRequiresSituationTarget(skillDefId);
        bool needsDie = SkillRequiresSelectedDieTarget(skillDefId);
        if (needsSituation && needsDie)
            return "Need Target";
        if (needsSituation)
            return "Need Situation";
        if (needsDie)
            return "Need Die";
        return "Need Target";
    }

    string BuildSkillStatusText(
        SkillCooldownState cooldown,
        bool baseUsable,
        bool canQuickCast,
        bool isTargetingThisSlot)
    {
        if (cooldown == null)
            return "-";
        if (isTargetingThisSlot)
            return BuildSelectTargetStatus(cooldown.skillDefId);
        if (cooldown.cooldownRemainingTurns > 0)
            return $"CD {cooldown.cooldownRemainingTurns}";
        if (cooldown.usedThisTurn)
            return "Used";
        if (canQuickCast)
            return "Ready";
        if (baseUsable)
            return BuildNeedTargetStatus(cooldown.skillDefId);
        if (cooldown.cooldownRemainingTurns == 0 && !cooldown.usedThisTurn)
            return "Disabled";
        return "Blocked";
    }

    Color ResolveSkillSlotColor(
        SkillCooldownState cooldown,
        bool baseUsable,
        bool canQuickCast,
        bool isTargetingThisSlot)
    {
        if (cooldown == null)
            return slotEmptyColor;
        if (isTargetingThisSlot)
            return slotReadyColor;
        if (canQuickCast)
            return slotReadyColor;
        if (cooldown.cooldownRemainingTurns > 0 || cooldown.usedThisTurn)
            return slotCooldownColor;
        if (baseUsable)
            return slotBlockedColor;
        return slotCooldownColor;
    }

    void EnsureTargetingSessionValidity()
    {
        if (!SkillTargetingSession.IsFor(GameManager.Instance))
            return;

        int slotIndex = SkillTargetingSession.ActiveSkillSlotIndex;
        var cooldown = GetCooldownBySlotIndex(slotIndex);
        if (cooldown == null || string.IsNullOrWhiteSpace(cooldown.skillDefId))
        {
            SkillTargetingSession.Cancel();
            return;
        }

        if (!GameManager.Instance.CanUseSkillBySlotIndex(slotIndex))
        {
            SkillTargetingSession.Cancel();
            return;
        }

        if (CanQuickCastWithoutAdditionalTarget(cooldown.skillDefId))
            SkillTargetingSession.Cancel();
    }

    bool CanRequestCommitByPhase()
    {
        if (GameManager.Instance?.CurrentRunState == null)
            return false;

        var phase = PhaseManager.Instance.CurrentPhase;
        return phase == TurnPhase.AgentRoll ||
               phase == TurnPhase.Adjustment ||
               phase == TurnPhase.TargetAndAttack;
    }

    void RequestCommitWithConfirmation()
    {
        int pendingCount = PhaseManager.Instance.RequestCommitAssignmentPhase();
        if (pendingCount <= 0)
            return;

        var modal = ModalManager.Instance;
        var messageArgs = new Dictionary<string, object>
        {
            { "count", pendingCount }
        };

        modal.ShowConfirmation(
            titleTable,
            titleKey,
            messageTable,
            messageKey,
            onConfirm: () => { PhaseManager.Instance.ConfirmCommitAssignmentPhase(); },
            onCancel: null,
            messageArgs: messageArgs);
    }

    void TryResolveContentRoot()
    {
        if (contentRoot != null)
            return;

        contentRoot = transform as RectTransform;
    }

    void LoadSkillDefsIfNeeded()
    {
        if (loadedSkillDefs)
            return;
        loadedSkillDefs = true;

        skillDefById.Clear();

        try
        {
            var defs = GameStaticDataLoader.LoadSkillDefs();
            for (int index = 0; index < defs.Count; index++)
            {
                var def = defs[index];
                if (def == null || string.IsNullOrWhiteSpace(def.skillId))
                    continue;

                skillDefById[def.skillId] = def;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[BottomActionBarController] Failed to load skill defs: {exception.Message}");
        }
    }

    void BindVisualTree()
    {
        if (contentRoot == null)
            return;

        var background = contentRoot.GetComponent<Image>();
        if (background != null)
        {
            background.color = barBackgroundColor;
            background.raycastTarget = true;
        }

        bodyRoot = FindRectChild(contentRoot, "ActionBarBody");
        skillRowRoot = bodyRoot != null ? FindRectChild(bodyRoot, "SkillRow") : null;

        var commitRoot = bodyRoot != null ? FindRectChild(bodyRoot, "CommitButton") : null;
        if (commitRoot != null)
        {
            commitButton = commitRoot.GetComponent<Button>();
            commitButtonImage = commitRoot.GetComponent<Image>();
            var commitTextRoot = FindRectChild(commitRoot, "CommitText");
            if (commitTextRoot != null)
                commitButtonText = commitTextRoot.GetComponent<TextMeshProUGUI>();
        }

        CollectSkillSlots();
    }

    void WireButtons()
    {
        if (commitButton != null)
        {
            commitButton.onClick.RemoveListener(OnCommitButtonPressed);
            commitButton.onClick.AddListener(OnCommitButtonPressed);
        }

        for (int index = 0; index < skillSlots.Count; index++)
        {
            var slot = skillSlots[index];
            if (slot?.button == null)
                continue;

            int capturedIndex = index;
            slot.button.onClick.RemoveAllListeners();
            slot.button.onClick.AddListener(() => OnSkillSlotPressed(capturedIndex));
        }
    }

    void CollectSkillSlots()
    {
        skillSlots.Clear();
        if (skillRowRoot == null)
            return;

        var unordered = new List<(int order, SkillSlotWidgets slot)>();
        for (int i = 0; i < skillRowRoot.childCount; i++)
        {
            var child = skillRowRoot.GetChild(i) as RectTransform;
            if (child == null)
                continue;
            if (!child.name.StartsWith("SkillSlot_", StringComparison.Ordinal))
                continue;

            var slot = new SkillSlotWidgets
            {
                root = child,
                button = child.GetComponent<Button>(),
                background = child.GetComponent<Image>(),
                hotkeyText = FindTextChild(child, "HotkeyText"),
                nameText = FindTextChild(child, "NameText"),
                statusText = FindTextChild(child, "StatusText")
            };

            int order = ParseSlotOrder(child.name, i);
            unordered.Add((order, slot));
        }

        unordered.Sort((a, b) => a.order.CompareTo(b.order));
        for (int i = 0; i < unordered.Count; i++)
            skillSlots.Add(unordered[i].slot);
    }

    static int ParseSlotOrder(string name, int fallback)
    {
        if (string.IsNullOrWhiteSpace(name))
            return fallback;

        int underscoreIndex = name.LastIndexOf('_');
        if (underscoreIndex < 0 || underscoreIndex >= name.Length - 1)
            return fallback;

        string suffix = name.Substring(underscoreIndex + 1);
        if (!int.TryParse(suffix, out int parsed))
            return fallback;

        return parsed;
    }

    static RectTransform FindRectChild(RectTransform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
            return null;

        var child = parent.Find(childName) as RectTransform;
        return child;
    }

    static TextMeshProUGUI FindTextChild(RectTransform parent, string childName)
    {
        var child = FindRectChild(parent, childName);
        if (child == null)
            return null;

        return child.GetComponent<TextMeshProUGUI>();
    }

    static string ToDisplayTitle(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        string normalized = raw.Trim();
        if (normalized.StartsWith("skill.", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring("skill.".Length);
        if (normalized.StartsWith("skill_", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring("skill_".Length);
        if (normalized.EndsWith(".name", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring(0, normalized.Length - ".name".Length);

        normalized = normalized.Replace('_', ' ').Replace('.', ' ');
        var parts = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return normalized;

        for (int index = 0; index < parts.Length; index++)
        {
            var part = parts[index];
            if (part.Length == 0)
                continue;

            if (part.Length == 1)
            {
                parts[index] = part.ToUpperInvariant();
                continue;
            }

            parts[index] = char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant();
        }

        return string.Join(" ", parts);
    }

    static string GetParamString(Newtonsoft.Json.Linq.JObject effectParams, string key)
    {
        if (effectParams == null || string.IsNullOrWhiteSpace(key))
            return string.Empty;

        var token = effectParams[key];
        if (token == null || token.Type == Newtonsoft.Json.Linq.JTokenType.Null)
            return string.Empty;

        return token.ToString().Trim();
    }

    sealed class SkillSlotWidgets
    {
        public RectTransform root;
        public Button button;
        public Image background;
        public TextMeshProUGUI hotkeyText;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI statusText;
    }
}
