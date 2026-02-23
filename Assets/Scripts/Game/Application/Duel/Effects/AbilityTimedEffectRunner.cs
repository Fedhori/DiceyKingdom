using System;
using System.Collections.Generic;
using Game.Domain.Duel;
using Game.Domain.Modifiers;
using Game.Infrastructure.Data;
using UnityEngine;

namespace Game.Application.Duel.Effects
{
    public sealed class AbilityTimedEffectRunner
    {
        const string timedSourcePrefix = "Timed";
        const string rollSourcePrefix = "Timed:Roll:";

        readonly GameDatabase database;
        readonly DuelEffectClashResolver resolver;

        public AbilityTimedEffectRunner(GameDatabase database, DuelEffectClashResolver resolver = null)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            this.resolver = resolver ?? new DuelEffectClashResolver();
        }

        public AbilityTimedEffectRunResult ApplyForTiming(DuelState state, DuelEffectTiming timing)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            state.EnsureInitialized();

            if (timing == DuelEffectTiming.Roll)
            {
                ClearPreviousRollTimedModifiers(state);
            }

            List<AbilityRuntimeContext> contexts = BuildAbilityContexts(state);
            int appliedCount = 0;
            int failedCount = 0;
            int skippedCount = 0;

            for (int contextIndex = 0; contextIndex < contexts.Count; contextIndex++)
            {
                AbilityRuntimeContext sourceContext = contexts[contextIndex];
                if (sourceContext.abilityDef == null || sourceContext.abilityDef.effects == null)
                {
                    continue;
                }

                for (int effectIndex = 0; effectIndex < sourceContext.abilityDef.effects.Count; effectIndex++)
                {
                    TimedEffectDef timedEffect = sourceContext.abilityDef.effects[effectIndex];
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

                        List<AbilityRuntimeContext> targets = ClashResolveTargets(contexts, sourceContext, opDef);
                        if (targets.Count <= 0)
                        {
                            skippedCount += 1;
                            continue;
                        }

                        for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                        {
                            AbilityRuntimeContext targetContext = targets[targetIndex];
                            if (!TryCreateCommand(
                                    sourceContext,
                                    targetContext,
                                    timing,
                                    effectIndex,
                                    opIndex,
                                    opDef,
                                    out DuelEffectCommand command,
                                    out string warningMessage))
                            {
                                failedCount += 1;
                                Debug.LogWarning($"[AbilityTimedEffectRunner] {warningMessage}");
                                continue;
                            }

                            DuelEffectResult result = resolver.Apply(state, command);
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

            return new AbilityTimedEffectRunResult(appliedCount, failedCount, skippedCount);
        }

        static bool IsTimingMatch(TimedEffectDef timedEffect, DuelEffectTiming timing)
        {
            if (timedEffect == null || string.IsNullOrWhiteSpace(timedEffect.timing))
            {
                return false;
            }

            return Enum.TryParse(timedEffect.timing, false, out DuelEffectTiming parsedTiming) &&
                   parsedTiming == timing;
        }

        static bool EvaluateCondition(
            DuelState state,
            AbilityRuntimeContext sourceContext,
            ConditionDef condition,
            List<AbilityRuntimeContext> allContexts)
        {
            if (condition == null || string.IsNullOrWhiteSpace(condition.type))
            {
                return true;
            }

            switch (condition.type)
            {
                case "Always":
                    return true;
                case "IsInAbilityHolder":
                    return sourceContext.clashIndex < 0;
                case "OpponentCountEquals":
                {
                    if (sourceContext.clashIndex < 0)
                    {
                        return false;
                    }

                    int expectedCount = condition.count ?? condition.value ?? 1;
                    int opponentCount = 0;
                    for (int i = 0; i < allContexts.Count; i++)
                    {
                        AbilityRuntimeContext target = allContexts[i];
                        if (target.clashIndex != sourceContext.clashIndex)
                        {
                            continue;
                        }

                        if (target.isPlayerSide == sourceContext.isPlayerSide)
                        {
                            continue;
                        }

                        opponentCount += 1;
                    }

                    return opponentCount == expectedCount;
                }
                case "HasTag":
                    if (string.IsNullOrWhiteSpace(condition.tag))
                    {
                        return false;
                    }

                    if (sourceContext.ability == null || sourceContext.ability.tags == null)
                    {
                        return false;
                    }

                    return sourceContext.ability.tags.Contains(condition.tag);
                default:
                    Debug.LogWarning($"[AbilityTimedEffectRunner] Unsupported condition type '{condition.type}'.");
                    return false;
            }
        }

        static List<AbilityRuntimeContext> ClashResolveTargets(
            List<AbilityRuntimeContext> contexts,
            AbilityRuntimeContext sourceContext,
            EffectOpDef opDef)
        {
            var targets = new List<AbilityRuntimeContext>();
            string scope = string.IsNullOrWhiteSpace(opDef.scope) ? "Self" : opDef.scope;

            for (int i = 0; i < contexts.Count; i++)
            {
                AbilityRuntimeContext candidate = contexts[i];
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
            AbilityRuntimeContext sourceContext,
            AbilityRuntimeContext candidateContext)
        {
            switch (scope)
            {
                case "Self":
                    return string.Equals(
                        sourceContext.abilityId,
                        candidateContext.abilityId,
                        StringComparison.Ordinal);
                case "AllAbilities":
                case "AllActions":
                    return true;
                case "SameClashActions":
                case "SameClashAbilities":
                case "SameClash":
                    return sourceContext.clashIndex >= 0 &&
                           sourceContext.clashIndex == candidateContext.clashIndex;
                case "SameClashAllies":
                    return sourceContext.clashIndex >= 0 &&
                           sourceContext.clashIndex == candidateContext.clashIndex &&
                           sourceContext.isPlayerSide == candidateContext.isPlayerSide;
                case "SameClashOpponents":
                    return sourceContext.clashIndex >= 0 &&
                           sourceContext.clashIndex == candidateContext.clashIndex &&
                           sourceContext.isPlayerSide != candidateContext.isPlayerSide;
                default:
                    Debug.LogWarning($"[AbilityTimedEffectRunner] Unsupported scope '{scope}'.");
                    return false;
            }
        }

        static bool IsSideMatch(
            string side,
            AbilityRuntimeContext candidateContext)
        {
            if (string.IsNullOrWhiteSpace(side))
            {
                return true;
            }

            if (string.Equals(side, "Player", StringComparison.Ordinal))
            {
                return candidateContext.isPlayerSide;
            }

            if (string.Equals(side, "Opponent", StringComparison.Ordinal))
            {
                return !candidateContext.isPlayerSide;
            }

            Debug.LogWarning($"[AbilityTimedEffectRunner] Unsupported side '{side}'.");
            return false;
        }

        static bool TryCreateCommand(
            AbilityRuntimeContext sourceContext,
            AbilityRuntimeContext targetContext,
            DuelEffectTiming timing,
            int effectIndex,
            int opIndex,
            EffectOpDef opDef,
            out DuelEffectCommand command,
            out string warningMessage)
        {
            command = null;
            warningMessage = string.Empty;

            if (!Enum.TryParse(opDef.op, false, out DuelEffectOpCode opCode))
            {
                warningMessage = $"Unsupported opCode '{opDef.op}'.";
                return false;
            }

            command = new DuelEffectCommand
            {
                opCode = opCode,
                sourceId = BuildSourceId(sourceContext, timing, effectIndex, opIndex),
                abilityId = targetContext.abilityId,
                clashIndex = targetContext.clashIndex,
                fromClashIndex = sourceContext.clashIndex,
                toClashIndex = targetContext.clashIndex,
                isPlayerSide = !string.IsNullOrWhiteSpace(opDef.side)
                    ? string.Equals(opDef.side, "Player", StringComparison.Ordinal)
                    : targetContext.isPlayerSide
            };

            if (opCode == DuelEffectOpCode.ModifyAttackResult || opCode == DuelEffectOpCode.AddAttackModifier)
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

            if (opCode == DuelEffectOpCode.AddAttackModifier)
            {
                if (!TryParseModifierLayer(opDef.layer, out ModifierLayer layer))
                {
                    warningMessage = $"Invalid layer '{opDef.layer}' for AddAttackModifier.";
                    return false;
                }

                if (!TryParseModifierTarget(opDef.target, out DuelModifierTarget target))
                {
                    warningMessage = $"Invalid target '{opDef.target}' for AddAttackModifier.";
                    return false;
                }

                command.modifierLayer = layer;
                command.modifierTarget = target;
            }

            if (opCode == DuelEffectOpCode.ModifyTotalAttack || opCode == DuelEffectOpCode.ModifyHealth)
            {
                if (!opDef.TryGetAmount(out int amount))
                {
                    warningMessage = $"Missing amount for op '{opDef.op}'.";
                    return false;
                }

                command.amount = amount;
                command.clashIndex = sourceContext.clashIndex;
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
            if (string.Equals(layer, "Duel", StringComparison.Ordinal))
            {
                modifierLayer = ModifierLayer.Duel;
                return true;
            }

            if (string.Equals(layer, "Permanent", StringComparison.Ordinal))
            {
                modifierLayer = ModifierLayer.Permanent;
                return true;
            }

            modifierLayer = ModifierLayer.Duel;
            return false;
        }

        static bool TryParseModifierTarget(string target, out DuelModifierTarget modifierTarget)
        {
            if (string.Equals(target, "Attack", StringComparison.Ordinal))
            {
                modifierTarget = DuelModifierTarget.Attack;
                return true;
            }

            if (string.Equals(target, "AttackResult", StringComparison.Ordinal))
            {
                modifierTarget = DuelModifierTarget.AttackResult;
                return true;
            }

            modifierTarget = DuelModifierTarget.Attack;
            return false;
        }

        static string BuildSourceId(
            AbilityRuntimeContext sourceContext,
            DuelEffectTiming timing,
            int effectIndex,
            int opIndex)
        {
            return $"{timedSourcePrefix}:{timing}:{sourceContext.abilityId}:{effectIndex}:{opIndex}";
        }

        static void ClearPreviousRollTimedModifiers(DuelState state)
        {
            foreach (KeyValuePair<string, AbilityInstance> pair in state.abilitiesById)
            {
                AbilityInstance ability = pair.Value;
                if (ability == null)
                {
                    continue;
                }

                ability.EnsureInitialized();
                RemoveSourcePrefixedModifiers(ability.attackModifiers, rollSourcePrefix);
                RemoveSourcePrefixedModifiers(ability.attackResultModifiers, rollSourcePrefix);
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

        List<AbilityRuntimeContext> BuildAbilityContexts(DuelState state)
        {
            var contexts = new List<AbilityRuntimeContext>();
            var visitedAbilityIds = new HashSet<string>(StringComparer.Ordinal);

            if (state.abilityHolderAbilityIds != null)
            {
                for (int i = 0; i < state.abilityHolderAbilityIds.Count; i++)
                {
                    TryAddContext(
                        contexts,
                        visitedAbilityIds,
                        state,
                        state.abilityHolderAbilityIds[i],
                        true,
                        -1);
                }
            }

            if (state.clashes == null)
            {
                return contexts;
            }

            for (int clashIndex = 0; clashIndex < state.clashes.Count; clashIndex++)
            {
                ClashState clash = state.clashes[clashIndex];
                if (clash == null)
                {
                    continue;
                }

                clash.EnsureInitialized();

                for (int i = 0; i < clash.playerActionIds.Count; i++)
                {
                    TryAddContext(
                        contexts,
                        visitedAbilityIds,
                        state,
                        clash.playerActionIds[i],
                        true,
                        clashIndex);
                }

                for (int i = 0; i < clash.opponentActionIds.Count; i++)
                {
                    TryAddContext(
                        contexts,
                        visitedAbilityIds,
                        state,
                        clash.opponentActionIds[i],
                        false,
                        clashIndex);
                }
            }

            return contexts;
        }

        void TryAddContext(
            List<AbilityRuntimeContext> contexts,
            HashSet<string> visitedAbilityIds,
            DuelState state,
            string abilityId,
            bool isPlayerSide,
            int clashIndex)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return;
            }

            if (!visitedAbilityIds.Add(abilityId))
            {
                Debug.LogWarning(
                    $"[AbilityTimedEffectRunner] Duplicate abilityId({abilityId}) context detected. Later context was skipped.");
                return;
            }

            if (!state.abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) || ability == null)
            {
                return;
            }

            ability.EnsureInitialized();

            AbilityDef abilityDef = null;
            if (database.abilitiesById != null && !string.IsNullOrWhiteSpace(ability.abilityDefId))
            {
                database.abilitiesById.TryGetValue(ability.abilityDefId, out abilityDef);
            }

            contexts.Add(new AbilityRuntimeContext
            {
                abilityId = abilityId,
                ability = ability,
                abilityDef = abilityDef,
                isPlayerSide = isPlayerSide,
                clashIndex = clashIndex
            });
        }

        struct AbilityRuntimeContext
        {
            public string abilityId;
            public AbilityInstance ability;
            public AbilityDef abilityDef;
            public bool isPlayerSide;
            public int clashIndex;
        }
    }
}
