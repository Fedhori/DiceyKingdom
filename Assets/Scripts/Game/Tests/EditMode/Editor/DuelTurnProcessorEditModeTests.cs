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
        public void TryClashResolveAllClashes_AppliesClashDamageFromData()
        {
            GameDatabase database = CreateDatabase();
            database.clashesById["clash.0"] = CreateClashDefWithHealthDamage();

            var state = new DuelState
            {
                playerHealth = 5,
                opponentHealth = 5
            };
            AddClashes(state, 3);

            state.clashes[0].clashId = "clash.0";
            state.clashes[1].clashId = "clash.0";
            state.clashes[2].clashId = "clash.0";
            state.abilitiesById["p0"] = CreateAbility("ability.player", 3);
            state.abilitiesById["e0"] = CreateAbility("ability.opponent", 2);
            state.clashes[0].playerAbilityIds.Add("p0");
            state.clashes[0].opponentAbilityIds.Add("e0");

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
            Assert.AreEqual(3, result.outcomeEffectAppliedCount);
            Assert.AreEqual(0, result.outcomeEffectFailedCount);
            Assert.AreEqual(DuelPhase.Reset, runner.currentPhase);
            Assert.AreEqual(1, state.turnIndex);
        }

        [Test]
        public void TryClashResolveAllClashes_AppliesTurnEndCooldown()
        {
            GameDatabase database = CreateDatabase();
            database.clashesById["clash.0"] = CreateClashDefWithDrawOnly();

            var state = new DuelState
            {
                playerHealth = 5,
                opponentHealth = 5
            };
            AddClashes(state, 3);

            state.clashes[0].clashId = "clash.0";
            state.clashes[1].clashId = "clash.0";
            state.clashes[2].clashId = "clash.0";
            state.abilitiesById["p0"] = CreateAbility("ability.player", 2);
            state.abilitiesById["p0"].cooldownTurns = 2;
            state.abilitiesById["p0"].cooldownRemaining = 2;
            state.abilitiesById["e0"] = CreateAbility("ability.opponent", 2);
            state.clashes[0].playerAbilityIds.Add("p0");
            state.clashes[0].opponentAbilityIds.Add("e0");

            var runner = new DuelPhaseRunner(state);
            var processor = new DuelTurnProcessor(database);

            AdvanceToClashResolve(runner);

            bool success = processor.TryClashResolveAllClashes(
                state,
                runner,
                out DuelClashResolveResult result,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(1, state.abilitiesById["p0"].cooldownRemaining);
            Assert.AreEqual(1, result.cooldownUpdatedCount);
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
            AddClashes(state, 1);

            state.abilitiesById["p0"] = new AbilityInstance
            {
                abilityDefId = "ability.player",
                attack = 1
            };
            state.clashes[0].playerAbilityIds.Add("p0");

            var runner = new DuelPhaseRunner(state);
            var processor = new DuelTurnProcessor(database);

            Assert.IsTrue(runner.StartDuel());
            Assert.IsTrue(runner.AdvanceToNextPhase()); // OpponentSetup
            Assert.IsTrue(runner.AdvanceToNextPhase()); // PlayerSetup

            bool success = processor.TryRollAllDeployedAbilities(
                state,
                runner,
                out DuelRollResult result,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(1, result.rolledAbilityCount);
            Assert.AreEqual(1, result.timedEffectResult.appliedCount);
            Assert.AreEqual(2, state.abilitiesById["p0"].attackResult);
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
                    cooldownTickPerTurn = -1,
                    attackResultMin = 1,
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
                damage = 1
            };
        }

        static ClashDef CreateClashDefWithDrawOnly()
        {
            return new ClashDef
            {
                damage = 1
            };
        }

        static AbilityDef CreateRollEffectAbilityDef()
        {
            return new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 0,
                cooldown = 0,
                damage = 1,
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

        static AbilityInstance CreateAbility(string abilityDefId, int attackResult)
        {
            return new AbilityInstance
            {
                abilityDefId = abilityDefId,
                attack = 6,
                baseRoll = attackResult,
                attackResult = attackResult
            };
        }

        static void AddClashes(DuelState state, int count)
        {
            state.clashes.Clear();
            for (int i = 0; i < count; i++)
            {
                state.clashes.Add(new ClashState());
            }
        }
    }
}


