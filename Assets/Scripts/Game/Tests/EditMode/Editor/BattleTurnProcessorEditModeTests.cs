using System.Collections.Generic;
using Game.Application.Battle;
using Game.Domain.Battle;
using Game.Infrastructure.Data;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class BattleTurnProcessorEditModeTests
    {
        [Test]
        public void TryResolveAllBattlefields_AppliesOutcomeEffectsFromData()
        {
            GameDatabase database = CreateDatabase();
            database.battlefieldsById["bf_0"] = CreateBattlefieldDefWithMoraleDamage();

            var state = new BattleState
            {
                playerMorale = 5,
                enemyMorale = 5,
                mana = 3
            };

            state.battlefields[0].battlefieldId = "bf_0";
            state.troopsById["p0"] = CreateTroop("troop_player", 3);
            state.troopsById["e0"] = CreateTroop("troop_enemy", 2);
            state.battlefields[0].playerTroopIds.Add("p0");
            state.battlefields[0].enemyTroopIds.Add("e0");

            var runner = new BattlePhaseRunner(state);
            var processor = new BattleTurnProcessor(database);

            AdvanceToResolve(runner);

            bool success = processor.TryResolveAllBattlefields(
                state,
                runner,
                out BattleResolveResult result,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(4, state.enemyMorale);
            Assert.AreEqual(1, result.outcomeEffectAppliedCount);
            Assert.AreEqual(0, result.outcomeEffectFailedCount);
            Assert.AreEqual(BattlePhase.Recall, runner.currentPhase);
            Assert.AreEqual(1, state.turnIndex);
        }

        [Test]
        public void TryResolveAllBattlefields_AppliesTurnEndManaAndCooldown()
        {
            GameDatabase database = CreateDatabase();
            database.battlefieldsById["bf_0"] = CreateBattlefieldDefWithDrawOnly();

            var state = new BattleState
            {
                playerMorale = 5,
                enemyMorale = 5,
                mana = 1
            };
            state.cooldowns["skill_a"] = 2;

            state.battlefields[0].battlefieldId = "bf_0";
            state.troopsById["p0"] = CreateTroop("troop_player", 2);
            state.troopsById["e0"] = CreateTroop("troop_enemy", 2);
            state.battlefields[0].playerTroopIds.Add("p0");
            state.battlefields[0].enemyTroopIds.Add("e0");

            var runner = new BattlePhaseRunner(state);
            var processor = new BattleTurnProcessor(database);

            AdvanceToResolve(runner);

            bool success = processor.TryResolveAllBattlefields(
                state,
                runner,
                out BattleResolveResult result,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(3, state.mana);
            Assert.AreEqual(1, state.cooldowns["skill_a"]);
            Assert.AreEqual(1, result.cooldownUpdatedCount);
            Assert.AreEqual(1, result.manaBeforeTurnEnd);
            Assert.AreEqual(3, result.manaAfterTurnEnd);
        }

        [Test]
        public void TryRollAllDeployedTroops_AppliesRollTimedEffects()
        {
            GameDatabase database = CreateDatabase();
            database.troopsById["troop_player"] = CreateRollEffectTroopDef();

            var state = new BattleState
            {
                playerMorale = 5,
                enemyMorale = 5
            };

            state.troopsById["p0"] = new TroopInstance
            {
                troopDefId = "troop_player",
                attack = 1
            };
            state.battlefields[0].playerTroopIds.Add("p0");

            var runner = new BattlePhaseRunner(state);
            var processor = new BattleTurnProcessor(database);

            Assert.IsTrue(runner.StartBattle());
            Assert.IsTrue(runner.AdvanceToNextPhase()); // EnemyDeploy
            Assert.IsTrue(runner.AdvanceToNextPhase()); // PlayerDeploy

            bool success = processor.TryRollAllDeployedTroops(
                state,
                runner,
                out BattleRollResult result,
                out string failureMessage);

            Assert.IsTrue(success, failureMessage);
            Assert.AreEqual(1, result.rolledTroopCount);
            Assert.AreEqual(1, result.timedEffectResult.appliedCount);
            Assert.AreEqual(2, state.troopsById["p0"].attackResult);
            Assert.AreEqual(BattlePhase.Tactics, runner.currentPhase);
        }

        static void AdvanceToResolve(BattlePhaseRunner runner)
        {
            Assert.IsTrue(runner.StartBattle());
            Assert.IsTrue(runner.AdvanceToNextPhase()); // EnemyDeploy
            Assert.IsTrue(runner.AdvanceToNextPhase()); // PlayerDeploy
            Assert.IsTrue(runner.AdvanceToNextPhase()); // Roll
            Assert.IsTrue(runner.AdvanceToNextPhase()); // Tactics
            Assert.IsTrue(runner.AdvanceToNextPhase()); // Resolve
        }

        static GameDatabase CreateDatabase()
        {
            var database = new GameDatabase
            {
                battleConfig = new BattleConfigDef
                {
                    battlefieldCount = 3,
                    manaMax = 5,
                    manaRegenPerTurn = 2,
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

        static BattlefieldDef CreateBattlefieldDefWithMoraleDamage()
        {
            return new BattlefieldDef
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
                                    op = "ModifyMorale",
                                    side = "Enemy",
                                    delta = -1
                                }
                            }
                        }
                    }
                }
            };
        }

        static BattlefieldDef CreateBattlefieldDefWithDrawOnly()
        {
            return new BattlefieldDef
            {
                outcomeEffects = new Dictionary<string, List<EffectBlockDef>>
                {
                    ["Draw"] = new List<EffectBlockDef>()
                }
            };
        }

        static TroopDef CreateRollEffectTroopDef()
        {
            return new TroopDef
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

        static TroopInstance CreateTroop(string troopDefId, int attackResult)
        {
            return new TroopInstance
            {
                troopDefId = troopDefId,
                attack = 6,
                baseRoll = attackResult,
                attackResult = attackResult
            };
        }
    }
}
