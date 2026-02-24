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

        public static string FormatCombat(DuelState duelState, int combatIndex)
        {
            if (duelState == null ||
                duelState.combats == null ||
                combatIndex < 0 ||
                combatIndex >= duelState.combats.Count)
            {
                return $"Combat {combatIndex}: (missing)";
            }

            CombatState combat = duelState.combats[combatIndex];
            if (combat == null)
            {
                return $"Combat {combatIndex}: (missing)";
            }

            int playerCount = combat.playerAbilityIds == null ? 0 : combat.playerAbilityIds.Count;
            int opponentCount = combat.opponentAbilityIds == null ? 0 : combat.opponentAbilityIds.Count;

            int playerTotalPower = DuelSimulator.ComputeTotalPower(
                combat,
                duelState.abilitiesById,
                true);

            int opponentTotalPower = DuelSimulator.ComputeTotalPower(
                combat,
                duelState.abilitiesById,
                false);

            string capLabel = combat.maxPlayerAssignments.HasValue
                ? combat.maxPlayerAssignments.Value.ToString()
                : "unlimited";

            return
                $"Combat {combatIndex} | TotalPower P:{playerTotalPower} E:{opponentTotalPower} | Abilities P:{playerCount} E:{opponentCount} | Cap:{capLabel}";
        }

        public static string FormatLoadoutAbilities(DuelState duelState, string selectedAbilityId)
        {
            string selectedLine = FormatSelectedAbility(duelState, selectedAbilityId);

            if (duelState == null ||
                duelState.loadoutAbilityIds == null ||
                duelState.loadoutAbilityIds.Count <= 0)
            {
                return $"{selectedLine}\nLoadout Abilities: none";
            }

            var lines = new List<string>
            {
                selectedLine,
                $"Loadout Abilities ({duelState.loadoutAbilityIds.Count}):"
            };

            for (int i = 0; i < duelState.loadoutAbilityIds.Count; i++)
            {
                string abilityId = duelState.loadoutAbilityIds[i];
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
                    $"- {abilityDefId} | Type:{ability.abilityType} | Power:{ability.power} | Power Result:{ability.powerResult} | CD:{cooldownLabel}{selectedSuffix}");
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
            return $"Selected Ability: {abilityDefId} | Type:{ability.abilityType} | Power:{ability.power} | Power Result:{ability.powerResult} | CD:{cooldownLabel} | {location}";
        }

        public static string FormatSelectedCombat(DuelState duelState, int selectedCombatIndex)
        {
            if (duelState == null ||
                duelState.combats == null ||
                selectedCombatIndex < 0 ||
                selectedCombatIndex >= duelState.combats.Count)
            {
                return "Selected Combat: (none)";
            }

            CombatState combat = duelState.combats[selectedCombatIndex];
            if (combat == null)
            {
                return $"Selected Combat: {selectedCombatIndex} (missing)";
            }

            int playerCount = combat.playerAbilityIds == null ? 0 : combat.playerAbilityIds.Count;
            int opponentCount = combat.opponentAbilityIds == null ? 0 : combat.opponentAbilityIds.Count;

            return
                $"Selected Combat: {selectedCombatIndex} | Abilities P:{playerCount} E:{opponentCount}";
        }

        static string ResolveAbilityLocation(DuelState duelState, string abilityId)
        {
            if (duelState.loadoutAbilityIds != null && duelState.loadoutAbilityIds.Contains(abilityId))
            {
                return "loadout";
            }

            if (duelState.combats == null)
            {
                return "unknown";
            }

            for (int i = 0; i < duelState.combats.Count; i++)
            {
                CombatState combat = duelState.combats[i];
                if (combat == null)
                {
                    continue;
                }

                if (combat.playerAbilityIds != null && combat.playerAbilityIds.Contains(abilityId))
                {
                    return $"player@{i}";
                }

                if (combat.opponentAbilityIds != null && combat.opponentAbilityIds.Contains(abilityId))
                {
                    return $"opponent@{i}";
                }
            }

            return "unknown";
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

            return string.Join(" / ", effectLabels);
        }

        static string ToDisplayLabel(string rawLabel)
        {
            if (string.IsNullOrWhiteSpace(rawLabel))
            {
                return string.Empty;
            }

            string normalized = rawLabel.Replace('_', ' ').Replace('.', ' ').Trim();
            if (normalized.Length <= 0)
            {
                return string.Empty;
            }

            string[] chunks = normalized.Split(' ');
            var builder = new StringBuilder(normalized.Length);
            for (int i = 0; i < chunks.Length; i++)
            {
                string chunk = chunks[i];
                if (string.IsNullOrWhiteSpace(chunk))
                {
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }

                string lower = chunk.ToLowerInvariant();
                builder.Append(char.ToUpperInvariant(lower[0]));
                if (lower.Length > 1)
                {
                    builder.Append(lower.Substring(1));
                }
            }

            return builder.ToString();
        }
    }
}
