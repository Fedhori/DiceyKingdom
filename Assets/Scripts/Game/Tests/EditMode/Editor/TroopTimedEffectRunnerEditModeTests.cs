using System.Collections.Generic;
using Game.Application.Battle.Effects;
using Game.Domain.Battle;
using Game.Infrastructure.Data;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class TroopTimedEffectRunnerEditModeTests
    {
        [Test]
        public void ApplyForTiming_Roll_DoublesMikoAttackResult_WhenSingleEnemyIsPresent()
        {
            GameDatabase database = CreateDatabase();
            BattleState state = CreateBattleState();

            state.troopsById["p_miko"] = new TroopInstance
            {
                instanceId = "p_miko",
                troopDefId = "troop_miko_assassin",
                attack = 4,
                baseRoll = 4,
                attackResult = 4
            };
            state.troopsById["e_rat"] = new TroopInstance
            {
                instanceId = "e_rat",
                troopDefId = "troop_ratkin",
                attack = 2,
                baseRoll = 2,
                attackResult = 2
            };
            state.battlefields[0].playerTroopIds.Add("p_miko");
            state.battlefields[0].enemyTroopIds.Add("e_rat");

            var runner = new TroopTimedEffectRunner(database);
            TroopTimedEffectRunResult result = runner.ApplyForTiming(state, BattleEffectTiming.Roll);

            Assert.AreEqual(1, result.appliedCount);
            Assert.AreEqual(0, result.failedCount);
            Assert.AreEqual(8, state.troopsById["p_miko"].attackResult);
        }

        [Test]
        public void ApplyForTiming_Roll_DoesNotStackPreviousRollTimedModifiers()
        {
            GameDatabase database = CreateDatabase();
            BattleState state = CreateBattleState();

            state.troopsById["p_miko"] = new TroopInstance
            {
                instanceId = "p_miko",
                troopDefId = "troop_miko_assassin",
                attack = 4,
                baseRoll = 4,
                attackResult = 4
            };
            state.troopsById["e_rat"] = new TroopInstance
            {
                instanceId = "e_rat",
                troopDefId = "troop_ratkin",
                attack = 2,
                baseRoll = 2,
                attackResult = 2
            };
            state.battlefields[0].playerTroopIds.Add("p_miko");
            state.battlefields[0].enemyTroopIds.Add("e_rat");

            var runner = new TroopTimedEffectRunner(database);
            runner.ApplyForTiming(state, BattleEffectTiming.Roll);
            state.troopsById["p_miko"].attackResult = 4;
            runner.ApplyForTiming(state, BattleEffectTiming.Roll);

            Assert.AreEqual(8, state.troopsById["p_miko"].attackResult);
        }

        [Test]
        public void ApplyForTiming_Roll_DwarfCannon_ReducesEnemyAttackResultOnSameBattlefield()
        {
            GameDatabase database = CreateDatabase();
            BattleState state = CreateBattleState();

            state.troopsById["p_dwarf"] = new TroopInstance
            {
                instanceId = "p_dwarf",
                troopDefId = "troop_dwarf_cannon",
                attack = 4,
                baseRoll = 3,
                attackResult = 3
            };
            state.troopsById["e_a"] = new TroopInstance
            {
                instanceId = "e_a",
                troopDefId = "troop_ratkin",
                attack = 2,
                baseRoll = 2,
                attackResult = 2
            };
            state.troopsById["e_b"] = new TroopInstance
            {
                instanceId = "e_b",
                troopDefId = "troop_ratkin",
                attack = 2,
                baseRoll = 1,
                attackResult = 1
            };
            state.battlefields[0].playerTroopIds.Add("p_dwarf");
            state.battlefields[0].enemyTroopIds.Add("e_a");
            state.battlefields[0].enemyTroopIds.Add("e_b");

            var runner = new TroopTimedEffectRunner(database);
            TroopTimedEffectRunResult result = runner.ApplyForTiming(state, BattleEffectTiming.Roll);

            Assert.AreEqual(2, result.appliedCount);
            Assert.AreEqual(1, state.troopsById["e_a"].attackResult);
            Assert.AreEqual(1, state.troopsById["e_b"].attackResult);
        }

        [Test]
        public void ApplyForTiming_TurnEnd_ReservistInCamp_AddsBattleAttackModifier()
        {
            GameDatabase database = CreateDatabase();
            BattleState state = CreateBattleState();

            state.troopsById["p_reserve"] = new TroopInstance
            {
                instanceId = "p_reserve",
                troopDefId = "troop_reservist",
                attack = 2,
                baseRoll = 0,
                attackResult = 0
            };
            state.campTroopIds.Add("p_reserve");

            var runner = new TroopTimedEffectRunner(database);
            TroopTimedEffectRunResult result = runner.ApplyForTiming(state, BattleEffectTiming.TurnEnd);

            Assert.AreEqual(1, result.appliedCount);
            Assert.AreEqual(0, result.failedCount);
            Assert.AreEqual(1, state.troopsById["p_reserve"].attackModifiers.Count);
            Assert.AreEqual(2, state.troopsById["p_reserve"].attackModifiers[0].value);
        }

        static GameDatabase CreateDatabase()
        {
            var database = new GameDatabase();
            database.troopsById["troop_miko_assassin"] = new TroopDef
            {
                attack = 4,
                effects = new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = "Roll",
                        condition = new ConditionDef
                        {
                            type = "EnemyCountEquals",
                            value = 1
                        },
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = "ModifyAttackResult",
                                scope = "Self",
                                mode = "PercentBonus",
                                value = 100
                            }
                        }
                    }
                }
            };

            database.troopsById["troop_dwarf_cannon"] = new TroopDef
            {
                attack = 4,
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
                                scope = "SameBattlefieldTroops",
                                side = "Enemy",
                                mode = "Add",
                                value = -1
                            }
                        }
                    }
                }
            };

            database.troopsById["troop_reservist"] = new TroopDef
            {
                attack = 2,
                effects = new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = "TurnEnd",
                        condition = new ConditionDef
                        {
                            type = "IsInCamp"
                        },
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = "AddAttackModifier",
                                target = "Attack",
                                layer = "Battle",
                                mode = "Add",
                                value = 2
                            }
                        }
                    }
                }
            };

            database.troopsById["troop_ratkin"] = new TroopDef
            {
                attack = 2,
                effects = new List<TimedEffectDef>()
            };
            return database;
        }

        static BattleState CreateBattleState()
        {
            var state = new BattleState
            {
                playerMorale = 10,
                enemyMorale = 10
            };

            for (int i = 0; i < state.battlefields.Count; i++)
            {
                BattlefieldState battlefield = state.battlefields[i];
                battlefield.playerTroopIds.Clear();
                battlefield.enemyTroopIds.Clear();
            }

            state.troopsById.Clear();
            state.campTroopIds.Clear();
            return state;
        }
    }
}
