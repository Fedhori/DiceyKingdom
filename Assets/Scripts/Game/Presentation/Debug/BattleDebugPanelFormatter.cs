using Game.Application.Battle;
using Game.Domain.Battle;
using Game.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Presentation.Debug
{
    public static class BattleDebugPanelFormatter
    {
        public static string FormatPhase(BattlePhaseRunner phaseRunner)
        {
            string phaseLabel = phaseRunner == null ? "(none)" : phaseRunner.currentPhase.ToString();
            return $"Phase: {phaseLabel}";
        }

        public static string FormatTurn(BattleState battleState)
        {
            int turn = battleState == null ? 0 : battleState.turnIndex;
            return $"Turn: {turn}";
        }

        public static string FormatMana(BattleState battleState)
        {
            int mana = battleState == null ? 0 : battleState.mana;
            return $"Mana: {mana}";
        }

        public static string FormatStability(BattleState battleState)
        {
            int stability = battleState == null ? 0 : battleState.stability;
            return $"Stability: {stability}";
        }

        public static string FormatPlayerMorale(BattleState battleState)
        {
            int playerMorale = battleState == null ? 0 : battleState.playerMorale;
            return $"Player Morale: {playerMorale}";
        }

        public static string FormatEnemyMorale(BattleState battleState)
        {
            int enemyMorale = battleState == null ? 0 : battleState.enemyMorale;
            return $"Enemy Morale: {enemyMorale}";
        }

        public static string FormatBattlefield(
            BattleState battleState,
            int battlefieldIndex,
            GameDatabase database = null)
        {
            if (battleState == null ||
                battleState.battlefields == null ||
                battlefieldIndex < 0 ||
                battlefieldIndex >= battleState.battlefields.Count)
            {
                return $"Battlefield {battlefieldIndex}: (missing)";
            }

            BattlefieldState battlefield = battleState.battlefields[battlefieldIndex];
            if (battlefield == null)
            {
                return $"Battlefield {battlefieldIndex}: (missing)";
            }

            int playerCount = battlefield.playerTroopIds == null ? 0 : battlefield.playerTroopIds.Count;
            int enemyCount = battlefield.enemyTroopIds == null ? 0 : battlefield.enemyTroopIds.Count;

            int playerTotalAttack = ComputeTotalAttackPreview(
                battleState,
                battlefield.playerTroopIds,
                battlefield.totalAttackBonusPlayer);

            int enemyTotalAttack = ComputeTotalAttackPreview(
                battleState,
                battlefield.enemyTroopIds,
                battlefield.totalAttackBonusEnemy);

            string battlefieldId = string.IsNullOrWhiteSpace(battlefield.battlefieldId)
                ? "(no-id)"
                : battlefield.battlefieldId;

            string slotLabel = battlefield.slotLimit.HasValue
                ? battlefield.slotLimit.Value.ToString()
                : "unlimited";

            return
                $"Battlefield {battlefieldIndex} ({battlefieldId}) | TotalAttack P:{playerTotalAttack} E:{enemyTotalAttack} | Troops P:{playerCount} E:{enemyCount} | Slot:{slotLabel}";
        }

        public static string FormatCampTroops(BattleState battleState, string selectedTroopId)
        {
            string selectedLine = FormatSelectedTroop(battleState, selectedTroopId);

            if (battleState == null ||
                battleState.campTroopIds == null ||
                battleState.campTroopIds.Count <= 0)
            {
                return $"{selectedLine}\nCamp Troops: none";
            }

            var lines = new List<string>
            {
                selectedLine,
                $"Camp Troops ({battleState.campTroopIds.Count}):"
            };

            for (int i = 0; i < battleState.campTroopIds.Count; i++)
            {
                string troopId = battleState.campTroopIds[i];
                if (!TryResolveTroop(battleState, troopId, out TroopInstance troop))
                {
                    lines.Add("- (missing troop)");
                    continue;
                }

                string troopDefId = ResolveTroopDefId(troop, null);
                bool isSelected = string.Equals(troopId, selectedTroopId, StringComparison.Ordinal);
                string selectedSuffix = isSelected ? " <selected>" : string.Empty;
                lines.Add(
                    $"- {troopDefId} | Attack:{troop.attack} | Attack Result:{troop.attackResult}{selectedSuffix}");
            }

            return string.Join("\n", lines);
        }

        public static string FormatTroopEffects(GameDatabase database, string troopDefId)
        {
            TroopDef troopDef = ResolveTroopDef(database, troopDefId);
            return ResolveEffectsLabel(troopDef);
        }

        public static string FormatSelectedTroop(BattleState battleState, string selectedTroopId)
        {
            if (string.IsNullOrWhiteSpace(selectedTroopId))
            {
                return "Selected Troop: (none)";
            }

            if (battleState == null ||
                battleState.troopsById == null ||
                !battleState.troopsById.TryGetValue(selectedTroopId, out TroopInstance troop) ||
                troop == null)
            {
                return "Selected Troop: (missing)";
            }

            string location = ResolveTroopLocation(battleState, selectedTroopId);
            string troopDefId = ResolveTroopDefId(troop, null);
            return $"Selected Troop: {troopDefId} | Attack:{troop.attack} | Attack Result:{troop.attackResult} | {location}";
        }

        public static string FormatSelectedBattlefield(BattleState battleState, int selectedBattlefieldIndex)
        {
            if (battleState == null ||
                battleState.battlefields == null ||
                selectedBattlefieldIndex < 0 ||
                selectedBattlefieldIndex >= battleState.battlefields.Count)
            {
                return "Selected Battlefield: (none)";
            }

            BattlefieldState battlefield = battleState.battlefields[selectedBattlefieldIndex];
            if (battlefield == null)
            {
                return $"Selected Battlefield: {selectedBattlefieldIndex} (missing)";
            }

            int playerCount = battlefield.playerTroopIds == null ? 0 : battlefield.playerTroopIds.Count;
            int enemyCount = battlefield.enemyTroopIds == null ? 0 : battlefield.enemyTroopIds.Count;
            string battlefieldId = string.IsNullOrWhiteSpace(battlefield.battlefieldId)
                ? "(no-id)"
                : battlefield.battlefieldId;

            return
                $"Selected Battlefield: {selectedBattlefieldIndex} ({battlefieldId}) | Troops P:{playerCount} E:{enemyCount}";
        }

        static string ResolveTroopLocation(BattleState battleState, string troopId)
        {
            if (battleState.campTroopIds != null && battleState.campTroopIds.Contains(troopId))
            {
                return "camp";
            }

            if (battleState.battlefields == null)
            {
                return "unknown";
            }

            for (int i = 0; i < battleState.battlefields.Count; i++)
            {
                BattlefieldState field = battleState.battlefields[i];
                if (field == null)
                {
                    continue;
                }

                if (field.playerTroopIds != null && field.playerTroopIds.Contains(troopId))
                {
                    return $"player@{i}";
                }

                if (field.enemyTroopIds != null && field.enemyTroopIds.Contains(troopId))
                {
                    return $"enemy@{i}";
                }
            }

            return "unknown";
        }

        static int ComputeTotalAttackPreview(
            BattleState battleState,
            List<string> troopIds,
            int totalAttackBonus)
        {
            int total = totalAttackBonus;
            if (battleState == null || battleState.troopsById == null || troopIds == null)
            {
                return total;
            }

            for (int i = 0; i < troopIds.Count; i++)
            {
                string troopId = troopIds[i];
                if (string.IsNullOrWhiteSpace(troopId))
                {
                    continue;
                }

                if (!battleState.troopsById.TryGetValue(troopId, out TroopInstance troop) || troop == null)
                {
                    continue;
                }

                total += troop.attackResult;
            }

            return total;
        }

        static bool TryResolveTroop(BattleState battleState, string troopId, out TroopInstance troop)
        {
            troop = null;
            if (battleState == null || battleState.troopsById == null || string.IsNullOrWhiteSpace(troopId))
            {
                return false;
            }

            if (!battleState.troopsById.TryGetValue(troopId, out troop) || troop == null)
            {
                return false;
            }

            return true;
        }

        static TroopDef ResolveTroopDef(GameDatabase database, string troopDefId)
        {
            if (database == null ||
                database.troopsById == null ||
                string.IsNullOrWhiteSpace(troopDefId))
            {
                return null;
            }

            if (!database.troopsById.TryGetValue(troopDefId, out TroopDef troopDef) || troopDef == null)
            {
                return null;
            }

            return troopDef;
        }

        static string ResolveTroopDefId(TroopInstance troop, TroopDef troopDef)
        {
            if (troop != null && !string.IsNullOrWhiteSpace(troop.troopDefId))
            {
                return troop.troopDefId;
            }

            if (troopDef != null && !string.IsNullOrWhiteSpace(troopDef.id))
            {
                return troopDef.id;
            }

            return "(no-def)";
        }

        static string ResolveEffectsLabel(TroopDef troopDef)
        {
            if (troopDef == null || troopDef.effects == null || troopDef.effects.Count <= 0)
            {
                return "none";
            }

            var effectLabels = new List<string>();

            for (int i = 0; i < troopDef.effects.Count; i++)
            {
                TimedEffectDef timedEffect = troopDef.effects[i];
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
                if (string.Equals(token, "troop", StringComparison.OrdinalIgnoreCase) ||
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
