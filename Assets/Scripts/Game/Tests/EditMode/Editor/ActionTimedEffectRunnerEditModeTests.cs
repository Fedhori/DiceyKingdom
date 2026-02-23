using System.Collections.Generic;
using Game.Application.Duel.Effects;
using Game.Domain.Duel;
using Game.Infrastructure.Data;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class ActionTimedEffectRunnerEditModeTests
    {
        [Test]
        public void ApplyForTiming_Roll_DoublesMikoAttackResult_WhenSingleOpponentIsPresent()
        {
            GameDatabase database = CreateDatabase();
            DuelState state = CreateDuelState();

            state.actionsById["p_miko"] = new ActionInstance
            {
                instanceId = "p_miko",
                actionDefId = "action.miko.assassin",
                attack = 4,
                baseRoll = 4,
                attackResult = 4
            };
            state.actionsById["e_rat"] = new ActionInstance
            {
                instanceId = "e_rat",
                actionDefId = "action.ratkin",
                attack = 2,
                baseRoll = 2,
                attackResult = 2
            };
            state.clashes[0].playerActionIds.Add("p_miko");
            state.clashes[0].opponentActionIds.Add("e_rat");

            var runner = new ActionTimedEffectRunner(database);
            ActionTimedEffectRunResult result = runner.ApplyForTiming(state, DuelEffectTiming.Roll);

            Assert.AreEqual(1, result.appliedCount);
            Assert.AreEqual(0, result.failedCount);
            Assert.AreEqual(8, state.actionsById["p_miko"].attackResult);
        }

        [Test]
        public void ApplyForTiming_Roll_DoesNotStackPreviousRollTimedModifiers()
        {
            GameDatabase database = CreateDatabase();
            DuelState state = CreateDuelState();

            state.actionsById["p_miko"] = new ActionInstance
            {
                instanceId = "p_miko",
                actionDefId = "action.miko.assassin",
                attack = 4,
                baseRoll = 4,
                attackResult = 4
            };
            state.actionsById["e_rat"] = new ActionInstance
            {
                instanceId = "e_rat",
                actionDefId = "action.ratkin",
                attack = 2,
                baseRoll = 2,
                attackResult = 2
            };
            state.clashes[0].playerActionIds.Add("p_miko");
            state.clashes[0].opponentActionIds.Add("e_rat");

            var runner = new ActionTimedEffectRunner(database);
            runner.ApplyForTiming(state, DuelEffectTiming.Roll);
            state.actionsById["p_miko"].attackResult = 4;
            runner.ApplyForTiming(state, DuelEffectTiming.Roll);

            Assert.AreEqual(8, state.actionsById["p_miko"].attackResult);
        }

        [Test]
        public void ApplyForTiming_Roll_DwarfCannon_ReducesOpponentAttackResultOnSameClash()
        {
            GameDatabase database = CreateDatabase();
            DuelState state = CreateDuelState();

            state.actionsById["p_dwarf"] = new ActionInstance
            {
                instanceId = "p_dwarf",
                actionDefId = "action.dwarf.cannon",
                attack = 4,
                baseRoll = 3,
                attackResult = 3
            };
            state.actionsById["e_a"] = new ActionInstance
            {
                instanceId = "e_a",
                actionDefId = "action.ratkin",
                attack = 2,
                baseRoll = 2,
                attackResult = 2
            };
            state.actionsById["e_b"] = new ActionInstance
            {
                instanceId = "e_b",
                actionDefId = "action.ratkin",
                attack = 2,
                baseRoll = 1,
                attackResult = 1
            };
            state.clashes[0].playerActionIds.Add("p_dwarf");
            state.clashes[0].opponentActionIds.Add("e_a");
            state.clashes[0].opponentActionIds.Add("e_b");

            var runner = new ActionTimedEffectRunner(database);
            ActionTimedEffectRunResult result = runner.ApplyForTiming(state, DuelEffectTiming.Roll);

            Assert.AreEqual(2, result.appliedCount);
            Assert.AreEqual(1, state.actionsById["e_a"].attackResult);
            Assert.AreEqual(1, state.actionsById["e_b"].attackResult);
        }

        [Test]
        public void ApplyForTiming_TurnEnd_ReservistInActionHolder_AddsDuelAttackModifier()
        {
            GameDatabase database = CreateDatabase();
            DuelState state = CreateDuelState();

            state.actionsById["p_reserve"] = new ActionInstance
            {
                instanceId = "p_reserve",
                actionDefId = "action.reservist",
                attack = 2,
                baseRoll = 0,
                attackResult = 0
            };
            state.actionHolderActionIds.Add("p_reserve");

            var runner = new ActionTimedEffectRunner(database);
            ActionTimedEffectRunResult result = runner.ApplyForTiming(state, DuelEffectTiming.TurnEnd);

            Assert.AreEqual(1, result.appliedCount);
            Assert.AreEqual(0, result.failedCount);
            Assert.AreEqual(1, state.actionsById["p_reserve"].attackModifiers.Count);
            Assert.AreEqual(2, state.actionsById["p_reserve"].attackModifiers[0].value);
        }

        static GameDatabase CreateDatabase()
        {
            var database = new GameDatabase();
            database.actionsById["action.miko.assassin"] = new ActionDef
            {
                attack = 4,
                effects = new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = "Roll",
                        condition = new ConditionDef
                        {
                            type = "OpponentCountEquals",
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

            database.actionsById["action.dwarf.cannon"] = new ActionDef
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
                                scope = "SameClashActions",
                                side = "Opponent",
                                mode = "Add",
                                value = -1
                            }
                        }
                    }
                }
            };

            database.actionsById["action.reservist"] = new ActionDef
            {
                attack = 2,
                effects = new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = "TurnEnd",
                        condition = new ConditionDef
                        {
                            type = "IsInActionHolder"
                        },
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = "AddAttackModifier",
                                target = "Attack",
                                layer = "Duel",
                                mode = "Add",
                                value = 2
                            }
                        }
                    }
                }
            };

            database.actionsById["action.ratkin"] = new ActionDef
            {
                attack = 2,
                effects = new List<TimedEffectDef>()
            };
            return database;
        }

        static DuelState CreateDuelState()
        {
            var state = new DuelState
            {
                playerHealth = 10,
                opponentHealth = 10
            };

            for (int i = 0; i < state.clashes.Count; i++)
            {
                ClashState clash = state.clashes[i];
                clash.playerActionIds.Clear();
                clash.opponentActionIds.Clear();
            }

            state.actionsById.Clear();
            state.actionHolderActionIds.Clear();
            return state;
        }
    }
}
