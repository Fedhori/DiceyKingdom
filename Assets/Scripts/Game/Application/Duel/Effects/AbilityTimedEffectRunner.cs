using System;
using System.Collections.Generic;
using Game.Domain.Duel;
using Game.Domain.Modifiers;
using Game.Infrastructure.Data;
using Game.Infrastructure.Data.Effects;
using UnityEngine;

namespace Game.Application.Duel.Effects
{
    public sealed class AbilityTimedEffectRunner
    {
        const string timedSourcePrefix = "Timed";
        const string formationSourcePrefix = "Timed:Formation:";
        const string rollSourcePrefix = "Timed:Roll:";

        readonly GameDatabase database;
        readonly DuelEffectCombatResolver resolver;

        public AbilityTimedEffectRunner(GameDatabase database, DuelEffectCombatResolver resolver = null)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            this.resolver = resolver ?? new DuelEffectCombatResolver();
        }

        public AbilityTimedEffectRunResult ApplyForTiming(DuelState state, DuelEffectTiming timing)
        {
            return ApplyForTiming(state, timing, null, null);
        }

        public AbilityTimedEffectRunResult ApplyForTiming(
            DuelState state,
            DuelEffectTiming timing,
            IReadOnlyCollection<string> sourceAbilityIds)
        {
            return ApplyForTiming(state, timing, sourceAbilityIds, null);
        }

        public AbilityTimedEffectRunResult ApplyForTiming(
            DuelState state,
            DuelEffectTiming timing,
            IReadOnlyCollection<string> sourceAbilityIds,
            DuelEffectContext effectContext)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            state.EnsureInitialized();

            if (timing == DuelEffectTiming.Formation)
            {
                ClearPreviousFormationTimedModifiers(state);
            }
            else if (timing == DuelEffectTiming.Roll)
            {
                ClearPreviousRollTimedModifiers(state);
            }

            List<AbilityRuntimeContext> contexts = BuildAbilityContexts(state);
            HashSet<string> sourceFilter = BuildSourceFilter(sourceAbilityIds);
            DuelEffectContext resolvedEffectContext = effectContext ?? new DuelEffectContext();
            int appliedCount = 0;
            int failedCount = 0;
            int skippedCount = 0;

