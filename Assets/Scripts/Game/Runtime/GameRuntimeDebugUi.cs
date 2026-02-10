using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GameRuntimeDebugUi : MonoBehaviour
{
    GameManager gameManager;
    GameTurnRuntime runtime;

    readonly Dictionary<string, int> advisorCooldownById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, int> advisorUseCountBySituationKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    readonly List<string> decreeInventory = new List<string>();

    bool loadoutInitialized;
    int observedTurnNumber;
    int selectedDieIndex = -1;
    int? selectedSituationInstanceId;
    Vector2 leftScroll;
    Vector2 rightScroll;

    void Update()
    {
        EnsureRuntimeReferences();
        if (runtime == null)
            return;

        SanitizeSelections();
        SyncTurnTick();
        HandleHotkeys();
    }

    void OnGUI()
    {
        EnsureRuntimeReferences();
        if (runtime == null || gameManager == null || gameManager.staticDataCatalog == null)
            return;

        var state = runtime.state;
        var leftRect = new Rect(12f, 12f, Mathf.Min(680f, Screen.width * 0.62f), Screen.height - 24f);
        var rightRect = new Rect(leftRect.xMax + 10f, 12f, Screen.width - leftRect.xMax - 22f, Screen.height - 24f);

        GUILayout.BeginArea(leftRect, GUI.skin.box);
        leftScroll = GUILayout.BeginScrollView(leftScroll);

        DrawTopHud(state);
        GUILayout.Space(8f);
        DrawDicePanel(state);
        GUILayout.Space(8f);
        DrawSituationsPanel(state);
        GUILayout.Space(8f);
        DrawAssignmentControls(state);

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        GUILayout.BeginArea(rightRect, GUI.skin.box);
        rightScroll = GUILayout.BeginScrollView(rightScroll);

        DrawAdjustmentPanel(state);
        GUILayout.Space(8f);
        DrawPhaseControls(state);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void EnsureRuntimeReferences()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;
        if (gameManager == null)
            return;

        if (runtime != gameManager.turnRuntime)
        {
            runtime = gameManager.turnRuntime;
            loadoutInitialized = false;
            observedTurnNumber = runtime != null ? runtime.state.turnNumber : 0;
        }

        if (!loadoutInitialized && runtime != null && gameManager.startingLoadout != null)
            InitializeFromLoadout(gameManager.startingLoadout);
    }

    void InitializeFromLoadout(GameStartingLoadout loadout)
    {
        advisorCooldownById.Clear();
        advisorUseCountBySituationKey.Clear();
        decreeInventory.Clear();

        for (int i = 0; i < loadout.advisorIds.Count; i++)
        {
            var advisorId = loadout.advisorIds[i] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(advisorId))
                continue;

            if (!advisorCooldownById.ContainsKey(advisorId))
                advisorCooldownById.Add(advisorId, 0);
        }

        for (int i = 0; i < loadout.decreeIds.Count; i++)
        {
            var decreeId = loadout.decreeIds[i] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(decreeId))
                continue;

            decreeInventory.Add(decreeId);
        }

        loadoutInitialized = true;
        observedTurnNumber = runtime.state.turnNumber;
    }

    void SyncTurnTick()
    {
        if (runtime == null)
            return;

        var turnNumber = runtime.state.turnNumber;
        if (turnNumber == observedTurnNumber)
            return;

        if (turnNumber < observedTurnNumber)
        {
            if (gameManager != null && gameManager.startingLoadout != null)
                InitializeFromLoadout(gameManager.startingLoadout);
            else
                observedTurnNumber = turnNumber;

            selectedDieIndex = -1;
            selectedSituationInstanceId = null;
            leftScroll = Vector2.zero;
            rightScroll = Vector2.zero;
            return;
        }

        var delta = turnNumber - observedTurnNumber;
        observedTurnNumber = turnNumber;

        for (int step = 0; step < delta; step++)
            TickAdvisorCooldowns();
    }

    void TickAdvisorCooldowns()
    {
        var keys = advisorCooldownById.Keys.ToList();
        for (int i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            advisorCooldownById[key] = Mathf.Max(0, advisorCooldownById[key] - 1);
        }
    }

    void HandleHotkeys()
    {
        if (runtime == null)
            return;

        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.spaceKey.wasPressedThisFrame)
            runtime.TryAdvancePhase();
        if (keyboard.rKey.wasPressedThisFrame && gameManager != null)
            gameManager.TryRestartRun();

        for (int i = 0; i < 10; i++)
        {
            var pressed = i switch
            {
                0 => keyboard.digit1Key.wasPressedThisFrame,
                1 => keyboard.digit2Key.wasPressedThisFrame,
                2 => keyboard.digit3Key.wasPressedThisFrame,
                3 => keyboard.digit4Key.wasPressedThisFrame,
                4 => keyboard.digit5Key.wasPressedThisFrame,
                5 => keyboard.digit6Key.wasPressedThisFrame,
                6 => keyboard.digit7Key.wasPressedThisFrame,
                7 => keyboard.digit8Key.wasPressedThisFrame,
                8 => keyboard.digit9Key.wasPressedThisFrame,
                9 => keyboard.digit0Key.wasPressedThisFrame,
                _ => false
            };

            if (!pressed)
                continue;

            if (i >= runtime.state.dice.Count)
                continue;

            selectedDieIndex = i;
        }
    }

    void SanitizeSelections()
    {
        if (runtime == null)
            return;

        if (selectedDieIndex >= runtime.state.dice.Count)
            selectedDieIndex = runtime.state.dice.Count - 1;

        if (selectedSituationInstanceId.HasValue)
        {
            var exists = runtime.state.activeSituations.Any(item => item.situationInstanceId == selectedSituationInstanceId.Value);
            if (!exists)
                selectedSituationInstanceId = null;
        }
    }

    void DrawTopHud(GameRunRuntimeState state)
    {
        GUILayout.Label($"Turn {state.turnNumber} | Phase: {state.phase}");
        GUILayout.Label($"DEF {state.defense} | STB {state.stability} | Gold {state.gold} | Situations {state.activeSituations.Count}");

        if (state.resourceGuardUntilTurnByResource.TryGetValue("defense", out var defenseGuardTurn) && state.turnNumber <= defenseGuardTurn)
            GUILayout.Label($"Guard DEF until turn {defenseGuardTurn}");
        if (state.resourceGuardUntilTurnByResource.TryGetValue("stability", out var stabilityGuardTurn) && state.turnNumber <= stabilityGuardTurn)
            GUILayout.Label($"Guard STB until turn {stabilityGuardTurn}");

        if (state.isGameOver)
            GUILayout.Label($"GAME OVER: {state.gameOverReason}");
    }

    void DrawDicePanel(GameRunRuntimeState state)
    {
        GUILayout.Label("Dice");
        for (int i = 0; i < state.dice.Count; i++)
        {
            var die = state.dice[i];
            var isSelected = i == selectedDieIndex;
            var assigned = die.assignedSituationInstanceId.HasValue ? $"S#{die.assignedSituationInstanceId.Value}" : "None";
            var faceText = die.hasRolled ? $"rolled {die.rolledFace} / now {die.currentFace}" : "not rolled";
            var upgrade = string.IsNullOrWhiteSpace(die.upgradeId) ? "-" : die.upgradeId;
            var label = $"[{i + 1}] {faceText} | assign {assigned} | upg {upgrade}";
            if (GUILayout.Button((isSelected ? "> " : "") + label))
                selectedDieIndex = i;
        }
    }

    void DrawSituationsPanel(GameRunRuntimeState state)
    {
        GUILayout.Label("Situations");

        if (state.activeSituations.Count == 0)
        {
            GUILayout.Label("(empty)");
            return;
        }

        for (int i = 0; i < state.activeSituations.Count; i++)
        {
            var situation = state.activeSituations[i];
            var isSelected = selectedSituationInstanceId.HasValue && selectedSituationInstanceId.Value == situation.situationInstanceId;
            var assignedDice = GetAssignedDiceForSituation(state, situation.situationInstanceId);
            var assignedSum = 0;
            for (int dieIndex = 0; dieIndex < assignedDice.Count; dieIndex++)
                assignedSum += Mathf.Max(0, assignedDice[dieIndex].currentFace);

            GUILayout.BeginVertical(GUI.skin.box);
            if (GUILayout.Button((isSelected ? "> " : "") + $"#{situation.boardOrder} {situation.situationId} (inst {situation.situationInstanceId})"))
                selectedSituationInstanceId = situation.situationInstanceId;

            GUILayout.Label($"Demand {situation.demand} | Deadline {situation.deadline} | Assigned {assignedDice.Count} dice / {assignedSum}");

            if (gameManager.staticDataCatalog.TryGetSituation(situation.situationId, out var definition))
            {
                DrawEffectList("Success", definition.onSuccess);
                DrawEffectList("Fail", definition.onFail);
            }

            GUILayout.EndVertical();
        }
    }

    void DrawEffectList(string title, IReadOnlyList<GameEffectDefinition> effects)
    {
        GUILayout.Label(title + " Effects:");
        if (effects == null || effects.Count == 0)
        {
            GUILayout.Label("- none");
            return;
        }

        for (int i = 0; i < effects.Count; i++)
            GUILayout.Label("- " + FormatEffect(effects[i]));
    }

    string FormatEffect(GameEffectDefinition effect)
    {
        if (effect == null)
            return "(null)";

        var icon = effect.targetResource switch
        {
            "defense" => "[DEF] ",
            "stability" => "[STB] ",
            _ => string.Empty
        };

        return effect.effectType switch
        {
            "resource_delta" => $"{icon}{FormatSigned(effect.value)}",
            "demand_delta" => $"Demand {FormatSigned(effect.value)} ({effect.targetMode})",
            "deadline_delta" => $"Deadline {FormatSigned(effect.value)} ({effect.targetMode})",
            "resource_guard" => $"{icon}Guard {effect.duration} turn",
            "die_face_delta" => $"Die {FormatSigned(effect.value)} ({effect.targetMode})",
            "die_face_set" => $"Die set {Mathf.RoundToInt(effect.value ?? 0f)} ({effect.targetMode})",
            "die_face_min" => $"Die min {Mathf.RoundToInt(effect.value ?? 0f)} ({effect.targetMode})",
            "die_face_mult" => $"Die x{effect.value:0.##} ({effect.targetMode})",
            "reroll_assigned_dice" => $"Reroll assigned dice ({effect.targetMode})",
            _ => effect.effectType
        };
    }

    string FormatSigned(float? value)
    {
        if (!value.HasValue)
            return "0";
        var intValue = Mathf.RoundToInt(value.Value);
        return intValue >= 0 ? $"+{intValue}" : intValue.ToString();
    }

    void DrawAssignmentControls(GameRunRuntimeState state)
    {
        GUILayout.Label("Assignment Controls");

        if (state.phase != GameTurnPhase.Assignment)
        {
            GUILayout.Label("Assignment available in Assignment phase.");
            return;
        }

        if (selectedDieIndex < 0 || selectedDieIndex >= state.dice.Count)
            GUILayout.Label("Select a die first.");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Assign selected die -> selected situation"))
        {
            if (selectedDieIndex >= 0 && selectedSituationInstanceId.HasValue)
                runtime.TryAssignDieToSituation(selectedDieIndex, selectedSituationInstanceId.Value);
        }

        if (GUILayout.Button("Unassign selected die"))
        {
            if (selectedDieIndex >= 0)
                runtime.TryAssignDieToSituation(selectedDieIndex, null);
        }
        GUILayout.EndHorizontal();
    }

    void DrawAdjustmentPanel(GameRunRuntimeState state)
    {
        GUILayout.Label("Adjustment");
        GUILayout.Label($"Selected Die: {(selectedDieIndex >= 0 ? (selectedDieIndex + 1).ToString() : "none")}, Selected Situation: {(selectedSituationInstanceId.HasValue ? selectedSituationInstanceId.Value.ToString() : "none")}");

        if (state.phase != GameTurnPhase.Adjustment)
            GUILayout.Label("Adjustment actions are intended for Adjustment phase.");

        DrawAdvisorButtons(state);
        GUILayout.Space(6f);
        DrawDecreeButtons(state);
    }

    void DrawAdvisorButtons(GameRunRuntimeState state)
    {
        GUILayout.Label("Advisors");
        if (advisorCooldownById.Count == 0)
        {
            GUILayout.Label("- none");
            return;
        }

        var ids = advisorCooldownById.Keys.OrderBy(id => id, StringComparer.OrdinalIgnoreCase).ToList();
        for (int i = 0; i < ids.Count; i++)
        {
            var advisorId = ids[i];
            var cooldown = advisorCooldownById[advisorId];
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label($"{advisorId} (cd {cooldown})", GUILayout.Width(220f));
                GUI.enabled = !state.isGameOver && cooldown <= 0;
                if (GUILayout.Button("Use"))
                {
                    if (TryUseAdvisor(advisorId, out var consumed))
                    {
                        if (consumed && gameManager.staticDataCatalog.TryGetAdvisor(advisorId, out var definition))
                            advisorCooldownById[advisorId] = Mathf.Max(0, definition.cooldown);
                    }
                }
                GUI.enabled = true;
            }
        }
    }

    void DrawDecreeButtons(GameRunRuntimeState state)
    {
        GUILayout.Label("Decrees");
        if (decreeInventory.Count == 0)
        {
            GUILayout.Label("- none");
            return;
        }

        for (int i = decreeInventory.Count - 1; i >= 0; i--)
        {
            var decreeId = decreeInventory[i];
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label(decreeId, GUILayout.Width(220f));
                GUI.enabled = !state.isGameOver;
                if (GUILayout.Button("Use"))
                {
                    if (TryUseDecree(decreeId, out var consumed) && consumed)
                        decreeInventory.RemoveAt(i);
                }
                GUI.enabled = true;
            }
        }
    }

    bool TryUseAdvisor(string advisorId, out bool consumed)
    {
        consumed = false;
        if (!gameManager.staticDataCatalog.TryGetAdvisor(advisorId, out var definition))
            return false;

        if (!ResolveTargets(definition.targetType, out var selectedSituationId, out var selectedDieId))
            return false;

        if (definition.maxUsesPerSituation.HasValue && selectedSituationId.HasValue)
        {
            var key = BuildSituationUseKey(advisorId, selectedSituationId.Value);
            if (advisorUseCountBySituationKey.TryGetValue(key, out var used) && used >= definition.maxUsesPerSituation.Value)
                return false;
        }

        if (!ValidateConditions(definition.conditions, selectedDieId))
            return false;

        var applied = runtime.TryApplyDirectEffects(definition.effects, null, selectedSituationId, selectedDieId);
        if (!applied)
            return false;

        if (definition.maxUsesPerSituation.HasValue && selectedSituationId.HasValue)
        {
            var key = BuildSituationUseKey(advisorId, selectedSituationId.Value);
            advisorUseCountBySituationKey.TryGetValue(key, out var used);
            advisorUseCountBySituationKey[key] = used + 1;
        }

        consumed = true;
        return true;
    }

    bool TryUseDecree(string decreeId, out bool consumed)
    {
        consumed = false;
        if (!gameManager.staticDataCatalog.TryGetDecree(decreeId, out var definition))
            return false;

        if (!ResolveTargets(definition.targetType, out var selectedSituationId, out var selectedDieId))
            return false;
        if (!ValidateConditions(definition.conditions, selectedDieId))
            return false;

        var applied = runtime.TryApplyDirectEffects(definition.effects, null, selectedSituationId, selectedDieId);
        if (!applied)
            return false;

        consumed = true;
        return true;
    }

    bool ResolveTargets(string targetType, out int? selectedSituationId, out int? selectedDieId)
    {
        selectedSituationId = null;
        selectedDieId = null;

        if (string.IsNullOrWhiteSpace(targetType) || targetType.Equals("self", StringComparison.OrdinalIgnoreCase))
            return true;

        if (targetType.Equals("selected_situation", StringComparison.OrdinalIgnoreCase))
        {
            if (!selectedSituationInstanceId.HasValue)
                return false;

            var exists = runtime.state.activeSituations.Any(item => item.situationInstanceId == selectedSituationInstanceId.Value);
            if (!exists)
                return false;

            selectedSituationId = selectedSituationInstanceId.Value;
            return true;
        }

        if (targetType.Equals("selected_die", StringComparison.OrdinalIgnoreCase))
        {
            if (selectedDieIndex < 0 || selectedDieIndex >= runtime.state.dice.Count)
                return false;
            if (!runtime.state.dice[selectedDieIndex].hasRolled)
                return false;

            selectedDieId = selectedDieIndex;
            return true;
        }

        return false;
    }

    bool ValidateConditions(IReadOnlyList<GameConditionDefinition> conditions, int? selectedDieId)
    {
        if (conditions == null || conditions.Count == 0)
            return true;

        for (int i = 0; i < conditions.Count; i++)
        {
            var condition = conditions[i];
            if (condition == null)
                continue;
            if (string.IsNullOrWhiteSpace(condition.conditionType))
                continue;

            switch (condition.conditionType)
            {
                case "selected_die_face_eq":
                {
                    if (!selectedDieId.HasValue)
                        return false;
                    var target = Mathf.RoundToInt(condition.value ?? 0f);
                    var die = runtime.state.dice[selectedDieId.Value];
                    if (die.currentFace != target)
                        return false;
                    break;
                }
                default:
                    break;
            }
        }

        return true;
    }

    string BuildSituationUseKey(string advisorId, int situationInstanceId)
    {
        return advisorId + "#" + situationInstanceId;
    }

    List<GameDieRuntimeState> GetAssignedDiceForSituation(GameRunRuntimeState state, int situationInstanceId)
    {
        var list = new List<GameDieRuntimeState>();
        for (int i = 0; i < state.dice.Count; i++)
        {
            var die = state.dice[i];
            if (!die.assignedSituationInstanceId.HasValue)
                continue;
            if (die.assignedSituationInstanceId.Value != situationInstanceId)
                continue;
            list.Add(die);
        }

        return list;
    }

    void DrawPhaseControls(GameRunRuntimeState state)
    {
        GUILayout.Label("Phase Controls");
        GUILayout.Label("SPACE: advance phase, R: restart run, Number keys 1..0: select die");

        GUI.enabled = !state.isGameOver;
        if (GUILayout.Button("Advance Phase"))
            runtime.TryAdvancePhase();
        GUI.enabled = true;

        if (GUILayout.Button("Restart Run"))
        {
            if (gameManager != null)
                gameManager.TryRestartRun();
        }

        GUILayout.Label($"Last resolution: demandApplied={state.lastResolutionAppliedDemandTotal}, success={state.lastResolutionSuccessCount}, fail={state.lastResolutionFailCount}");
    }
}
