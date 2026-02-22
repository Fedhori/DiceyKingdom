using System;
using System.Collections.Generic;
using Game.Domain.Battle;
using Game.Domain.Modifiers;
using Game.Infrastructure.Data;
using UnityEngine;

namespace Game.Application.Battle.Effects
{
    public sealed class TroopTimedEffectRunner
    {
        const string timedSourcePrefix = "Timed";
        const string rollSourcePrefix = "Timed:Roll:";

        readonly GameDatabase database;
        readonly BattleEffectResolver resolver;

        public TroopTimedEffectRunner(GameDatabase database, BattleEffectResolver resolver = null)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            this.resolver = resolver ?? new BattleEffectResolver();
        }

        public TroopTimedEffectRunResult ApplyForTiming(BattleState state, BattleEffectTiming timing)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            state.EnsureInitialized();

            if (timing == BattleEffectTiming.Roll)
            {
                ClearPreviousRollTimedModifiers(state);
            }

            List<TroopRuntimeContext> contexts = BuildTroopContexts(state);
            int appliedCount = 0;
            int failedCount = 0;
            int skippedCount = 0;

            for (int contextIndex = 0; contextIndex < contexts.Count; contextIndex++)
            {
                TroopRuntimeContext sourceContext = contexts[contextIndex];
                if (sourceContext.troopDef == null || sourceContext.troopDef.effects == null)
                {
                    continue;
                }

                for (int effectIndex = 0; effectIndex < sourceContext.troopDef.effects.Count; effectIndex++)
                {
                    TimedEffectDef timedEffect = sourceContext.troopDef.effects[effectIndex];
                    if (!IsTimingMatch(timedEffect, timing))
                    {
                        continue;
                    }

                    if (!EvaluateCondition(state, sourceContext, timedEffect.condition, contexts))
                    {
                        skippedCount += 1;
                        continue;
                    }

                    if (timedEffect.ops == null || timedEffect.ops.Count <= 0)
                    {
                        skippedCount += 1;
                        continue;
                    }

                    for (int opIndex = 0; opIndex < timedEffect.ops.Count; opIndex++)
                    {
                        EffectOpDef opDef = timedEffect.ops[opIndex];
                        if (opDef == null)
                        {
                            skippedCount += 1;
                            continue;
                        }

                        List<TroopRuntimeContext> targets = ResolveTargets(contexts, sourceContext, opDef);
                        if (targets.Count <= 0)
                        {
                            skippedCount += 1;
                            continue;
                        }

                        for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                        {
                            TroopRuntimeContext targetContext = targets[targetIndex];
                            if (!TryCreateCommand(
                                    sourceContext,
                                    targetContext,
                                    timing,
                                    effectIndex,
                                    opIndex,
                                    opDef,
                                    out BattleEffectCommand command,
                                    out string warningMessage))
                            {
                                failedCount += 1;
                                Debug.LogWarning($"[TroopTimedEffectRunner] {warningMessage}");
                                continue;
                            }

                            BattleEffectResult result = resolver.Apply(state, command);
                            if (result.isSuccess)
                            {
                                appliedCount += 1;
                            }
                            else
                            {
                                failedCount += 1;
                            }
                        }
                    }
                }
            }

