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
                opponentHealth = 10,
                maxPlayerHealth = 10,
                maxOpponentHealth = 10
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
        public void TryResolveAllCombats_AppliesUsedAbilityCooldownAfterTurnEndTick()
        {
            GameDatabase database = CreateDatabase();

            var state = new DuelState
            {
                playerHealth = 5,
                opponentHealth = 5,
                maxPlayerHealth = 10,
                maxOpponentHealth = 10
            };
            AddCombats(state, 1);

            state.abilitiesById["p0"] = CreateAbility("ability.player", 2);
            state.abilitiesById["p0"].cooldownTurns = 2;
            state.abilitiesById["p0"].cooldownRemaining = 0;
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
        public void TryResolveAllCombats_AppliesTurnEndTickToIdleLoadoutAbilities()
        {
            GameDatabase database = CreateDatabase();

            var state = new DuelState
            {
                playerHealth = 5,
                opponentHealth = 5,
                maxPlayerHealth = 10,
                maxOpponentHealth = 10
            };
            AddCombats(state, 1);

            state.abilitiesById["p0"] = CreateAbility("ability.player", 2);
            state.abilitiesById["p0"].cooldownTurns = 2;
            state.abilitiesById["p0"].cooldownRemaining = 0;
            state.abilitiesById["p1"] = CreateAbility("ability.player", 2);
            state.abilitiesById["p1"].cooldownTurns = 2;
            state.abilitiesById["p1"].cooldownRemaining = 2;
            state.loadoutAbilityIds.Add("p1");
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
            Assert.AreEqual(1, state.abilitiesById["p1"].cooldownRemaining);
            Assert.GreaterOrEqual(result.cooldownUpdatedCount, 2);
        }

        [Test]
        public void TryResolveAllCombats_CooldownOne_IsAvailableOnNextTurn()
        {
            GameDatabase database = CreateDatabase();

            var state = new DuelState
            {
                playerHealth = 5,
                opponentHealth = 5,
                maxPlayerHealth = 10,
                maxOpponentHealth = 10
            };
            AddCombats(state, 1);

            state.abilitiesById["p0"] = CreateAbility("ability.player", 2);
            state.abilitiesById["p0"].cooldownTurns = 1;
            state.abilitiesById["p0"].cooldownRemaining = 0;
            state.abilitiesById["e0"] = CreateAbility("ability.opponent", 2);
            state.combats[0].playerAbilityIds.Add("p0");
            state.combats[0].opponentAbilityIds.Add("e0");

            var runner = new DuelPhaseRunner(state);
            var processor = new DuelTurnProcessor(database);

            AdvanceToResolve(runner);

            bool success = processor.TryResolveAllCombats(
                state,
                runner,
                out DuelCombatResolveResult _,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(0, state.abilitiesById["p0"].cooldownRemaining);
        }

        [Test]
        public void TryResolveAllCombats_TurnEndPassiveCooldownTwo_TriggersEveryOtherTurn()
        {
            GameDatabase database = CreateDatabase();
            database.abilitiesById["ability.regeneration"] = CreatePassiveRegenAbilityDef(cooldown: 2, healAmount: 1);

            var state = new DuelState
            {
                playerHealth = 5,
                opponentHealth = 5,
                maxPlayerHealth = 10,
                maxOpponentHealth = 10
            };
            AddCombats(state, 1);
            state.abilitiesById["passive0"] = new AbilityInstance
            {
                abilityDefId = "ability.regeneration",
                abilityType = AbilityType.Passive,
                cooldownTurns = 2,
                cooldownRemaining = 0,
                power = 0
            };
            state.loadoutAbilityIds.Add("passive0");

            var runner = new DuelPhaseRunner(state);
            var processor = new DuelTurnProcessor(database);
            Assert.IsTrue(runner.StartDuel());

            RunTurnToResolve(processor, state, runner, out string firstFailure);
            Assert.AreEqual(string.Empty, firstFailure);
            Assert.AreEqual(6, state.playerHealth);
            Assert.AreEqual(1, state.abilitiesById["passive0"].cooldownRemaining);

            RunTurnToResolve(processor, state, runner, out string secondFailure);
            Assert.AreEqual(string.Empty, secondFailure);
            Assert.AreEqual(6, state.playerHealth);
            Assert.AreEqual(0, state.abilitiesById["passive0"].cooldownRemaining);

            RunTurnToResolve(processor, state, runner, out string thirdFailure);
            Assert.AreEqual(string.Empty, thirdFailure);
            Assert.AreEqual(7, state.playerHealth);
            Assert.AreEqual(1, state.abilitiesById["passive0"].cooldownRemaining);
        }

        [Test]
        public void TryResolveAllCombats_ShieldUpEffect_BlocksWinnerOutgoingDamage()
        {
            GameDatabase database = CreateDatabase();

            var state = new DuelState
            {
                playerHealth = 10,
                opponentHealth = 10,
                maxPlayerHealth = 10,
                maxOpponentHealth = 10
            };
            AddCombats(state, 1);

            database.abilitiesById["ability.shield_up"] = CreateShieldUpEffectAbilityDef();
            state.abilitiesById["p0"] = CreateAbility("ability.shield_up", 5);
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
        public void TryResolveAllCombats_AfterCombatWinBonus_IncreasesAppliedDamage()
        {
            GameDatabase database = CreateDatabase();
            database.abilitiesById["ability.win_damage_plus"] = CreateWinDamagePlusEffectAbilityDef();

            var state = new DuelState
            {
                playerHealth = 10,
                opponentHealth = 10,
                maxPlayerHealth = 10,
                maxOpponentHealth = 10
            };
            AddCombats(state, 1);

            state.abilitiesById["p0"] = CreateAbility("ability.win_damage_plus", 8);
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
            Assert.AreEqual(8, state.opponentHealth);
            Assert.AreEqual(2, result.steps[0].appliedDamage);
        }

        [Test]
        public void TryResolveAllCombats_AfterCombatDefeatDestroy_RemovesAbilityInstance()
        {
            GameDatabase database = CreateDatabase();
            database.abilitiesById["ability.lose_destroy"] = CreateLoseDestroyEffectAbilityDef();

            var state = new DuelState
            {
                playerHealth = 10,
                opponentHealth = 10,
                maxPlayerHealth = 10,
                maxOpponentHealth = 10
            };
            AddCombats(state, 1);

            state.abilitiesById["p0"] = CreateAbility("ability.player", 9);
            state.abilitiesById["e0"] = CreateAbility("ability.lose_destroy", 1);
            state.combats[0].playerAbilityIds.Add("p0");
            state.combats[0].opponentAbilityIds.Add("e0");

            var runner = new DuelPhaseRunner(state);
            var processor = new DuelTurnProcessor(database);
            AdvanceToResolve(runner);

            bool success = processor.TryResolveAllCombats(
                state,
                runner,
                out DuelCombatResolveResult _,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.IsFalse(state.abilitiesById.ContainsKey("e0"));
            Assert.IsFalse(state.opponentLoadoutAbilityIds.Contains("e0"));
        }

        [Test]
        public void TryResolveAllCombats_HealthLostTiming_TriggersForLoadoutAbilityIgnoringCooldown()
        {
            GameDatabase database = CreateDatabase();
            database.abilitiesById["ability.berserk"] = CreateBerserkAbilityDef();

            var state = new DuelState
            {
                playerHealth = 10,
                opponentHealth = 10,
                maxPlayerHealth = 10,
                maxOpponentHealth = 10
            };
            AddCombats(state, 1);

            state.abilitiesById["p_berserk"] = new AbilityInstance
            {
                abilityDefId = "ability.berserk",
                abilityType = AbilityType.Attack,
                cooldownTurns = 1,
                cooldownRemaining = 1,
                power = 6
            };
            state.loadoutAbilityIds.Add("p_berserk");

            state.abilitiesById["p0"] = CreateAbility("ability.player", 1);
            state.abilitiesById["e0"] = CreateAbility("ability.opponent", 5);
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
            Assert.AreEqual(9, state.playerHealth);
            Assert.AreEqual(DuelOutcome.Defeat, result.steps[0].outcome);
            Assert.AreEqual(1, state.abilitiesById["p_berserk"].powerModifiers.Count);
            Assert.AreEqual(3, state.abilitiesById["p_berserk"].powerModifiers[0].value);
        }

        [Test]
        public void TryResolveAllCombats_HealthLostTiming_ImmediatelyAffectsUnresolvedCombatResults()
        {
            GameDatabase database = CreateDatabase();
            database.abilitiesById["ability.berserk"] = CreateBerserkAbilityDef();

            var state = new DuelState
            {
                playerHealth = 10,
                opponentHealth = 10,
                maxPlayerHealth = 10,
                maxOpponentHealth = 10
            };
            AddCombats(state, 2);

            state.abilitiesById["p0"] = CreateAbility("ability.player", 1);
            state.abilitiesById["e0"] = CreateAbility("ability.opponent", 5);
            state.combats[0].playerAbilityIds.Add("p0");
            state.combats[0].opponentAbilityIds.Add("e0");

            state.abilitiesById["p_berserk"] = CreateAbility("ability.berserk", 4);
            state.abilitiesById["p_berserk"].cooldownTurns = 1;
            state.abilitiesById["p_berserk"].cooldownRemaining = 1;
            state.abilitiesById["e1"] = CreateAbility("ability.opponent", 6);
            state.combats[1].playerAbilityIds.Add("p_berserk");
            state.combats[1].opponentAbilityIds.Add("e1");

            var runner = new DuelPhaseRunner(state);
            var processor = new DuelTurnProcessor(database);
            AdvanceToResolve(runner);

            bool success = processor.TryResolveAllCombats(
                state,
                runner,
                out DuelCombatResolveResult result,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(2, result.steps.Count);

            Assert.AreEqual(DuelOutcome.Defeat, result.steps[0].outcome);
            Assert.AreEqual(9, result.steps[0].playerHealthAfterStep);
            Assert.AreEqual(10, result.steps[0].opponentHealthAfterStep);
            Assert.IsTrue(result.steps[0].abilityPowerAfterStep.TryGetValue("p_berserk", out int step0BerserkPower));
            Assert.AreEqual(9, step0BerserkPower);

            Assert.AreEqual(7, state.abilitiesById["p_berserk"].powerResult);
            Assert.AreEqual(1, state.abilitiesById["p_berserk"].powerModifiers.Count);
            Assert.AreEqual(3, state.abilitiesById["p_berserk"].powerModifiers[0].value);

            Assert.AreEqual(DuelOutcome.Victory, result.steps[1].outcome);
            Assert.AreEqual(9, result.steps[1].playerHealthAfterStep);
            Assert.AreEqual(9, result.steps[1].opponentHealthAfterStep);
            Assert.IsTrue(result.steps[1].abilityPowerAfterStep.TryGetValue("p_berserk", out int step1BerserkPower));
            Assert.AreEqual(9, step1BerserkPower);
            Assert.AreEqual(9, state.playerHealth);
            Assert.AreEqual(9, state.opponentHealth);
        }

        [Test]
        public void TryResolveAllCombats_FailsWhenNoCombatsExist()
        {
            GameDatabase database = CreateDatabase();
            var state = new DuelState
            {
                playerHealth = 10,
                opponentHealth = 10,
                maxPlayerHealth = 10,
                maxOpponentHealth = 10
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
                opponentHealth = 5,
                maxPlayerHealth = 10,
                maxOpponentHealth = 10
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
                    cooldownTickPerTurn = 1,
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
                cooldown = 1,
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
                cooldown = 1,
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
                cooldown = 1,
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

        static AbilityDef CreatePassiveRegenAbilityDef(int cooldown, int healAmount)
        {
            return new AbilityDef
            {
                type = AbilityType.Passive.ToString(),
                buildCost = 0,
                cooldown = cooldown,
                power = 0,
                effects = new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = "TurnEnd",
                        condition = new ConditionDef
                        {
                            type = "Always"
                        },
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = "ModifyHealth",
                                side = "Player",
                                value = healAmount
                            }
                        }
                    }
                }
            };
        }

        static AbilityDef CreateWinDamagePlusEffectAbilityDef()
        {
            return new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 1,
                cooldown = 1,
                power = 10,
                effects = new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = "AfterCombat",
                        condition = new ConditionDef
                        {
                            type = "OutcomeIsVictory"
                        },
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = "ModifyOutgoingDamageOnWin",
                                scope = "Self",
                                value = 1
                            }
                        }
                    }
                }
            };
        }

        static AbilityDef CreateLoseDestroyEffectAbilityDef()
        {
            return new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 1,
                cooldown = 1,
                power = 4,
                effects = new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = "AfterCombat",
                        condition = new ConditionDef
                        {
                            type = "OutcomeIsDefeat"
                        },
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = "DestroyAbility",
                                scope = "Self"
                            }
                        }
                    }
                }
            };
        }

        static AbilityDef CreateBerserkAbilityDef()
        {
            return new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 1,
                cooldown = 1,
                power = 6,
                effects = new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = "HealthLost",
                        condition = new ConditionDef
                        {
                            type = "Always"
                        },
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = "AddPowerModifier",
                                scope = "Self",
                                target = "Power",
                                layer = "Duel",
                                mode = "Add",
                                value = 3
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
                cooldownTurns = 1,
                cooldownRemaining = 0,
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

        static void RunTurnToResolve(
            DuelTurnProcessor processor,
            DuelState state,
            DuelPhaseRunner runner,
            out string failureMessage)
        {
            failureMessage = string.Empty;
            Assert.AreEqual(DuelPhase.Reset, runner.currentPhase);
            Assert.IsTrue(runner.AdvanceToNextPhase());
            Assert.IsTrue(runner.AdvanceToNextPhase());
            Assert.IsTrue(runner.AdvanceToNextPhase());
            Assert.IsTrue(runner.AdvanceToNextPhase());

            bool success = processor.TryResolveAllCombats(
                state,
                runner,
                out DuelCombatResolveResult _,
                out failureMessage);
            Assert.IsTrue(success, failureMessage);
        }
    }
}
