using System;
using System.Collections.Generic;
using Game.Application.Battle.Effects;
using Game.Domain.Battle;
using Game.Domain.Modifiers;
using Game.Infrastructure.Data;
using UnityEngine;

namespace Game.Application.Battle
{
    public readonly struct BattleRollResult
    {
        public int rolledTroopCount { get; }
        public TroopTimedEffectRunResult timedEffectResult { get; }

        public BattleRollResult(
            int rolledTroopCount,
            TroopTimedEffectRunResult timedEffectResult)
        {
            this.rolledTroopCount = rolledTroopCount;
            this.timedEffectResult = timedEffectResult;
        }
    }

    public readonly struct BattleResolveStepResult
    {
        public int battlefieldIndex { get; }
        public BattleOutcome outcome { get; }
        public int playerTotalAttack { get; }
        public int enemyTotalAttack { get; }

        public BattleResolveStepResult(
            int battlefieldIndex,
            BattleOutcome outcome,
            int playerTotalAttack,
            int enemyTotalAttack)
        {
            this.battlefieldIndex = battlefieldIndex;
            this.outcome = outcome;
            this.playerTotalAttack = playerTotalAttack;
            this.enemyTotalAttack = enemyTotalAttack;
        }
    }

    public sealed class BattleResolveResult
    {
        public IReadOnlyList<BattleResolveStepResult> steps { get; }
        public TroopTimedEffectRunResult turnEndTimedEffectResult { get; }
        public int outcomeEffectAppliedCount { get; }
        public int outcomeEffectFailedCount { get; }
        public int cooldownUpdatedCount { get; }
        public int manaBeforeTurnEnd { get; }
        public int manaAfterTurnEnd { get; }

        public BattleResolveResult(
            IReadOnlyList<BattleResolveStepResult> steps,
            TroopTimedEffectRunResult turnEndTimedEffectResult,
            int outcomeEffectAppliedCount,
            int outcomeEffectFailedCount,
            int cooldownUpdatedCount,
            int manaBeforeTurnEnd,
            int manaAfterTurnEnd)
        {
            this.steps = steps ?? Array.Empty<BattleResolveStepResult>();
            this.turnEndTimedEffectResult = turnEndTimedEffectResult;
            this.outcomeEffectAppliedCount = outcomeEffectAppliedCount;
            this.outcomeEffectFailedCount = outcomeEffectFailedCount;
            this.cooldownUpdatedCount = cooldownUpdatedCount;
            this.manaBeforeTurnEnd = manaBeforeTurnEnd;
            this.manaAfterTurnEnd = manaAfterTurnEnd;
        }
    }

    public sealed class BattleTurnProcessor
    {
        readonly GameDatabase database;
        readonly BattleEffectResolver effectResolver;
        readonly TroopTimedEffectRunner timedEffectRunner;

        public BattleTurnProcessor(GameDatabase database, BattleEffectResolver effectResolver = null)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            this.effectResolver = effectResolver ?? new BattleEffectResolver();
            timedEffectRunner = new TroopTimedEffectRunner(this.database, this.effectResolver);
        }