            return new TroopTimedEffectRunResult(appliedCount, failedCount, skippedCount);
        }

        static bool IsTimingMatch(TimedEffectDef timedEffect, BattleEffectTiming timing)
        {
            if (timedEffect == null || string.IsNullOrWhiteSpace(timedEffect.timing))
            {
                return false;
            }

            return Enum.TryParse(timedEffect.timing, false, out BattleEffectTiming parsedTiming) &&
                   parsedTiming == timing;
        }

        static bool EvaluateCondition(
            BattleState state,
            TroopRuntimeContext sourceContext,
            ConditionDef condition,
            List<TroopRuntimeContext> allContexts)
        {
            if (condition == null || string.IsNullOrWhiteSpace(condition.type))
            {
                return true;
            }

            switch (condition.type)
            {
                case "Always":
                    return true;
                case "IsInCamp":
                    return sourceContext.battlefieldIndex < 0;
                case "EnemyCountEquals":
                {
                    if (sourceContext.battlefieldIndex < 0)
                    {
                        return false;
                    }

                    int expectedCount = condition.count ?? condition.value ?? 1;
                    int enemyCount = 0;
                    for (int i = 0; i < allContexts.Count; i++)
                    {
                        TroopRuntimeContext target = allContexts[i];
                        if (target.battlefieldIndex != sourceContext.battlefieldIndex)
                        {
                            continue;
                        }

                        if (target.isPlayerSide == sourceContext.isPlayerSide)
                        {
                            continue;
                        }

                        enemyCount += 1;
                    }

                    return enemyCount == expectedCount;
                }
                case "HasTag":
                    if (string.IsNullOrWhiteSpace(condition.tag))
                    {
                        return false;
                    }

                    if (sourceContext.troop == null || sourceContext.troop.tags == null)
                    {
                        return false;
                    }

                    return sourceContext.troop.tags.Contains(condition.tag);
                default:
                    Debug.LogWarning($"[TroopTimedEffectRunner] Unsupported condition type '{condition.type}'.");
                    return false;
            }
        }

        static List<TroopRuntimeContext> ResolveTargets(
            List<TroopRuntimeContext> contexts,
            TroopRuntimeContext sourceContext,
            EffectOpDef opDef)
        {
            var targets = new List<TroopRuntimeContext>();
            string scope = string.IsNullOrWhiteSpace(opDef.scope) ? "Self" : opDef.scope;

            for (int i = 0; i < contexts.Count; i++)
            {
                TroopRuntimeContext candidate = contexts[i];
                if (!IsScopeMatch(scope, sourceContext, candidate))
                {
                    continue;
                }

                if (!IsSideMatch(opDef.side, candidate))
                {
                    continue;
                }

                targets.Add(candidate);
            }

            return targets;
        }

        static bool IsScopeMatch(
            string scope,
            TroopRuntimeContext sourceContext,
            TroopRuntimeContext candidateContext)
        {
            switch (scope)
            {
                case "Self":
                    return string.Equals(
                        sourceContext.troopId,
                        candidateContext.troopId,
                        StringComparison.Ordinal);
                case "AllTroops":
                    return true;
                case "SameBattlefieldTroops":
                case "SameBattlefield":
                    return sourceContext.battlefieldIndex >= 0 &&
                           sourceContext.battlefieldIndex == candidateContext.battlefieldIndex;
                case "SameBattlefieldAllies":
                    return sourceContext.battlefieldIndex >= 0 &&
                           sourceContext.battlefieldIndex == candidateContext.battlefieldIndex &&
                           sourceContext.isPlayerSide == candidateContext.isPlayerSide;
                case "SameBattlefieldEnemies":
                    return sourceContext.battlefieldIndex >= 0 &&
                           sourceContext.battlefieldIndex == candidateContext.battlefieldIndex &&
                           sourceContext.isPlayerSide != candidateContext.isPlayerSide;
                default:
                    Debug.LogWarning($"[TroopTimedEffectRunner] Unsupported scope '{scope}'.");
                    return false;
            }
        }

        static bool IsSideMatch(
            string side,
            TroopRuntimeContext candidateContext)
        {
            if (string.IsNullOrWhiteSpace(side))
            {
                return true;
            }

            if (string.Equals(side, "Player", StringComparison.Ordinal))
            {
                return candidateContext.isPlayerSide;
            }

            if (string.Equals(side, "Enemy", StringComparison.Ordinal))
            {
                return !candidateContext.isPlayerSide;
            }

            Debug.LogWarning($"[TroopTimedEffectRunner] Unsupported side '{side}'.");
            return false;
        }

        static bool TryCreateCommand(
            TroopRuntimeContext sourceContext,
            TroopRuntimeContext targetContext,
            BattleEffectTiming timing,
            int effectIndex,
            int opIndex,
            EffectOpDef opDef,
            out BattleEffectCommand command,
            out string warningMessage)
        {
            command = null;
            warningMessage = string.Empty;

            if (!Enum.TryParse(opDef.op, false, out BattleEffectOpCode opCode))
            {
                warningMessage = $"Unsupported opCode '{opDef.op}'.";
                return false;
            }

            command = new BattleEffectCommand
            {
                opCode = opCode,
                sourceId = BuildSourceId(sourceContext, timing, effectIndex, opIndex),
                troopId = targetContext.troopId,
                battlefieldIndex = targetContext.battlefieldIndex,
                fromBattlefieldIndex = sourceContext.battlefieldIndex,
                toBattlefieldIndex = targetContext.battlefieldIndex,
                isPlayerSide = !string.IsNullOrWhiteSpace(opDef.side)
                    ? string.Equals(opDef.side, "Player", StringComparison.Ordinal)
                    : targetContext.isPlayerSide
            };

            if (opCode == BattleEffectOpCode.ModifyAttackResult || opCode == BattleEffectOpCode.AddAttackModifier)
            {
                if (!TryParseModifierOperation(opDef.mode, out NumericModifierOperation modifierOperation))
                {
                    warningMessage = $"Invalid mode '{opDef.mode}' for op '{opDef.op}'.";
                    return false;
                }

                if (!opDef.TryGetAmount(out int amount))
                {
                    warningMessage = $"Missing amount for op '{opDef.op}'.";
                    return false;
                }

                command.modifierOperation = modifierOperation;
                command.amount = amount;
            }

            if (opCode == BattleEffectOpCode.AddAttackModifier)
            {
                if (!TryParseModifierLayer(opDef.layer, out ModifierLayer layer))
                {
                    warningMessage = $"Invalid layer '{opDef.layer}' for AddAttackModifier.";
                    return false;
                }

                if (!TryParseModifierTarget(opDef.target, out BattleModifierTarget target))
                {
                    warningMessage = $"Invalid target '{opDef.target}' for AddAttackModifier.";
                    return false;
                }

                command.modifierLayer = layer;
                command.modifierTarget = target;
            }

            if (opCode == BattleEffectOpCode.TransformOutcome)
            {
                if (!Enum.TryParse(opDef.transformKind, false, out BattleOutcomeTransformKind transformKind))
                {
                    warningMessage = $"Invalid transformKind '{opDef.transformKind}'.";
                    return false;
                }

                command.transformKind = transformKind;
            }

            if (opCode == BattleEffectOpCode.ModifyTotalAttack || opCode == BattleEffectOpCode.ModifyMorale)
            {
                if (!opDef.TryGetAmount(out int amount))
                {
                    warningMessage = $"Missing amount for op '{opDef.op}'.";
                    return false;
                }

                command.amount = amount;
                command.battlefieldIndex = sourceContext.battlefieldIndex;
            }

            return true;
        }

        static bool TryParseModifierOperation(string mode, out NumericModifierOperation operation)
        {
            if (string.Equals(mode, "Add", StringComparison.Ordinal))
            {
                operation = NumericModifierOperation.Add;
                return true;
            }

            if (string.Equals(mode, "PercentBonus", StringComparison.Ordinal))
            {
                operation = NumericModifierOperation.PercentBonus;
                return true;
            }

            operation = NumericModifierOperation.Add;
            return false;
        }

        static bool TryParseModifierLayer(string layer, out ModifierLayer modifierLayer)
        {
            if (string.Equals(layer, "Battle", StringComparison.Ordinal))
            {
                modifierLayer = ModifierLayer.Battle;
                return true;
            }

            if (string.Equals(layer, "Permanent", StringComparison.Ordinal))
            {
                modifierLayer = ModifierLayer.Permanent;
                return true;
            }

            modifierLayer = ModifierLayer.Battle;
            return false;
        }

        static bool TryParseModifierTarget(string target, out BattleModifierTarget modifierTarget)
        {
            if (string.Equals(target, "Attack", StringComparison.Ordinal))
            {
                modifierTarget = BattleModifierTarget.Attack;
                return true;
            }

            if (string.Equals(target, "AttackResult", StringComparison.Ordinal))
            {
                modifierTarget = BattleModifierTarget.AttackResult;
                return true;
            }

            modifierTarget = BattleModifierTarget.Attack;
            return false;
        }

        static string BuildSourceId(
            TroopRuntimeContext sourceContext,
            BattleEffectTiming timing,
            int effectIndex,
            int opIndex)
        {
            return $"{timedSourcePrefix}:{timing}:{sourceContext.troopId}:{effectIndex}:{opIndex}";
        }

        static void ClearPreviousRollTimedModifiers(BattleState state)
        {
            foreach (KeyValuePair<string, TroopInstance> pair in state.troopsById)
            {
                TroopInstance troop = pair.Value;
                if (troop == null)
                {
                    continue;
                }

                troop.EnsureInitialized();
                RemoveSourcePrefixedModifiers(troop.attackResultModifiers, rollSourcePrefix);
            }
        }

        static void RemoveSourcePrefixedModifiers(List<NumericModifier> modifiers, string sourcePrefix)
        {
            if (modifiers == null || modifiers.Count <= 0)
            {
                return;
            }

            for (int i = modifiers.Count - 1; i >= 0; i--)
            {
                NumericModifier modifier = modifiers[i];
                if (modifier == null || string.IsNullOrWhiteSpace(modifier.sourceId))
                {
                    continue;
                }

                if (!modifier.sourceId.StartsWith(sourcePrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                modifiers.RemoveAt(i);
            }
        }

        List<TroopRuntimeContext> BuildTroopContexts(BattleState state)
        {
            var contexts = new List<TroopRuntimeContext>();
            var visitedTroopIds = new HashSet<string>(StringComparer.Ordinal);

            if (state.campTroopIds != null)
            {
                for (int i = 0; i < state.campTroopIds.Count; i++)
                {
                    TryAddContext(
                        contexts,
                        visitedTroopIds,
                        state,
                        state.campTroopIds[i],
                        true,
                        -1);
                }
            }

            if (state.battlefields == null)
            {
                return contexts;
            }

            for (int battlefieldIndex = 0; battlefieldIndex < state.battlefields.Count; battlefieldIndex++)
            {
                BattlefieldState battlefield = state.battlefields[battlefieldIndex];
                if (battlefield == null)
                {
                    continue;
                }

                battlefield.EnsureInitialized();

                for (int i = 0; i < battlefield.playerTroopIds.Count; i++)
                {
                    TryAddContext(
                        contexts,
                        visitedTroopIds,
                        state,
                        battlefield.playerTroopIds[i],
                        true,
                        battlefieldIndex);
                }

                for (int i = 0; i < battlefield.enemyTroopIds.Count; i++)
                {
                    TryAddContext(
                        contexts,
                        visitedTroopIds,
                        state,
                        battlefield.enemyTroopIds[i],
                        false,
                        battlefieldIndex);
                }
            }

            return contexts;
        }

        void TryAddContext(
            List<TroopRuntimeContext> contexts,
            HashSet<string> visitedTroopIds,
            BattleState state,
            string troopId,
            bool isPlayerSide,
            int battlefieldIndex)
        {
            if (string.IsNullOrWhiteSpace(troopId))
            {
                return;
            }

            if (!visitedTroopIds.Add(troopId))
            {
                return;
            }

            if (!state.troopsById.TryGetValue(troopId, out TroopInstance troop) || troop == null)
            {
                return;
            }

            troop.EnsureInitialized();

            TroopDef troopDef = null;
            if (database.troopsById != null && !string.IsNullOrWhiteSpace(troop.troopDefId))
            {
                database.troopsById.TryGetValue(troop.troopDefId, out troopDef);
            }

            contexts.Add(new TroopRuntimeContext
            {
                troopId = troopId,
                troop = troop,
                troopDef = troopDef,
                isPlayerSide = isPlayerSide,
                battlefieldIndex = battlefieldIndex
            });
        }

        struct TroopRuntimeContext
        {
            public string troopId;
            public TroopInstance troop;
            public TroopDef troopDef;
            public bool isPlayerSide;
            public int battlefieldIndex;
        }
    }
}