            for (int contextIndex = 0; contextIndex < contexts.Count; contextIndex++)
            {
                AbilityRuntimeContext sourceContext = contexts[contextIndex];
                if (sourceFilter != null &&
                    !sourceFilter.Contains(sourceContext.abilityId))
                {
                    continue;
                }

                if (!CanRunForTiming(sourceContext, timing))
                {
                    skippedCount += 1;
                    continue;
                }

                if (timing == DuelEffectTiming.HealthLost &&
                    !IsHealthLostSideMatch(sourceContext, resolvedEffectContext))
                {
                    skippedCount += 1;
                    continue;
                }

                if (sourceContext.abilityDef == null || sourceContext.abilityDef.effects == null)
                {
                    continue;
                }

                int sourceAppliedCountBefore = appliedCount;
                for (int effectIndex = 0; effectIndex < sourceContext.abilityDef.effects.Count; effectIndex++)
                {
                    TimedEffectDef timedEffect = sourceContext.abilityDef.effects[effectIndex];
                    if (!IsTimingMatch(timedEffect, timing))
                    {
                        continue;
                    }

                    if (!EvaluateCondition(state, sourceContext, timedEffect.condition, contexts, resolvedEffectContext))
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

                        List<AbilityRuntimeContext> targets = ResolveTargets(contexts, sourceContext, opDef);
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

                            bool shouldSyncPowerResultOnHealthLost = ShouldSyncPowerResultOnHealthLost(
                                timing,
                                resolvedEffectContext,
                                command,
                                targetContext);
                            int effectivePowerBefore = 0;
                            if (shouldSyncPowerResultOnHealthLost)
                            {
                                effectivePowerBefore = ComputeEffectivePower(targetContext.ability);
                            }

                            DuelEffectResult result = resolver.Apply(state, command, resolvedEffectContext);
                            if (result.isSuccess)
                            {
                                appliedCount += 1;

                                if (shouldSyncPowerResultOnHealthLost)
                                {
                                    TrySyncPowerResultWithPowerDelta(
                                        targetContext.ability,
                                        effectivePowerBefore);
                                }

                                if (command.opCode == DuelEffectOpCode.ModifyHealth &&
                                    command.amount < 0 &&
                                    timing != DuelEffectTiming.HealthLost)
                                {
                                    ApplyForTiming(
                                        state,
                                        DuelEffectTiming.HealthLost,
                                        null,
                                        new DuelEffectContext
                                        {
                                            hasHealthLost = true,
                                            healthLostIsPlayerSide = command.isPlayerSide,
                                            healthLostAmount = -command.amount,
                                            hasResolveProgress = resolvedEffectContext.hasResolveProgress,
                                            currentResolvedCombatIndex = resolvedEffectContext.currentResolvedCombatIndex
                                        });
                                }

                                FinalizeDuelEndedIfNeeded(state);
                            }
                            else
                            {
                                failedCount += 1;
                            }
                        }
                    }
                }

                if (appliedCount > sourceAppliedCountBefore &&
                    ShouldConsumeSourceCooldown(sourceContext))
                {
                    TryConsumeSourceCooldown(sourceContext.ability);
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

        static HashSet<string> BuildSourceFilter(IReadOnlyCollection<string> sourceAbilityIds)
        {
            if (sourceAbilityIds == null || sourceAbilityIds.Count <= 0)
            {
                return null;
            }

            return new HashSet<string>(sourceAbilityIds, StringComparer.Ordinal);
        }

        static bool CanRunForTiming(AbilityRuntimeContext sourceContext, DuelEffectTiming timing)
        {
            AbilityInstance ability = sourceContext.ability;
            if (ability == null)
            {
                return false;
            }

            ability.EnsureInitialized();
            if (timing != DuelEffectTiming.HealthLost &&
                ability.cooldownRemaining > 0)
            {
                return false;
            }

            if (timing == DuelEffectTiming.Deploy ||
                timing == DuelEffectTiming.Formation ||
                timing == DuelEffectTiming.AfterCombat)
            {
                return sourceContext.combatIndex >= 0;
            }

            return true;
        }

        static bool IsHealthLostSideMatch(AbilityRuntimeContext sourceContext, DuelEffectContext effectContext)
        {
            if (effectContext == null || !effectContext.hasHealthLost)
            {
                return false;
            }

            return sourceContext.isPlayerSide == effectContext.healthLostIsPlayerSide;
        }

        static bool ShouldSyncPowerResultOnHealthLost(
            DuelEffectTiming timing,
            DuelEffectContext effectContext,
            DuelEffectCommand command,
            AbilityRuntimeContext targetContext)
        {
            if (timing != DuelEffectTiming.HealthLost ||
                effectContext == null ||
                !effectContext.hasResolveProgress ||
                command == null ||
                command.opCode != DuelEffectOpCode.AddPowerModifier ||
                command.modifierTarget != DuelModifierTarget.Power ||
                targetContext.ability == null)
            {
                return false;
            }

            if (targetContext.combatIndex < 0 ||
                targetContext.combatIndex <= effectContext.currentResolvedCombatIndex)
            {
                return false;
            }

            targetContext.ability.EnsureInitialized();
            return targetContext.ability.baseRoll > 0;
        }

        static int ComputeEffectivePower(AbilityInstance ability)
        {
            if (ability == null)
            {
                return 0;
            }

            ability.EnsureInitialized();
            return NumericModifierCalculator.Apply(
                ability.power,
                ability.powerModifiers,
                minValue: 0,
                logContext: "AbilityTimedEffectRunner.ComputeEffectivePower");
        }

        static void TrySyncPowerResultWithPowerDelta(AbilityInstance ability, int effectivePowerBefore)
        {
            if (ability == null)
            {
                return;
            }

            ability.EnsureInitialized();
            if (ability.baseRoll <= 0)
            {
                return;
            }

            int effectivePowerAfter = ComputeEffectivePower(ability);
            int delta = effectivePowerAfter - effectivePowerBefore;
            if (delta == 0)
            {
                return;
            }

            ability.powerResult = Mathf.Max(1, ability.powerResult + delta);
        }

        static bool ShouldConsumeSourceCooldown(AbilityRuntimeContext sourceContext)
        {
            return sourceContext.ability != null &&
                   (sourceContext.ability.abilityType == AbilityType.Skill ||
                    sourceContext.ability.abilityType == AbilityType.Passive);
        }

        static void TryConsumeSourceCooldown(AbilityInstance ability)
        {
            if (ability == null)
            {
                return;
            }

            int cooldownTurns = Mathf.Max(0, ability.cooldownTurns);
            if (cooldownTurns <= 0)
            {
                return;
            }

            ability.cooldownRemaining = cooldownTurns;
        }

        static bool EvaluateCondition(
            DuelState state,
            AbilityRuntimeContext sourceContext,
            ConditionDef condition,
            List<AbilityRuntimeContext> allContexts,
            DuelEffectContext effectContext)
        {
            if (condition == null || string.IsNullOrWhiteSpace(condition.type))
            {
                return true;
            }

            switch (condition.type)
            {
                case "Always":
                    return true;
                case "IsInLoadout":
                    return sourceContext.combatIndex < 0;
                case "OpponentCountEquals":
                {
                    if (sourceContext.combatIndex < 0)
                    {
                        return false;
                    }

                    int expectedCount = condition.count ?? condition.value ?? 1;
                    int opponentCount = 0;
                    for (int i = 0; i < allContexts.Count; i++)
                    {
                        AbilityRuntimeContext target = allContexts[i];
                        if (target.combatIndex != sourceContext.combatIndex)
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
                case "OpponentCountGreaterThanSelf":
                {
                    if (sourceContext.combatIndex < 0)
                    {
                        return false;
                    }

                    int opponentCount = 0;
                    int selfCount = 0;
                    for (int i = 0; i < allContexts.Count; i++)
                    {
                        AbilityRuntimeContext target = allContexts[i];
                        if (target.combatIndex != sourceContext.combatIndex)
                        {
                            continue;
                        }

                        if (target.isPlayerSide == sourceContext.isPlayerSide)
                        {
                            selfCount += 1;
                        }
                        else
                        {
                            opponentCount += 1;
                        }
                    }

                    return opponentCount > selfCount;
                }
                case "OutcomeIsVictory":
                    return IsOutcomeMatch(effectContext, sourceContext.isPlayerSide, DuelOutcome.Victory);
                case "OutcomeIsDefeat":
                    return IsOutcomeMatch(effectContext, sourceContext.isPlayerSide, DuelOutcome.Defeat);
                case "OutcomeIsDraw":
                    return IsOutcomeMatch(effectContext, sourceContext.isPlayerSide, DuelOutcome.Draw);
                default:
                    Debug.LogWarning($"[AbilityTimedEffectRunner] Unsupported condition type '{condition.type}'.");
                    return false;
            }
        }

        static bool IsOutcomeMatch(DuelEffectContext effectContext, bool isPlayerSide, DuelOutcome expected)
        {
            if (effectContext == null || !effectContext.hasOutcome)
            {
                return false;
            }

            DuelOutcome sourceRelativeOutcome = ToSourceRelativeOutcome(effectContext.outcome, isPlayerSide);
            return sourceRelativeOutcome == expected;
        }

        static DuelOutcome ToSourceRelativeOutcome(DuelOutcome playerPerspectiveOutcome, bool isPlayerSide)
        {
            if (isPlayerSide)
            {
                return playerPerspectiveOutcome;
            }

            if (playerPerspectiveOutcome == DuelOutcome.Victory)
            {
                return DuelOutcome.Defeat;
            }

            if (playerPerspectiveOutcome == DuelOutcome.Defeat)
            {
                return DuelOutcome.Victory;
            }

            return DuelOutcome.Draw;
        }

        static List<AbilityRuntimeContext> ResolveTargets(
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
                    return true;
                case "SameCombatAbilities":
                case "SameCombat":
                    return sourceContext.combatIndex >= 0 &&
                           sourceContext.combatIndex == candidateContext.combatIndex;
                case "SameCombatAllies":
                    return sourceContext.combatIndex >= 0 &&
                           sourceContext.combatIndex == candidateContext.combatIndex &&
                           sourceContext.isPlayerSide == candidateContext.isPlayerSide;
                case "SameCombatOpponents":
                    return sourceContext.combatIndex >= 0 &&
                           sourceContext.combatIndex == candidateContext.combatIndex &&
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
                combatIndex = targetContext.combatIndex,
                fromCombatIndex = sourceContext.combatIndex,
                toCombatIndex = targetContext.combatIndex,
                isPlayerSide = !string.IsNullOrWhiteSpace(opDef.side)
                    ? string.Equals(opDef.side, "Player", StringComparison.Ordinal)
                    : targetContext.isPlayerSide
            };

            if (opCode == DuelEffectOpCode.ModifyPowerResult || opCode == DuelEffectOpCode.AddPowerModifier)
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

            if (opCode == DuelEffectOpCode.PowerMinPercent)
            {
                if (!opDef.TryGetAmount(out int amount))
                {
                    warningMessage = $"Missing amount for op '{opDef.op}'.";
                    return false;
                }

                if (amount < 0 || amount > 100)
                {
                    warningMessage = $"Invalid amount '{amount}' for op '{opDef.op}'.";
                    return false;
                }

                command.amount = amount;
            }

            if (opCode == DuelEffectOpCode.AddPowerModifier)
            {
                if (!TryParseModifierLayer(opDef.layer, out ModifierLayer layer))
                {
                    warningMessage = $"Invalid layer '{opDef.layer}' for AddPowerModifier.";
                    return false;
                }

                if (!TryParseModifierTarget(opDef.target, out DuelModifierTarget target))
                {
                    warningMessage = $"Invalid target '{opDef.target}' for AddPowerModifier.";
                    return false;
                }

                command.modifierLayer = layer;
                command.modifierTarget = target;
            }

            if (opCode == DuelEffectOpCode.ModifyTotalPower || opCode == DuelEffectOpCode.ModifyHealth)
            {
                if (!opDef.TryGetAmount(out int amount))
                {
                    warningMessage = $"Missing amount for op '{opDef.op}'.";
                    return false;
                }

                command.amount = amount;
            }

            if (opCode == DuelEffectOpCode.ModifyTotalPower)
            {
                if (targetContext.combatIndex < 0)
                {
                    warningMessage =
                        $"ModifyTotalPower requires a combat target, but target ability({targetContext.abilityId}) is not in combat.";
                    return false;
                }

                command.combatIndex = targetContext.combatIndex;
            }

            if (opCode == DuelEffectOpCode.ModifyOutgoingDamageOnWin)
            {
                if (!opDef.TryGetAmount(out int amount))
                {
                    warningMessage = $"Missing amount for op '{opDef.op}'.";
                    return false;
                }

                if (targetContext.combatIndex < 0)
                {
                    warningMessage =
                        $"ModifyOutgoingDamageOnWin requires a combat target, but target ability({targetContext.abilityId}) is not in combat.";
                    return false;
                }

                command.amount = amount;
                command.combatIndex = targetContext.combatIndex;
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
            if (string.Equals(target, "Power", StringComparison.Ordinal))
            {
                modifierTarget = DuelModifierTarget.Power;
                return true;
            }

            if (string.Equals(target, "PowerResult", StringComparison.Ordinal))
            {
                modifierTarget = DuelModifierTarget.PowerResult;
                return true;
            }

            modifierTarget = DuelModifierTarget.Power;
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
                ability.rollMinPercent = 0;
                RemoveSourcePrefixedModifiers(ability.powerModifiers, rollSourcePrefix);
                RemoveSourcePrefixedModifiers(ability.powerResultModifiers, rollSourcePrefix);
            }
        }

        static void ClearPreviousFormationTimedModifiers(DuelState state)
        {
            foreach (KeyValuePair<string, AbilityInstance> pair in state.abilitiesById)
            {
                AbilityInstance ability = pair.Value;
                if (ability == null)
                {
                    continue;
                }

                ability.EnsureInitialized();
                RemoveSourcePrefixedModifiers(ability.powerModifiers, formationSourcePrefix);
                RemoveSourcePrefixedModifiers(ability.powerResultModifiers, formationSourcePrefix);
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

        static void FinalizeDuelEndedIfNeeded(DuelState state)
        {
            if (state == null)
            {
                return;
            }

            if (state.playerHealth > 0 && state.opponentHealth > 0)
            {
                return;
            }

            state.isDuelEnded = true;
            DuelSimulator.ClearModifierLayer(state, ModifierLayer.Duel);
        }

        List<AbilityRuntimeContext> BuildAbilityContexts(DuelState state)
        {
            var contexts = new List<AbilityRuntimeContext>();
            var visitedAbilityIds = new HashSet<string>(StringComparer.Ordinal);

            if (state.loadoutAbilityIds != null)
            {
                for (int i = 0; i < state.loadoutAbilityIds.Count; i++)
                {
                    TryAddContext(
                        contexts,
                        visitedAbilityIds,
                        state,
                        state.loadoutAbilityIds[i],
                        true,
                        -1);
                }
            }

            if (state.opponentLoadoutAbilityIds != null)
            {
                for (int i = 0; i < state.opponentLoadoutAbilityIds.Count; i++)
                {
                    TryAddContext(
                        contexts,
                        visitedAbilityIds,
                        state,
                        state.opponentLoadoutAbilityIds[i],
                        false,
                        -1);
                }
            }

            if (state.combats == null)
            {
                return contexts;
            }

            for (int combatIndex = 0; combatIndex < state.combats.Count; combatIndex++)
            {
                CombatState combat = state.combats[combatIndex];
                if (combat == null)
                {
                    continue;
                }

                combat.EnsureInitialized();

                for (int i = 0; i < combat.playerAbilityIds.Count; i++)
                {
                    TryAddContext(
                        contexts,
                        visitedAbilityIds,
                        state,
                        combat.playerAbilityIds[i],
                        true,
                        combatIndex);
                }

                for (int i = 0; i < combat.opponentAbilityIds.Count; i++)
                {
                    TryAddContext(
                        contexts,
                        visitedAbilityIds,
                        state,
                        combat.opponentAbilityIds[i],
                        false,
                        combatIndex);
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
            int combatIndex)
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
                combatIndex = combatIndex
            });
        }

        struct AbilityRuntimeContext
        {
            public string abilityId;
            public AbilityInstance ability;
            public AbilityDef abilityDef;
            public bool isPlayerSide;
            public int combatIndex;
        }
    }
}
