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

        public static string FormatResourceStatus()
        {
            return "Resource: none";
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

            int playerCount = clash.playerAbilityIds == null ? 0 : clash.playerAbilityIds.Count;
            int opponentCount = clash.opponentAbilityIds == null ? 0 : clash.opponentAbilityIds.Count;

            int playerTotalAttack = DuelSimulator.ComputeTotalAttack(
                clash,
                duelState.abilitiesById,
                true);

            int opponentTotalAttack = DuelSimulator.ComputeTotalAttack(
                clash,
                duelState.abilitiesById,
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
                    $"Clash {clashIndex} ({clashId}) | TotalAttack P:{playerTotalAttack} E:{opponentTotalAttack} | Abilities P:{playerCount} E:{opponentCount} | Slot:{slotLabel}";
            }

            return
                $"Clash {clashIndex} ({clashId}) | TotalAttack P:{playerTotalAttack} E:{opponentTotalAttack} | Abilities P:{playerCount} E:{opponentCount} | Slot:{slotLabel} | {damageLabel}";
        }

        public static string FormatBagAbilities(DuelState duelState, string selectedAbilityId)
        {
            string selectedLine = FormatSelectedAbility(duelState, selectedAbilityId);

            if (duelState == null ||
                duelState.bagAbilityIds == null ||
                duelState.bagAbilityIds.Count <= 0)
            {
                return $"{selectedLine}\nBag Abilities: none";
            }

            var lines = new List<string>
            {
                selectedLine,
                $"Bag Abilities ({duelState.bagAbilityIds.Count}):"
            };

            for (int i = 0; i < duelState.bagAbilityIds.Count; i++)
            {
                string abilityId = duelState.bagAbilityIds[i];
                if (!TryResolveAbility(duelState, abilityId, out AbilityInstance ability))
                {
                    lines.Add("- (missing ability)");
                    continue;
                }

                string abilityDefId = ResolveAbilityDefId(ability, null);
                bool isSelected = string.Equals(abilityId, selectedAbilityId, StringComparison.Ordinal);
                string selectedSuffix = isSelected ? " <selected>" : string.Empty;
                string cooldownLabel = ability.cooldownTurns > 0
                    ? $"{ability.cooldownRemaining}/{ability.cooldownTurns}"
                    : "-";
                lines.Add(
                    $"- {abilityDefId} | Type:{ability.abilityType} | Damage:{ability.attack} | Attack Result:{ability.attackResult} | CD:{cooldownLabel}{selectedSuffix}");
            }

            return string.Join("\n", lines);
        }

        public static string FormatAbilityEffects(GameDatabase database, string abilityDefId)
        {
            AbilityDef abilityDef = ResolveAbilityDef(database, abilityDefId);
            return ResolveEffectsLabel(abilityDef);
        }

        public static string FormatSelectedAbility(DuelState duelState, string selectedAbilityId)
        {
            if (string.IsNullOrWhiteSpace(selectedAbilityId))
            {
                return "Selected Ability: (none)";
            }

            if (duelState == null ||
                duelState.abilitiesById == null ||
                !duelState.abilitiesById.TryGetValue(selectedAbilityId, out AbilityInstance ability) ||
                ability == null)
            {
                return "Selected Ability: (missing)";
            }

            string location = ResolveAbilityLocation(duelState, selectedAbilityId);
            string abilityDefId = ResolveAbilityDefId(ability, null);
            string cooldownLabel = ability.cooldownTurns > 0
                ? $"{ability.cooldownRemaining}/{ability.cooldownTurns}"
                : "-";
            return $"Selected Ability: {abilityDefId} | Type:{ability.abilityType} | Damage:{ability.attack} | Attack Result:{ability.attackResult} | CD:{cooldownLabel} | {location}";
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

            int playerCount = clash.playerAbilityIds == null ? 0 : clash.playerAbilityIds.Count;
            int opponentCount = clash.opponentAbilityIds == null ? 0 : clash.opponentAbilityIds.Count;
            string clashId = string.IsNullOrWhiteSpace(clash.clashId)
                ? "(no-id)"
                : clash.clashId;

            return
                $"Selected Clash: {selectedClashIndex} ({clashId}) | Abilities P:{playerCount} E:{opponentCount}";
        }

        static string ResolveAbilityLocation(DuelState duelState, string abilityId)
        {
            if (duelState.bagAbilityIds != null && duelState.bagAbilityIds.Contains(abilityId))
            {
                return "bag";
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

                if (field.playerAbilityIds != null && field.playerAbilityIds.Contains(abilityId))
                {
                    return $"player@{i}";
                }

                if (field.opponentAbilityIds != null && field.opponentAbilityIds.Contains(abilityId))
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

        static bool TryResolveAbility(DuelState duelState, string abilityId, out AbilityInstance ability)
        {
            ability = null;
            if (duelState == null || duelState.abilitiesById == null || string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            if (!duelState.abilitiesById.TryGetValue(abilityId, out ability) || ability == null)
            {
                return false;
            }

            return true;
        }

        static AbilityDef ResolveAbilityDef(GameDatabase database, string abilityDefId)
        {
            if (database == null ||
                database.abilitiesById == null ||
                string.IsNullOrWhiteSpace(abilityDefId))
            {
                return null;
            }

            if (!database.abilitiesById.TryGetValue(abilityDefId, out AbilityDef abilityDef) || abilityDef == null)
            {
                return null;
            }

            return abilityDef;
        }

        static string ResolveAbilityDefId(AbilityInstance ability, AbilityDef abilityDef)
        {
            if (ability != null && !string.IsNullOrWhiteSpace(ability.abilityDefId))
            {
                return ability.abilityDefId;
            }

            if (abilityDef != null && !string.IsNullOrWhiteSpace(abilityDef.id))
            {
                return abilityDef.id;
            }

            return "(no-def)";
        }

        static string ResolveEffectsLabel(AbilityDef abilityDef)
        {
            if (abilityDef == null || abilityDef.effects == null || abilityDef.effects.Count <= 0)
            {
                return "none";
            }

            var effectLabels = new List<string>();

            for (int i = 0; i < abilityDef.effects.Count; i++)
            {
                TimedEffectDef timedEffect = abilityDef.effects[i];
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
                if (string.Equals(token, "ability", StringComparison.OrdinalIgnoreCase) ||
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


