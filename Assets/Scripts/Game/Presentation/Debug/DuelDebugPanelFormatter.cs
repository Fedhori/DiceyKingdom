using Game.Application.Duel;
using Game.Domain.Duel;
using Game.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Presentation.Debug
{
    public static class DuelDebugPanelFormatter
    {
        public static string FormatPhase(DuelPhaseRunner phaseRunner)
        {
            string phaseLabel = phaseRunner == null ? "(none)" : phaseRunner.currentPhase.ToString();
            return $"Phase: {phaseLabel}";
        }

        public static string FormatTurn(DuelState duelState)
        {
            int turn = duelState == null ? 0 : duelState.turnIndex;
            return $"Turn: {turn}";
        }

        public static string FormatFocus(DuelState duelState)
        {
            int focus = duelState == null ? 0 : duelState.focus;
            return $"Focus: {focus}";
        }

        public static string FormatHonor(DuelState duelState)
        {
            int honor = duelState == null ? 0 : duelState.honor;
            return $"Honor: {honor}";
        }

        public static string FormatPlayerHealth(DuelState duelState)
        {
            int playerHealth = duelState == null ? 0 : duelState.playerHealth;
            return $"Player Health: {playerHealth}";
        }

        public static string FormatOpponentHealth(DuelState duelState)
        {
            int opponentHealth = duelState == null ? 0 : duelState.opponentHealth;
            return $"Opponent Health: {opponentHealth}";
        }

        public static string FormatClash(
            DuelState duelState,
            int clashIndex,
            GameDatabase database = null)
        {
            if (duelState == null ||
                duelState.clashes == null ||
                clashIndex < 0 ||
                clashIndex >= duelState.clashes.Count)
            {
                return $"Clash {clashIndex}: (missing)";
            }

            ClashState clash = duelState.clashes[clashIndex];
            if (clash == null)
            {
                return $"Clash {clashIndex}: (missing)";
            }

            int playerCount = clash.playerActionIds == null ? 0 : clash.playerActionIds.Count;
            int opponentCount = clash.opponentActionIds == null ? 0 : clash.opponentActionIds.Count;

            int playerTotalAttack = DuelSimulator.ComputeTotalAttack(
                clash,
                duelState.actionsById,
                true);

            int opponentTotalAttack = DuelSimulator.ComputeTotalAttack(
                clash,
                duelState.actionsById,
                false);

            string clashId = string.IsNullOrWhiteSpace(clash.clashId)
                ? "(no-id)"
                : clash.clashId;

            string slotLabel = clash.slotLimit.HasValue
                ? clash.slotLimit.Value.ToString()
                : "unlimited";

            string damageLabel = FormatClashDamageLabel(database, clashId);
            if (string.IsNullOrWhiteSpace(damageLabel))
            {
                return
                    $"Clash {clashIndex} ({clashId}) | TotalAttack P:{playerTotalAttack} E:{opponentTotalAttack} | Actions P:{playerCount} E:{opponentCount} | Slot:{slotLabel}";
            }

            return
                $"Clash {clashIndex} ({clashId}) | TotalAttack P:{playerTotalAttack} E:{opponentTotalAttack} | Actions P:{playerCount} E:{opponentCount} | Slot:{slotLabel} | {damageLabel}";
        }

        public static string FormatActionHolderActions(DuelState duelState, string selectedActionId)
        {
            string selectedLine = FormatSelectedAction(duelState, selectedActionId);

            if (duelState == null ||
                duelState.actionHolderActionIds == null ||
                duelState.actionHolderActionIds.Count <= 0)
            {
                return $"{selectedLine}\nActionHolder Actions: none";
            }

            var lines = new List<string>
            {
                selectedLine,
                $"ActionHolder Actions ({duelState.actionHolderActionIds.Count}):"
            };

            for (int i = 0; i < duelState.actionHolderActionIds.Count; i++)
            {
                string actionId = duelState.actionHolderActionIds[i];
                if (!TryClashResolveAction(duelState, actionId, out ActionInstance action))
                {
                    lines.Add("- (missing action)");
                    continue;
                }

                string actionDefId = ClashResolveActionDefId(action, null);
                bool isSelected = string.Equals(actionId, selectedActionId, StringComparison.Ordinal);
                string selectedSuffix = isSelected ? " <selected>" : string.Empty;
                string cooldownLabel = action.cooldownTurns > 0
                    ? $"{action.cooldownRemaining}/{action.cooldownTurns}"
                    : "-";
                lines.Add(
                    $"- {actionDefId} | Type:{action.abilityType} | Damage:{action.attack} | Attack Result:{action.attackResult} | CD:{cooldownLabel}{selectedSuffix}");
            }

            return string.Join("\n", lines);
        }

        public static string FormatActionEffects(GameDatabase database, string actionDefId)
        {
            ActionDef actionDef = ClashResolveActionDef(database, actionDefId);
            return ClashResolveEffectsLabel(actionDef);
        }

        public static string FormatSelectedAction(DuelState duelState, string selectedActionId)
        {
            if (string.IsNullOrWhiteSpace(selectedActionId))
            {
                return "Selected Action: (none)";
            }

            if (duelState == null ||
                duelState.actionsById == null ||
                !duelState.actionsById.TryGetValue(selectedActionId, out ActionInstance action) ||
                action == null)
            {
                return "Selected Action: (missing)";
            }

            string location = ClashResolveActionLocation(duelState, selectedActionId);
            string actionDefId = ClashResolveActionDefId(action, null);
            string cooldownLabel = action.cooldownTurns > 0
                ? $"{action.cooldownRemaining}/{action.cooldownTurns}"
                : "-";
            return $"Selected Action: {actionDefId} | Type:{action.abilityType} | Damage:{action.attack} | Attack Result:{action.attackResult} | CD:{cooldownLabel} | {location}";
        }

        public static string FormatSelectedClash(DuelState duelState, int selectedClashIndex)
        {
            if (duelState == null ||
                duelState.clashes == null ||
                selectedClashIndex < 0 ||
                selectedClashIndex >= duelState.clashes.Count)
            {
                return "Selected Clash: (none)";
            }

            ClashState clash = duelState.clashes[selectedClashIndex];
            if (clash == null)
            {
                return $"Selected Clash: {selectedClashIndex} (missing)";
            }

            int playerCount = clash.playerActionIds == null ? 0 : clash.playerActionIds.Count;
            int opponentCount = clash.opponentActionIds == null ? 0 : clash.opponentActionIds.Count;
            string clashId = string.IsNullOrWhiteSpace(clash.clashId)
                ? "(no-id)"
                : clash.clashId;

            return
                $"Selected Clash: {selectedClashIndex} ({clashId}) | Actions P:{playerCount} E:{opponentCount}";
        }

        static string ClashResolveActionLocation(DuelState duelState, string actionId)
        {
            if (duelState.actionHolderActionIds != null && duelState.actionHolderActionIds.Contains(actionId))
            {
                return "actionHolder";
            }

            if (duelState.clashes == null)
            {
                return "unknown";
            }

            for (int i = 0; i < duelState.clashes.Count; i++)
            {
                ClashState field = duelState.clashes[i];
                if (field == null)
                {
                    continue;
                }

                if (field.playerActionIds != null && field.playerActionIds.Contains(actionId))
                {
                    return $"player@{i}";
                }

                if (field.opponentActionIds != null && field.opponentActionIds.Contains(actionId))
                {
                    return $"opponent@{i}";
                }
            }

            return "unknown";
        }

        static string FormatClashDamageLabel(GameDatabase database, string clashId)
        {
            if (database == null ||
                database.clashesById == null ||
                string.IsNullOrWhiteSpace(clashId))
            {
                return string.Empty;
            }

            if (!database.clashesById.TryGetValue(clashId, out ClashDef clashDef) ||
                clashDef == null)
            {
                return string.Empty;
            }

            return $"Damage:{clashDef.damage}";
        }

        static bool TryClashResolveAction(DuelState duelState, string actionId, out ActionInstance action)
        {
            action = null;
            if (duelState == null || duelState.actionsById == null || string.IsNullOrWhiteSpace(actionId))
            {
                return false;
            }

            if (!duelState.actionsById.TryGetValue(actionId, out action) || action == null)
            {
                return false;
            }

            return true;
        }

        static ActionDef ClashResolveActionDef(GameDatabase database, string actionDefId)
        {
            if (database == null ||
                database.actionsById == null ||
                string.IsNullOrWhiteSpace(actionDefId))
            {
                return null;
            }

            if (!database.actionsById.TryGetValue(actionDefId, out ActionDef actionDef) || actionDef == null)
            {
                return null;
            }

            return actionDef;
        }

        static string ClashResolveActionDefId(ActionInstance action, ActionDef actionDef)
        {
            if (action != null && !string.IsNullOrWhiteSpace(action.actionDefId))
            {
                return action.actionDefId;
            }

            if (actionDef != null && !string.IsNullOrWhiteSpace(actionDef.id))
            {
                return actionDef.id;
            }

            return "(no-def)";
        }

        static string ClashResolveEffectsLabel(ActionDef actionDef)
        {
            if (actionDef == null || actionDef.effects == null || actionDef.effects.Count <= 0)
            {
                return "none";
            }

            var effectLabels = new List<string>();

            for (int i = 0; i < actionDef.effects.Count; i++)
            {
                TimedEffectDef timedEffect = actionDef.effects[i];
                if (timedEffect == null)
                {
                    continue;
                }

                string timingLabel = ToDisplayLabel(timedEffect.timing);
                if (string.IsNullOrWhiteSpace(timingLabel))
                {
                    timingLabel = "Timing";
                }

                if (timedEffect.ops == null || timedEffect.ops.Count <= 0)
                {
                    effectLabels.Add(timingLabel);
                    continue;
                }

                var opLabels = new List<string>();
                for (int opIndex = 0; opIndex < timedEffect.ops.Count; opIndex++)
                {
                    EffectOpDef op = timedEffect.ops[opIndex];
                    if (op == null)
                    {
                        continue;
                    }

                    string opLabel = ToDisplayLabel(op.op);
                    if (string.IsNullOrWhiteSpace(opLabel))
                    {
                        opLabel = ToDisplayLabel(op.textLocKey);
                    }

                    if (string.IsNullOrWhiteSpace(opLabel))
                    {
                        continue;
                    }

                    if (op.TryGetAmount(out int amount))
                    {
                        string signedAmount = amount >= 0 ? $"+{amount}" : amount.ToString();
                        opLabel = $"{opLabel}({signedAmount})";
                    }

                    opLabels.Add(opLabel);
                }

                if (opLabels.Count <= 0)
                {
                    effectLabels.Add(timingLabel);
                    continue;
                }

                effectLabels.Add($"{timingLabel}: {string.Join(", ", opLabels)}");
            }

            if (effectLabels.Count <= 0)
            {
                return "none";
            }

            return string.Join(" | ", effectLabels);
        }

        static string ToDisplayLabel(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            string normalized = raw.Trim().Replace("-", "_").Replace(" ", "_");
            string[] tokens = normalized.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length <= 0)
            {
                return raw.Trim();
            }

            var words = new List<string>();
            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];
                if (string.Equals(token, "action", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(token, "name", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(token, "desc", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(token, "loc", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(token, "key", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string word = ToTitleWord(token);
                if (string.IsNullOrWhiteSpace(word))
                {
                    continue;
                }

                words.Add(word);
            }

            if (words.Count <= 0)
            {
                return raw.Trim();
            }

            return string.Join(" ", words);
        }

        static string ToTitleWord(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(token.Length + 4);
            builder.Append(char.ToUpperInvariant(token[0]));

            for (int i = 1; i < token.Length; i++)
            {
                char previous = token[i - 1];
                char current = token[i];

                bool shouldInsertSpace =
                    (char.IsUpper(current) && (char.IsLower(previous) || char.IsDigit(previous))) ||
                    (char.IsDigit(current) && !char.IsDigit(previous)) ||
                    (char.IsLetter(current) && char.IsDigit(previous));

                if (shouldInsertSpace)
                {
                    builder.Append(' ');
                }

                if (char.IsLetter(current))
                {
                    builder.Append(char.IsUpper(current) ? current : char.ToLowerInvariant(current));
                }
                else
                {
                    builder.Append(current);
                }
            }

            return builder.ToString();
        }
    }
}