        public bool TryRollAllDeployedTroops(
            BattleState state,
            BattlePhaseRunner phaseRunner,
            out BattleRollResult result,
            out string failureMessage)
        {
            result = new BattleRollResult(0, new TroopTimedEffectRunResult(0, 0, 0));
            failureMessage = string.Empty;

            if (state == null)
            {
                failureMessage = "battle state is null.";
                return false;
            }

            if (phaseRunner == null)
            {
                failureMessage = "phase runner is null.";
                return false;
            }

            state.EnsureInitialized();

            if (!phaseRunner.isStarted)
            {
                failureMessage = "battle is not started.";
                return false;
            }

            if (state.isBattleEnded)
            {
                failureMessage = "battle already ended.";
                return false;
            }

            if (phaseRunner.currentPhase == BattlePhase.PlayerDeploy)
            {
                if (!phaseRunner.AdvanceToNextPhase())
                {
                    failureMessage = $"failed to enter Roll phase ({phaseRunner.LastFailureReason}).";
                    return false;
                }
            }

            if (phaseRunner.currentPhase != BattlePhase.Roll)
            {
                failureMessage = $"current phase is {phaseRunner.currentPhase}, required phase is {BattlePhase.Roll}.";
                return false;
            }

            var deployedTroopIds = CollectDeployedTroopIds(state);
            if (deployedTroopIds.Count <= 0)
            {
                failureMessage = "no deployed troops to roll.";
                return false;
            }

            int rolledCount = 0;
            foreach (string troopId in deployedTroopIds)
            {
                if (!state.troopsById.TryGetValue(troopId, out TroopInstance troop) || troop == null)
                {
                    Debug.LogWarning($"[BattleTurnProcessor] Roll warning: troopId({troopId}) does not exist.");
                    continue;
                }

                BattleSimulator.RollTroop(troop);
                rolledCount += 1;
            }

            if (rolledCount <= 0)
            {
                failureMessage = "all deployed troops were invalid.";
                return false;
            }

            TroopTimedEffectRunResult timedResult = timedEffectRunner.ApplyForTiming(state, BattleEffectTiming.Roll);

            if (!phaseRunner.AdvanceToNextPhase())
            {
                Debug.LogWarning(
                    $"[BattleTurnProcessor] Roll warning: failed to move to next phase ({phaseRunner.LastFailureReason}).");
            }

            result = new BattleRollResult(rolledCount, timedResult);
            return true;
        }

        public bool TryResolveAllBattlefields(
            BattleState state,
            BattlePhaseRunner phaseRunner,
            out BattleResolveResult result,
            out string failureMessage)
        {
            result = new BattleResolveResult(
                Array.Empty<BattleResolveStepResult>(),
                new TroopTimedEffectRunResult(0, 0, 0),
                0,
                0,
                0,
                0,
                0);
            failureMessage = string.Empty;

            if (state == null)
            {
                failureMessage = "battle state is null.";
                return false;
            }

            if (phaseRunner == null)
            {
                failureMessage = "phase runner is null.";
                return false;
            }

            state.EnsureInitialized();

            if (!phaseRunner.isStarted)
            {
                failureMessage = "battle is not started.";
                return false;
            }

            if (state.isBattleEnded)
            {
                failureMessage = "battle already ended.";
                return false;
            }

            if (phaseRunner.currentPhase == BattlePhase.Tactics)
            {
                if (!phaseRunner.AdvanceToNextPhase())
                {
                    failureMessage = $"failed to enter Resolve phase ({phaseRunner.LastFailureReason}).";
                    return false;
                }
            }

            if (phaseRunner.currentPhase != BattlePhase.Resolve)
            {
                failureMessage = $"current phase is {phaseRunner.currentPhase}, required phase is {BattlePhase.Resolve}.";
                return false;
            }

            var steps = new List<BattleResolveStepResult>(state.battlefields.Count);
            int outcomeEffectAppliedCount = 0;
            int outcomeEffectFailedCount = 0;

            for (int battlefieldIndex = 0; battlefieldIndex < state.battlefields.Count; battlefieldIndex++)
            {
                BattlefieldState battlefield = state.battlefields[battlefieldIndex];
                if (battlefield == null)
                {
                    Debug.LogWarning($"[BattleTurnProcessor] Resolve warning: battlefields[{battlefieldIndex}] is null.");
                    continue;
                }

                battlefield.EnsureInitialized();

                int playerTotalAttack = BattleSimulator.ComputeTotalAttack(
                    battlefield,
                    state.troopsById,
                    true);
                int enemyTotalAttack = BattleSimulator.ComputeTotalAttack(
                    battlefield,
                    state.troopsById,
                    false);
                BattleOutcome outcome = BattleSimulator.ComputeOutcome(playerTotalAttack, enemyTotalAttack);

                ApplyOutcomeEffects(
                    state,
                    battlefieldIndex,
                    outcome,
                    ref outcomeEffectAppliedCount,
                    ref outcomeEffectFailedCount);

                if (state.playerMorale <= 0 || state.enemyMorale <= 0)
                {
                    state.isBattleEnded = true;
                    BattleSimulator.ClearModifierLayer(state, ModifierLayer.Battle);
                }

                steps.Add(new BattleResolveStepResult(
                    battlefieldIndex,
                    outcome,
                    playerTotalAttack,
                    enemyTotalAttack));

                if (state.isBattleEnded)
                {
                    break;
                }
            }

            if (steps.Count <= 0)
            {
                failureMessage = "no battlefields were resolved.";
                return false;
            }

            int manaBeforeTurnEnd = state.mana;
            int cooldownUpdatedCount = 0;
            TroopTimedEffectRunResult turnEndTimedEffects = new TroopTimedEffectRunResult(0, 0, 0);

            if (!state.isBattleEnded)
            {
                cooldownUpdatedCount = ApplyTurnEndMaintenance(state);
                turnEndTimedEffects = timedEffectRunner.ApplyForTiming(state, BattleEffectTiming.TurnEnd);
            }

            if (!state.isBattleEnded && !phaseRunner.AdvanceToNextPhase())
            {
                Debug.LogWarning(
                    $"[BattleTurnProcessor] Resolve warning: failed to move to next phase ({phaseRunner.LastFailureReason}).");
            }

            result = new BattleResolveResult(
                steps,
                turnEndTimedEffects,
                outcomeEffectAppliedCount,
                outcomeEffectFailedCount,
                cooldownUpdatedCount,
                manaBeforeTurnEnd,
                state.mana);
            return true;
        }

