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
        EnsureVisualTree();
        LoadSkillDefsIfNeeded();
    }

    void Start()
    {
        SubscribeEvents();
        RefreshView();
    }

    void OnDestroy()
    {
        UnsubscribeEvents();

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
        EnsureSkillSlotCount(requestedSlotCount);

        for (int index = 0; index < skillSlots.Count; index++)
        {
            var slot = skillSlots[index];
            if (slot == null)
                continue;

            slot.hotkeyText.text = $"[{SkillHotkeys[index]}]";

            var cooldown = GetCooldownBySlotIndex(index);
            if (cooldown == null || string.IsNullOrWhiteSpace(cooldown.skillDefId))
            {
                slot.nameText.text = "Empty";
                slot.statusText.text = "-";
                slot.background.color = slotEmptyColor;
                slot.button.interactable = false;
                continue;
            }

            slot.nameText.text = ResolveSkillDisplayName(cooldown.skillDefId);

            bool baseUsable = GameManager.Instance.CanUseSkillBySlotIndex(index);
            bool canQuickCast = baseUsable && CanQuickCastWithoutAdditionalTarget(cooldown.skillDefId);
            bool isTargetingThisSlot = SkillTargetingSession.IsFor(GameManager.Instance, index);

            slot.button.interactable = baseUsable;
            slot.statusText.text = BuildSkillStatusText(cooldown, baseUsable, canQuickCast, isTargetingThisSlot);
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

    void EnsureVisualTree()
    {
        if (contentRoot == null)
            return;

        EnsureBarBackground();
        EnsureBodyRoot();
        EnsureSkillRowRoot();
        EnsureCommitButton();
        EnsureSkillSlotCount(Mathf.Clamp(visibleSkillSlotCount, 1, SkillHotkeys.Length));
    }

    void EnsureBarBackground()
    {
        var image = contentRoot.GetComponent<Image>();
        if (image == null)
            image = contentRoot.gameObject.AddComponent<Image>();

        image.color = barBackgroundColor;
        image.raycastTarget = true;
    }

    void EnsureBodyRoot()
    {
        if (bodyRoot != null)
            return;

        var bodyObject = new GameObject(
            "ActionBarBody",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup));
        bodyObject.layer = LayerMask.NameToLayer("UI");
        bodyRoot = bodyObject.GetComponent<RectTransform>();
        bodyRoot.SetParent(contentRoot, false);
        bodyRoot.anchorMin = new Vector2(0f, 0f);
        bodyRoot.anchorMax = new Vector2(1f, 1f);
        bodyRoot.offsetMin = new Vector2(14f, 12f);
        bodyRoot.offsetMax = new Vector2(-14f, -12f);

        var layout = bodyObject.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        layout.spacing = 12f;
    }

    void EnsureSkillRowRoot()
    {
        if (skillRowRoot != null)
            return;

        var rowObject = new GameObject(
            "SkillRow",
            typeof(RectTransform),
            typeof(LayoutElement),
            typeof(HorizontalLayoutGroup));
        rowObject.layer = LayerMask.NameToLayer("UI");
        skillRowRoot = rowObject.GetComponent<RectTransform>();
        skillRowRoot.SetParent(bodyRoot, false);

        var rowLayoutElement = rowObject.GetComponent<LayoutElement>();
        rowLayoutElement.flexibleWidth = 1f;
        rowLayoutElement.preferredWidth = 1000f;
        rowLayoutElement.minWidth = 400f;

        var rowLayout = rowObject.GetComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;
        rowLayout.spacing = 10f;
    }

    void EnsureCommitButton()
    {
        if (commitButton != null)
            return;

        var buttonObject = new GameObject(
            "CommitButton",
            typeof(RectTransform),
            typeof(LayoutElement),
            typeof(Image),
            typeof(Button));
        buttonObject.layer = LayerMask.NameToLayer("UI");
        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(bodyRoot, false);

        var layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = 320f;
        layoutElement.minWidth = 280f;

        commitButtonImage = buttonObject.GetComponent<Image>();
        commitButtonImage.color = commitReadyColor;

        commitButton = buttonObject.GetComponent<Button>();
        commitButton.targetGraphic = commitButtonImage;
        commitButton.onClick.AddListener(OnCommitButtonPressed);

        commitButtonText = CreateLabel(
            "CommitText",
            buttonRect,
            24f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            labelColor);
        commitButtonText.rectTransform.anchorMin = Vector2.zero;
        commitButtonText.rectTransform.anchorMax = Vector2.one;
        commitButtonText.rectTransform.offsetMin = Vector2.zero;
        commitButtonText.rectTransform.offsetMax = Vector2.zero;
    }

    void EnsureSkillSlotCount(int desiredCount)
    {
        if (skillRowRoot == null)
            return;
        if (desiredCount == skillSlots.Count)
            return;

        ClearSkillSlots();

        for (int index = 0; index < desiredCount; index++)
            skillSlots.Add(CreateSkillSlot(index));
    }

    void ClearSkillSlots()
    {
        for (int index = 0; index < skillSlots.Count; index++)
        {
            var slot = skillSlots[index];
            if (slot?.root == null)
                continue;

            if (Application.isPlaying)
                Destroy(slot.root.gameObject);
            else
                DestroyImmediate(slot.root.gameObject);
        }

        skillSlots.Clear();
    }

    SkillSlotWidgets CreateSkillSlot(int slotIndex)
    {
        var slotObject = new GameObject(
            $"SkillSlot_{slotIndex + 1}",
            typeof(RectTransform),
            typeof(LayoutElement),
            typeof(Image),
            typeof(Button));
        slotObject.layer = LayerMask.NameToLayer("UI");

        var slotRect = slotObject.GetComponent<RectTransform>();
        slotRect.SetParent(skillRowRoot, false);

        var layoutElement = slotObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = 228f;
        layoutElement.minWidth = 180f;

        var background = slotObject.GetComponent<Image>();
        background.color = slotCooldownColor;

        var button = slotObject.GetComponent<Button>();
        button.targetGraphic = background;
        int capturedIndex = slotIndex;
        button.onClick.AddListener(() => OnSkillSlotPressed(capturedIndex));

        var hotkeyText = CreateLabel(
            "HotkeyText",
            slotRect,
            19f,
            FontStyles.Bold,
            TextAlignmentOptions.TopLeft,
            subtleLabelColor);
        hotkeyText.rectTransform.anchorMin = new Vector2(0f, 1f);
        hotkeyText.rectTransform.anchorMax = new Vector2(0f, 1f);
        hotkeyText.rectTransform.pivot = new Vector2(0f, 1f);
        hotkeyText.rectTransform.anchoredPosition = new Vector2(10f, -8f);
        hotkeyText.rectTransform.sizeDelta = new Vector2(54f, 24f);

        var nameText = CreateLabel(
            "NameText",
            slotRect,
            20f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            labelColor);
        nameText.rectTransform.anchorMin = new Vector2(0f, 0f);
        nameText.rectTransform.anchorMax = new Vector2(1f, 1f);
        nameText.rectTransform.offsetMin = new Vector2(10f, 34f);
        nameText.rectTransform.offsetMax = new Vector2(-10f, -30f);
        nameText.textWrappingMode = TextWrappingModes.Normal;
        nameText.overflowMode = TextOverflowModes.Ellipsis;

        var statusText = CreateLabel(
            "StatusText",
            slotRect,
            18f,
            FontStyles.Normal,
            TextAlignmentOptions.Bottom,
            subtleLabelColor);
        statusText.rectTransform.anchorMin = new Vector2(0f, 0f);
        statusText.rectTransform.anchorMax = new Vector2(1f, 0f);
        statusText.rectTransform.pivot = new Vector2(0.5f, 0f);
        statusText.rectTransform.anchoredPosition = new Vector2(0f, 8f);
        statusText.rectTransform.sizeDelta = new Vector2(-12f, 24f);

        return new SkillSlotWidgets
        {
            root = slotRect,
            button = button,
            background = background,
            hotkeyText = hotkeyText,
            nameText = nameText,
            statusText = statusText
        };
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

    static TextMeshProUGUI CreateLabel(
        string name,
        Transform parent,
        float fontSize,
        FontStyles style,
        TextAlignmentOptions alignment,
        Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.layer = LayerMask.NameToLayer("UI");
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.text = string.Empty;
        return text;
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

