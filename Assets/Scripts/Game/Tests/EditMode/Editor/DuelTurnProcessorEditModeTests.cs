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
        public void TryResolveAllClashes_AppliesDifferenceDamageAndReturnsPlayerAbilitiesToLoadout()
        {
            GameDatabase database = CreateDatabase();

            var state = new DuelState
            {
                playerHealth = 10,
                opponentHealth = 10
            };
            AddClashes(state, 1);

            state.abilitiesById["p0"] = CreateAbility("ability.player", 4);
            state.abilitiesById["e0"] = CreateAbility("ability.opponent", 1);
            state.clashes[0].playerAbilityIds.Add("p0");
            state.clashes[0].opponentAbilityIds.Add("e0");

            var runner = new DuelPhaseRunner(state);
            var processor = new DuelTurnProcessor(database);

            AdvanceToResolve(runner);

            bool success = processor.TryResolveAllClashes(
                state,
                runner,
                out DuelClashResolveResult result,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(7, state.opponentHealth);
            Assert.AreEqual(1, result.steps.Count);
            Assert.AreEqual(DuelOutcome.Victory, result.steps[0].outcome);
            Assert.AreEqual(3, result.steps[0].appliedDamage);
            Assert.AreEqual(DuelPhase.Reset, runner.currentPhase);
            Assert.AreEqual(1, state.turnIndex);
            Assert.IsTrue(state.loadoutAbilityIds.Contains("p0"));
            Assert.IsFalse(state.clashes[0].playerAbilityIds.Contains("p0"));
        }

        [Test]
        public void TryResolveAllClashes_AppliesTurnEndCooldown()
        {
            GameDatabase database = CreateDatabase();

            var state = new DuelState
            {
                playerHealth = 5,
                opponentHealth = 5
            };
            AddClashes(state, 1);

            state.abilitiesById["p0"] = CreateAbility("ability.player", 2);
            state.abilitiesById["p0"].cooldownTurns = 2;
            state.abilitiesById["p0"].cooldownRemaining = 2;
            state.abilitiesById["e0"] = CreateAbility("ability.opponent", 2);
            state.clashes[0].playerAbilityIds.Add("p0");
            state.clashes[0].opponentAbilityIds.Add("e0");

            var runner = new DuelPhaseRunner(state);
            var processor = new DuelTurnProcessor(database);

            AdvanceToResolve(runner);

            bool success = processor.TryResolveAllClashes(
                state,
                runner,
                out DuelClashResolveResult result,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(1, state.abilitiesById["p0"].cooldownRemaining);
            Assert.AreEqual(1, result.cooldownUpdatedCount);
        }

        [Test]
        public void TryResolveAllClashes_ShieldUpTag_BlocksWinnerOutgoingDamage()
        {
            GameDatabase database = CreateDatabase();

            var state = new DuelState
            {
                playerHealth = 10,
                opponentHealth = 10
            };
            AddClashes(state, 1);

            state.abilitiesById["p0"] = CreateAbility(
                "ability.shield.up",
                5,
                "ability.effect.no.outgoing.damage.on.win");
            state.abilitiesById["e0"] = CreateAbility("ability.opponent", 1);
            state.clashes[0].playerAbilityIds.Add("p0");
            state.clashes[0].opponentAbilityIds.Add("e0");

            var runner = new DuelPhaseRunner(state);
            var processor = new DuelTurnProcessor(database);

            AdvanceToResolve(runner);

            bool success = processor.TryResolveAllClashes(
                state,
                runner,
                out DuelClashResolveResult result,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(10, state.opponentHealth);
            Assert.AreEqual(0, result.steps[0].appliedDamage);
            Assert.AreEqual(DuelOutcome.Victory, result.steps[0].outcome);
        }

        [Test]
        public void TryResolveAllClashes_AdvancesPatternAndRebuildsClashes()
        {
            GameDatabase database = CreateDatabase();
            database.encountersById["encounter.1"] = new EncounterDef
            {
                enemy = new EncounterEnemyDef
                {
                    id = "enemy.1",
                    health = 10,
                    startPatternId = "pattern.a",
                    patterns = new List<EncounterEnemyPatternDef>
                    {
                        new EncounterEnemyPatternDef
                        {
                            patternId = "pattern.a",
                            clashes = new List<EncounterEnemyClashDef>
                            {
                                new EncounterEnemyClashDef
                                {
                                    clashId = "clash.a",
                                    abilityLoadout = new List<SummonAbilityRefDef>
                                    {
                                        new SummonAbilityRefDef
                                        {
                                            abilityId = "ability.test",
                                            count = 1
                                        }
                                    }
                                }
                            },
                            nextPatterns = new List<EncounterEnemyPatternTransitionDef>
                            {
                                new EncounterEnemyPatternTransitionDef
                                {
                                    patternId = "pattern.b",
                                    probability = 1.0
                                }
                            }
                        },
                        new EncounterEnemyPatternDef
                        {
                            patternId = "pattern.b",
                            clashes = new List<EncounterEnemyClashDef>
                            {
                                new EncounterEnemyClashDef
                                {
                                    clashId = "clash.b",
                                    maxPlayerAssignments = 1,
                                    abilityLoadout = new List<SummonAbilityRefDef>
                                    {
                                        new SummonAbilityRefDef
                                        {
                                            abilityId = "ability.test",
                                            count = 2
                                        }
                                    }
                                }
                            },
                            nextPatterns = new List<EncounterEnemyPatternTransitionDef>
                            {
                                new EncounterEnemyPatternTransitionDef
                                {
                                    patternId = "pattern.b",
                                    probability = 1.0
                                }
                            }
                        }
                    }
                }
            };

            var state = new DuelState
            {
                encounterId = "encounter.1",
                currentPatternId = "pattern.a",
                playerHealth = 10,
                opponentHealth = 10
            };
            AddClashes(state, 1);

            state.abilitiesById["p0"] = CreateAbility("ability.player", 1);
            state.abilitiesById["e0"] = CreateAbility("ability.opponent", 1);
            state.abilitiesById["enemy_0"] = CreateAbility("ability.test", 1);
            state.clashes[0].playerAbilityIds.Add("p0");
            state.clashes[0].opponentAbilityIds.Add("e0");
            state.clashes[0].opponentAbilityIds.Add("enemy_0");

            state.opponentClashLoadoutEntries.Add(new OpponentClashLoadoutEntry
            {
                clashIndex = 0,
                abilityDefId = "ability.test",
                count = 1
            });

            var runner = new DuelPhaseRunner(state);
            var processor = new DuelTurnProcessor(database, random: new System.Random(0));

            AdvanceToResolve(runner);

            bool success = processor.TryResolveAllClashes(
                state,
                runner,
                out DuelClashResolveResult result,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.IsTrue(result.patternAdvanced);
            Assert.AreEqual("pattern.b", state.currentPatternId);
            Assert.AreEqual(1, state.clashes.Count);
            Assert.AreEqual("clash.b", state.clashes[0].clashId);
            Assert.AreEqual(1, state.opponentClashLoadoutEntries.Count);
            Assert.AreEqual(2, state.opponentClashLoadoutEntries[0].count);
            Assert.IsFalse(state.abilitiesById.ContainsKey("enemy_0"));
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
                abilityType = AbilityType.Attack,
                power = 1
            };
            state.clashes[0].playerAbilityIds.Add("p0");

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

        static AbilityInstance CreateAbility(
            string abilityDefId,
            int powerResult,
            params string[] tags)
        {
            var ability = new AbilityInstance
            {
                abilityDefId = abilityDefId,
                abilityType = AbilityType.Attack,
                power = 6,
                baseRoll = powerResult,
                powerResult = powerResult
            };

            if (tags != null && tags.Length > 0)
            {
                ability.tags.AddRange(tags);
            }

            return ability;
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