        int ApplyTurnEndMaintenance(BattleState state)
        {
            if (database.battleConfig == null)
            {
                Debug.LogWarning("[BattleTurnProcessor] TurnEnd maintenance skipped: battle_config is missing.");
                return 0;
            }

            int manaMax = Mathf.Max(0, database.battleConfig.manaMax);
            state.mana = Mathf.Clamp(state.mana + database.battleConfig.manaRegenPerTurn, 0, manaMax);

            if (state.cooldowns == null)
            {
                state.cooldowns = new Dictionary<string, int>();
                Debug.LogWarning("[BattleTurnProcessor] cooldowns was null and has been auto-initialized.");
                return 0;
            }

            int cooldownUpdatedCount = 0;
            var cooldownKeys = new List<string>(state.cooldowns.Keys);
            int cooldownTick = database.battleConfig.cooldownTickPerTurn;

            for (int i = 0; i < cooldownKeys.Count; i++)
            {
                string key = cooldownKeys[i];
                int currentValue = state.cooldowns[key];
                int updatedValue = Mathf.Max(0, currentValue + cooldownTick);
                if (updatedValue == currentValue)
                {
                    continue;
                }

                state.cooldowns[key] = updatedValue;
                cooldownUpdatedCount += 1;
            }

            return cooldownUpdatedCount;
        }

