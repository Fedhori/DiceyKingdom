using System.Collections.Generic;
using Game.Application.Duel;
using Game.Domain.Duel;
using Game.Infrastructure.Data;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class DuelTurnProcessorEditModeTests
    {
        [Test]
        public void TryClashResolveAllClashes_AppliesOutcomeEffectsFromData()
        {
            GameDatabase database = CreateDatabase();
            database.clashesById["clash.0"] = CreateClashDefWithHealthDamage();

            var state = new DuelState
            {
                playerHealth = 5,
                opponentHealth = 5,
                focus = 3
            };

            state.clashes[0].clashId = "clash.0";
            state.actionsById["p0"] = CreateAction("action.player", 3);
            state.actionsById["e0"] = CreateAction("action.opponent", 2);
            state.clashes[0].playerActionIds.Add("p0");
            state.clashes[0].opponentActionIds.Add("e0");

            var runner = new DuelPhaseRunner(state);
            var processor = new DuelTurnProcessor(database);

            AdvanceToClashResolve(runner);

            bool success = processor.TryClashResolveAllClashes(
                state,
                runner,
                out DuelClashResolveResult result,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(4, state.opponentHealth);
            Assert.AreEqual(1, result.outcomeEffectAppliedCount);
            Assert.AreEqual(0, result.outcomeEffectFailedCount);
            Assert.AreEqual(DuelPhase.Reset, runner.currentPhase);
            Assert.AreEqual(1, state.turnIndex);
        }

        [Test]
        public void TryClashResolveAllClashes_AppliesTurnEndFocusAndCooldown()
        {
            GameDatabase database = CreateDatabase();
            database.clashesById["clash.0"] = CreateClashDefWithDrawOnly();

            var state = new DuelState
            {
                playerHealth = 5,
                opponentHealth = 5,
                focus = 1
            };
            state.cooldowns["skill_a"] = 2;

            state.clashes[0].clashId = "clash.0";
            state.actionsById["p0"] = CreateAction("action.player", 2);
            state.actionsById["e0"] = CreateAction("action.opponent", 2);
            state.clashes[0].playerActionIds.Add("p0");
            state.clashes[0].opponentActionIds.Add("e0");

            var runner = new DuelPhaseRunner(state);
            var processor = new DuelTurnProcessor(database);

            AdvanceToClashResolve(runner);

            bool success = processor.TryClashResolveAllClashes(
                state,
                runner,
                out DuelClashResolveResult result,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(3, state.focus);
            Assert.AreEqual(1, state.cooldowns["skill_a"]);
            Assert.AreEqual(1, result.cooldownUpdatedCount);
            Assert.AreEqual(1, result.focusBeforeTurnEnd);
            Assert.AreEqual(3, result.focusAfterTurnEnd);
        }

        [Test]
        public void TryRollAllDeployedActions_AppliesRollTimedEffects()
        {
            GameDatabase database = CreateDatabase();
            database.actionsById["action.player"] = CreateRollEffectActionDef();

            var state = new DuelState
            {
                playerHealth = 5,
                opponentHealth = 5
            };

            state.actionsById["p0"] = new ActionInstance
            {
                actionDefId = "action.player",
                attack = 1
            };
            state.clashes[0].playerActionIds.Add("p0");

            var runner = new DuelPhaseRunner(state);
            var processor = new DuelTurnProcessor(database);

            Assert.IsTrue(runner.StartDuel());
            Assert.IsTrue(runner.AdvanceToNextPhase()); // OpponentSetup
            Assert.IsTrue(runner.AdvanceToNextPhase()); // PlayerSetup

            bool success = processor.TryRollAllDeployedActions(
                state,
                runner,
                out DuelRollResult result,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(1, result.rolledActionCount);
            Assert.AreEqual(1, result.timedEffectResult.appliedCount);
            Assert.AreEqual(2, state.actionsById["p0"].attackResult);
            Assert.AreEqual(DuelPhase.Skill, runner.currentPhase);
        }

        static void AdvanceToClashResolve(DuelPhaseRunner runner)
        {
            Assert.IsTrue(runner.StartDuel());
            Assert.IsTrue(runner.AdvanceToNextPhase()); // OpponentSetup
            Assert.IsTrue(runner.AdvanceToNextPhase()); // PlayerSetup
            Assert.IsTrue(runner.AdvanceToNextPhase()); // Roll
            Assert.IsTrue(runner.AdvanceToNextPhase()); // Skill
            Assert.IsTrue(runner.AdvanceToNextPhase()); // ClashResolve
        }

        static GameDatabase CreateDatabase()
        {
            var database = new GameDatabase
            {
                duelConfig = new DuelConfigDef
                {
                    clashCount = 3,
                    focusMax = 5,
                    focusRegenPerTurn = 2,
                    cooldownTickPerTurn = -1,
                    attackResultMin = 1,
                    greatVictoryMultiplier = 2,
                    p0Rules = new P0RulesDef
                    {
                        disallowBaseAttackMutation = true,
                        defaultSlotLimit = null
                    }
                },
                runConfig = new RunConfigDef(),
                playerStart = new PlayerStartDef()
            };

            return database;
        }

        static ClashDef CreateClashDefWithHealthDamage()
        {
            return new ClashDef
            {
                outcomeEffects = new Dictionary<string, List<EffectBlockDef>>
                {
                    ["Victory"] = new List<EffectBlockDef>
                    {
                        new EffectBlockDef
                        {
                            ops = new List<EffectOpDef>
                            {
                                new EffectOpDef
                                {
                                    op = "ModifyHealth",
                                    side = "Opponent",
                                    delta = -1
                                }
                            }
                        }
                    }
                }
            };
        }

        static ClashDef CreateClashDefWithDrawOnly()
        {
            return new ClashDef
            {
                outcomeEffects = new Dictionary<string, List<EffectBlockDef>>
                {
                    ["Draw"] = new List<EffectBlockDef>()
                }
            };
        }

        static ActionDef CreateRollEffectActionDef()
        {
            return new ActionDef
            {
                attack = 1,
                effects = new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = "Roll",
                        condition = new ConditionDef
                        {
                            type = "Always"
                        },
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = "ModifyAttackResult",
                                scope = "Self",
                                mode = "Add",
                                value = 1
                            }
                        }
                    }
                }
            };
        }

        static ActionInstance CreateAction(string actionDefId, int attackResult)
        {
            return new ActionInstance
            {
                actionDefId = actionDefId,
                attack = 6,
                baseRoll = attackResult,
                attackResult = attackResult
            };
        }
    }
}
