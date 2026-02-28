using System.Collections.Generic;
using System.Linq;
using Game.Application.Duel;
using Game.Domain.Duel;
using Game.Infrastructure.Data;
using Game.Infrastructure.Data.Effects;
using Game.Presentation.Duel;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class DuelSessionRunnerEditModeTests
    {
        [Test]
        public void TryInitialize_WithAdvanceToPlayerSetup_EntersPlayerSetupAndDeploysOpponent()
        {
            GameDatabase database = CreateDatabase(startingHonor: 1);
            DuelUiQueryService queryService = CreateQueryService(database);
            var sessionRunner = new DuelSessionRunner();

            bool success = sessionRunner.TryInitialize(
                queryService,
                "enemy.test",
                advanceToPlayerSetup: true,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.NotNull(sessionRunner.DuelState);
            Assert.NotNull(sessionRunner.PhaseRunner);
            Assert.AreEqual(DuelPhase.PlayerSetup, sessionRunner.PhaseRunner.currentPhase);

            int deployedCount = sessionRunner.DuelState.combats.Sum(combat =>
            {
                if (combat == null || combat.opponentAbilityIds == null)
                {
                    return 0;
                }

                return combat.opponentAbilityIds.Count;
            });

            Assert.GreaterOrEqual(deployedCount, 1);
        }

        [Test]
        public void TryEnsureReadyForCombatStart_FromReset_AutoAdvancesToPlayerSetup()
        {
            GameDatabase database = CreateDatabase(startingHonor: 1);
            DuelUiQueryService queryService = CreateQueryService(database);
            var sessionRunner = new DuelSessionRunner();

            Assert.IsTrue(sessionRunner.TryInitialize(
                queryService,
                "enemy.test",
                advanceToPlayerSetup: false,
                out string initializeFailure),
                initializeFailure);
            Assert.AreEqual(DuelPhase.Reset, sessionRunner.PhaseRunner.currentPhase);

            bool success = sessionRunner.TryEnsureReadyForCombatStart(out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(DuelPhase.PlayerSetup, sessionRunner.PhaseRunner.currentPhase);
        }

        [Test]
        public void TryEnterOpponentSetup_FailsWhenCurrentPhaseIsNotReset()
        {
            GameDatabase database = CreateDatabase(startingHonor: 1);
            DuelUiQueryService queryService = CreateQueryService(database);
            var sessionRunner = new DuelSessionRunner();

            Assert.IsTrue(sessionRunner.TryInitialize(
                queryService,
                "enemy.test",
                advanceToPlayerSetup: true,
                out string initializeFailure),
                initializeFailure);
            Assert.AreEqual(DuelPhase.PlayerSetup, sessionRunner.PhaseRunner.currentPhase);

            bool success = sessionRunner.TryEnterOpponentSetup(
                out OpponentSetupBuildResult _,
                out string failureMessage);

            Assert.IsFalse(success);
            StringAssert.Contains("required phase is Reset", failureMessage);
        }

        [Test]
        public void TryPrepareOpponentSetupForCurrentTurn_BuildsPlanWithoutImmediateMutation()
        {
            GameDatabase database = CreateDatabase(startingHonor: 1);
            DuelUiQueryService queryService = CreateQueryService(database);
            var sessionRunner = new DuelSessionRunner();

            Assert.IsTrue(sessionRunner.TryInitialize(
                queryService,
                "enemy.test",
                advanceToPlayerSetup: false,
                out string initializeFailure),
                initializeFailure);
            Assert.AreEqual(DuelPhase.Reset, sessionRunner.PhaseRunner.currentPhase);

            bool prepared = sessionRunner.TryPrepareOpponentSetupForCurrentTurn(
                out OpponentSetupBuildResult plan,
                out string prepareFailure);

            Assert.IsTrue(prepared, prepareFailure);
            Assert.AreEqual(DuelPhase.OpponentSetup, sessionRunner.PhaseRunner.currentPhase);
            Assert.GreaterOrEqual(plan.steps.Count, 1);
            Assert.IsTrue(sessionRunner.DuelState.opponentLoadoutAbilityIds.Count >= 1);

            bool applied = sessionRunner.TryApplyOpponentDeployStep(plan.steps[0], out string applyFailure);
            Assert.IsTrue(applied, applyFailure);

            int deployedCount = sessionRunner.DuelState.combats.Sum(combat =>
            {
                if (combat == null || combat.opponentAbilityIds == null)
                {
                    return 0;
                }

                return combat.opponentAbilityIds.Count;
            });

            Assert.AreEqual(1, deployedCount);
        }

        [Test]
        public void TrySurrender_InPlayerSetupWithHonor_SucceedsAndConsumesHonor()
        {
            GameDatabase database = CreateDatabase(startingHonor: 1);
            DuelUiQueryService queryService = CreateQueryService(database);
            var sessionRunner = new DuelSessionRunner();

            Assert.IsTrue(sessionRunner.TryInitialize(
                queryService,
                "enemy.test",
                advanceToPlayerSetup: true,
                out string initializeFailure),
                initializeFailure);

            bool success = sessionRunner.TrySurrender(out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.IsTrue(sessionRunner.DuelState.isDuelEnded);
            Assert.AreEqual(0, sessionRunner.DuelState.honor);
        }

        [Test]
        public void TryInitialize_AppliesDuelStartTimedEffects()
        {
            GameDatabase database = CreateDatabase(startingHonor: 1);
            database.abilitiesById["ability.passive_duelstart_heal"] = new AbilityDef
            {
                type = AbilityType.Passive.ToString(),
                buildCost = 0,
                cooldown = 0,
                power = 0,
                isPlayerObtainable = true,
                effects = new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = nameof(DuelEffectTiming.DuelStart),
                        condition = new ConditionDef
                        {
                            type = "Always"
                        },
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = nameof(DuelEffectOpCode.ModifyHealth),
                                side = "Player",
                                value = 1
                            }
                        }
                    }
                }
            };
            database.playerStart.startingLoadoutAbilityIds.Add("ability.passive_duelstart_heal");

            DuelUiQueryService queryService = CreateQueryService(database);
            var sessionRunner = new DuelSessionRunner();
            bool success = sessionRunner.TryInitialize(
                queryService,
                "enemy.test",
                advanceToPlayerSetup: true,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(10, sessionRunner.DuelState.playerHealth);
        }

        [Test]
        public void TryInitialize_AppliesDeployTimedEffectsForAutoDeployedOpponent()
        {
            GameDatabase database = CreateDatabase(startingHonor: 1);
            database.abilitiesById["ability.enemy"] = new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 0,
                cooldown = 1,
                power = 2,
                effects = new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = nameof(DuelEffectTiming.Deploy),
                        condition = new ConditionDef
                        {
                            type = "Always"
                        },
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = nameof(DuelEffectOpCode.ModifyTotalPower),
                                scope = "Self",
                                side = "Opponent",
                                value = 1
                            }
                        }
                    }
                }
            };

            DuelUiQueryService queryService = CreateQueryService(database);
            var sessionRunner = new DuelSessionRunner();
            bool success = sessionRunner.TryInitialize(
                queryService,
                "enemy.test",
                advanceToPlayerSetup: true,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);

            int totalBonus = sessionRunner.DuelState.combats.Sum(combat =>
            {
                if (combat == null)
                {
                    return 0;
                }

                return combat.totalPowerBonusOpponent;
            });

            Assert.AreEqual(1, totalBonus);
        }

        [Test]
        public void TryInitialize_AppliesEnemyLoadoutPowerAndCooldownOverridesToInstances()
        {
            GameDatabase database = CreateDatabase(startingHonor: 1);
            database.enemiesById["enemy.test"].abilityLoadout[0].power = 11;
            database.enemiesById["enemy.test"].abilityLoadout[0].cooldown = 3;

            DuelUiQueryService queryService = CreateQueryService(database);
            var sessionRunner = new DuelSessionRunner();
            bool success = sessionRunner.TryInitialize(
                queryService,
                "enemy.test",
                advanceToPlayerSetup: false,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(DuelPhase.Reset, sessionRunner.PhaseRunner.currentPhase);
            Assert.AreEqual(1, sessionRunner.DuelState.opponentLoadoutAbilityIds.Count);

            string instanceId = sessionRunner.DuelState.opponentLoadoutAbilityIds[0];
            AbilityInstance ability = sessionRunner.DuelState.abilitiesById[instanceId];
            Assert.AreEqual(11, ability.power);
            Assert.AreEqual(3, ability.cooldownTurns);
        }

        static GameDatabase CreateDatabase(int startingHonor)
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
                playerStart = new PlayerStartDef
                {
                    startingHonor = startingHonor,
                    startingPlayerHealth = 10,
                    startingLoadoutAbilityIds = new List<string>
                    {
                        "ability.player"
                    }
                }
            };

            database.abilitiesById["ability.player"] = new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 0,
                cooldown = 1,
                power = 3,
                isPlayerObtainable = true
            };

            database.abilitiesById["ability.enemy"] = new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 0,
                cooldown = 1,
                power = 2
            };

            database.enemiesById["enemy.test"] = new EnemyDef
            {
                health = 8,
                abilityLoadout = new List<AbilityLoadoutEntryDef>
                {
                    new AbilityLoadoutEntryDef
                    {
                        abilityId = "ability.enemy",
                        count = 1
                    }
                }
            };

            return database;
        }

        static DuelUiQueryService CreateQueryService(GameDatabase database)
        {
            var queryService = new DuelUiQueryService();
            bool bindSuccess = queryService.TryBindDatabase(database, out string failureMessage);
            Assert.IsTrue(bindSuccess, failureMessage);
            return queryService;
        }
    }
}