        void ApplyOutcomeEffects(
            BattleState state,
            int battlefieldIndex,
            BattleOutcome outcome,
            ref int appliedCount,
            ref int failedCount)
        {
            BattlefieldDef battlefieldDef = ResolveBattlefieldDef(state, battlefieldIndex);
            if (battlefieldDef == null || battlefieldDef.outcomeEffects == null)
            {
                return;
            }

            string outcomeKey = outcome.ToString();
            if (!battlefieldDef.outcomeEffects.TryGetValue(outcomeKey, out List<EffectBlockDef> blocks) || blocks == null)
            {
                return;
            }

            for (int blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
            {
                EffectBlockDef block = blocks[blockIndex];
                if (block == null || block.ops == null)
                {
                    continue;
                }

                for (int opIndex = 0; opIndex < block.ops.Count; opIndex++)
                {
                    EffectOpDef op = block.ops[opIndex];
                    if (!TryBuildOutcomeCommand(
                            battlefieldDef,
                            battlefieldIndex,
                            outcome,
                            blockIndex,
                            opIndex,
                            op,
                            out BattleEffectCommand command,
                            out string warningMessage))
                    {
                        failedCount += 1;
                        Debug.LogWarning($"[BattleTurnProcessor] Outcome effect warning: {warningMessage}");
                        continue;
                    }

                    BattleEffectResult applyResult = effectResolver.Apply(state, command);
                    if (applyResult.isSuccess)
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

        BattlefieldDef ResolveBattlefieldDef(BattleState state, int battlefieldIndex)
        {
            if (state.battlefields == null ||
                battlefieldIndex < 0 ||
                battlefieldIndex >= state.battlefields.Count)
            {
                return null;
            }

            BattlefieldState battlefieldState = state.battlefields[battlefieldIndex];
            if (battlefieldState == null || string.IsNullOrWhiteSpace(battlefieldState.battlefieldId))
            {
                return null;
            }

            if (database.battlefieldsById == null)
            {
                return null;
            }

            if (!database.battlefieldsById.TryGetValue(battlefieldState.battlefieldId, out BattlefieldDef battlefieldDef))
            {
                return null;
            }

            return battlefieldDef;
        }

        static bool TryBuildOutcomeCommand(
            BattlefieldDef battlefieldDef,
            int battlefieldIndex,
            BattleOutcome outcome,
            int blockIndex,
            int opIndex,
            EffectOpDef op,
            out BattleEffectCommand command,
            out string warningMessage)
        {
            command = null;
            warningMessage = string.Empty;

            if (op == null)
            {
                warningMessage = "op is null.";
                return false;
            }

            if (!Enum.TryParse(op.op, false, out BattleEffectOpCode opCode))
            {
                warningMessage = $"unsupported op '{op.op}'.";
                return false;
            }

            command = new BattleEffectCommand
            {
                opCode = opCode,
                sourceId = $"Outcome:{battlefieldDef.id}:{outcome}:{blockIndex}:{opIndex}",
                battlefieldIndex = battlefieldIndex
            };

            switch (opCode)
            {
                case BattleEffectOpCode.ModifyMorale:
                case BattleEffectOpCode.ModifyTotalAttack:
                    if (!TryResolveSide(op.side, out bool isPlayerSide))
                    {
                        warningMessage = $"invalid side '{op.side}' for op '{op.op}'.";
                        return false;
                    }

                    if (!op.TryGetAmount(out int amount))
                    {
                        warningMessage = $"missing amount for op '{op.op}'.";
                        return false;
                    }

                    command.isPlayerSide = isPlayerSide;
                    command.amount = amount;
                    return true;
                default:
                    warningMessage = $"op '{op.op}' is not allowed in battlefield outcomeEffects.";
                    return false;
            }
        }

        static bool TryResolveSide(string side, out bool isPlayerSide)
        {
            if (string.Equals(side, "Player", StringComparison.Ordinal))
            {
                isPlayerSide = true;
                return true;
            }

            if (string.Equals(side, "Enemy", StringComparison.Ordinal))
            {
                isPlayerSide = false;
                return true;
            }

            isPlayerSide = true;
            return false;
        }

        static HashSet<string> CollectDeployedTroopIds(BattleState state)
        {
            var deployedTroopIds = new HashSet<string>(StringComparer.Ordinal);

            if (state.battlefields == null)
            {
                return deployedTroopIds;
            }

            for (int battlefieldIndex = 0; battlefieldIndex < state.battlefields.Count; battlefieldIndex++)
            {
                BattlefieldState battlefield = state.battlefields[battlefieldIndex];
                if (battlefield == null)
                {
                    continue;
                }

                battlefield.EnsureInitialized();
                CollectTroopIds(deployedTroopIds, battlefield.playerTroopIds, $"playerTroopIds[{battlefieldIndex}]");
                CollectTroopIds(deployedTroopIds, battlefield.enemyTroopIds, $"enemyTroopIds[{battlefieldIndex}]");
            }

            return deployedTroopIds;
        }

        static void CollectTroopIds(HashSet<string> buffer, List<string> troopIds, string sourceLabel)
        {
            if (troopIds == null)
            {
                Debug.LogWarning($"[BattleTurnProcessor] Roll warning: {sourceLabel} is null.");
                return;
            }

            for (int i = 0; i < troopIds.Count; i++)
            {
                string troopId = troopIds[i];
                if (string.IsNullOrWhiteSpace(troopId))
                {
                    Debug.LogWarning($"[BattleTurnProcessor] Roll warning: empty troopId at {sourceLabel}[{i}].");
                    continue;
                }

                buffer.Add(troopId);
            }
        }
    }
}
