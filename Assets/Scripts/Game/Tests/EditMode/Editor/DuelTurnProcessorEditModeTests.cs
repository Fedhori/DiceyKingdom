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
        public void TryResolveAllCombats_AppliesFixedOneDamageAndReturnsPlayerAbilitiesToLoadout()
        {
            GameDatabase database = CreateDatabase();

            var state = new DuelState
            {
                playerHealth = 10,
                opponentHealth = 10
            };
            AddCombats(state, 1);

            state.abilitiesById["p0"] = CreateAbility("ability.player", 4);
            state.abilitiesById["e0"] = CreateAbility("ability.opponent", 1);
            state.combats[0].playerAbilityIds.Add("p0");
            state.combats[0].opponentAbilityIds.Add("e0");

            var runner = new DuelPhaseRunner(state);
            var processor = new DuelTurnProcessor(database);

            AdvanceToResolve(runner);

            bool success = processor.TryResolveAllCombats(
                state,
                runner,
                out DuelCombatResolveResult result,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(9, state.opponentHealth);
            Assert.AreEqual(1, result.steps.Count);
            Assert.AreEqual(DuelOutcome.Victory, result.steps[0].outcome);
            Assert.AreEqual(1, result.steps[0].appliedDamage);
            Assert.AreEqual(DuelPhase.Reset, runner.currentPhase);
            Assert.AreEqual(1, state.turnIndex);
            Assert.IsTrue(state.loadoutAbilityIds.Contains("p0"));
            Assert.IsFalse(state.combats[0].playerAbilityIds.Contains("p0"));
        }

        [Test]
        public void TryResolveAllCombats_AppliesTurnEndCooldown()
        {
            GameDatabase database = CreateDatabase();

            var state = new DuelState
            {
                playerHealth = 5,
                opponentHealth = 5
            };
            AddCombats(state, 1);

            state.abilitiesById["p0"] = CreateAbility("ability.player", 2);
            state.abilitiesById["p0"].cooldownTurns = 2;
            state.abilitiesById["p0"].cooldownRemaining = 2;
            state.abilitiesById["e0"] = CreateAbility("ability.opponent", 2);
            state.combats[0].playerAbilityIds.Add("p0");
            state.combats[0].opponentAbilityIds.Add("e0");

            var runner = new DuelPhaseRunner(state);
            var processor = new DuelTurnProcessor(database);

            AdvanceToResolve(runner);

            bool success = processor.TryResolveAllCombats(
                state,
                runner,
                out DuelCombatResolveResult result,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(1, state.abilitiesById["p0"].cooldownRemaining);
            Assert.AreEqual(1, result.cooldownUpdatedCount);
        }

        [Test]
        public void TryResolveAllCombats_ShieldUpEffect_BlocksWinnerOutgoingDamage()
        {
            GameDatabase database = CreateDatabase();

            var state = new DuelState
            {
                playerHealth = 10,
                opponentHealth = 10
            };
            AddCombats(state, 1);

            database.abilitiesById["ability.shield.up"] = CreateShieldUpEffectAbilityDef();
            state.abilitiesById["p0"] = CreateAbility("ability.shield.up", 5);
            state.abilitiesById["e0"] = CreateAbility("ability.opponent", 1);
            state.combats[0].playerAbilityIds.Add("p0");
            state.combats[0].opponentAbilityIds.Add("e0");

            var runner = new DuelPhaseRunner(state);
            var processor = new DuelTurnProcessor(database);

            AdvanceToResolve(runner);

            bool success = processor.TryResolveAllCombats(
                state,
                runner,
                out DuelCombatResolveResult result,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(10, state.opponentHealth);
            Assert.AreEqual(0, result.steps[0].appliedDamage);
            Assert.AreEqual(DuelOutcome.Victory, result.steps[0].outcome);
        }

        [Test]
        public void TryResolveAllCombats_FailsWhenNoCombatsExist()
        {
            GameDatabase database = CreateDatabase();
            var state = new DuelState
            {
                playerHealth = 10,
                opponentHealth = 10
            };

            var runner = new DuelPhaseRunner(state);
            var processor = new DuelTurnProcessor(database);

            AdvanceToResolve(runner);

            bool success = processor.TryResolveAllCombats(
                state,
                runner,
                out DuelCombatResolveResult _,
                out string failureMessage);

            Assert.IsFalse(success);
            Assert.AreEqual("no combats were resolved.", failureMessage);
        }

        [Test]
        public void TryRollAllDeployedAbilities_AppliesRollTimedEffects()
        {
            GameDatabase database = CreateDatabase();
            database.abilitiesById["ability.player"] = CreateRollEffectAbilityDef();

            var state = new DuelState
            {
                playerHealth = 5,
                opponentHealth = 5
            };
            AddCombats(state, 1);

            state.abilitiesById["p0"] = new AbilityInstance
            {
                abilityDefId = "ability.player",
                abilityType = AbilityType.Attack,
                power = 1
            };
            state.combats[0].playerAbilityIds.Add("p0");

            var runner = new DuelPhaseRunner(state);
            var processor = new DuelTurnProcessor(database);

            Assert.IsTrue(runner.StartDuel());
            Assert.IsTrue(runner.AdvanceToNextPhase());
            Assert.IsTrue(runner.AdvanceToNextPhase());

            bool success = processor.TryRollAllDeployedAbilities(
                state,
                runner,
                out DuelRollResult result,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(1, result.rolledAbilityCount);
            Assert.AreEqual(1, result.timedEffectResult.appliedCount);
            Assert.AreEqual(2, state.abilitiesById["p0"].powerResult);
            Assert.AreEqual(DuelPhase.Resolve, runner.currentPhase);
        }

        static void AdvanceToResolve(DuelPhaseRunner runner)
        {
            Assert.IsTrue(runner.StartDuel());
            Assert.IsTrue(runner.AdvanceToNextPhase());
            Assert.IsTrue(runner.AdvanceToNextPhase());
            Assert.IsTrue(runner.AdvanceToNextPhase());
            Assert.IsTrue(runner.AdvanceToNextPhase());
        }

        static GameDatabase CreateDatabase()
        {
            var database = new GameDatabase
            {
                duelConfig = new DuelConfigDef
                {
                    cooldownTickPerTurn = -1,
                    powerResultMin = 1,
                    p0Rules = new P0RulesDef
                    {
                        disallowBasePowerMutation = true,
                        defaultSlotLimit = null
                    }
                },
                runConfig = new RunConfigDef(),
                playerStart = new PlayerStartDef()
            };

            database.abilitiesById["ability.test"] = new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                power = 1,
                buildCost = 0,
                cooldown = 0,
                effects = new List<TimedEffectDef>()
            };

            return database;
        }

        static AbilityDef CreateRollEffectAbilityDef()
        {
            return new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 0,
                cooldown = 0,
                power = 1,
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
                                op = "ModifyPowerResult",
                                scope = "Self",
                                mode = "Add",
                                value = 1
                            }
                        }
                    }
                }
            };
        }

        static AbilityDef CreateShieldUpEffectAbilityDef()
        {
            return new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 1,
                cooldown = 0,
                power = 10,
                effects = new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = "Resolve",
                        condition = new ConditionDef
                        {
                            type = "Always"
                        },
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = "PreventOutgoingDamageOnWin",
                                scope = "Self"
                            }
                        }
                    }
                }
            };
        }

        static AbilityInstance CreateAbility(
            string abilityDefId,
            int powerResult)
        {
            return new AbilityInstance
            {
                abilityDefId = abilityDefId,
                abilityType = AbilityType.Attack,
                power = 6,
                baseRoll = powerResult,
                powerResult = powerResult
            };
        }

        static void AddCombats(DuelState state, int count)
        {
            state.combats.Clear();
            for (int i = 0; i < count; i++)
            {
                state.combats.Add(new CombatState());
            }
        }
    }
}
