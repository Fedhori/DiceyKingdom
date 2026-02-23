using System.Collections.Generic;
using Game.Application.Duel.Effects;
using Game.Domain.Duel;
using Game.Infrastructure.Data;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class AbilityTimedEffectRunnerEditModeTests
    {
        [Test]
        public void ApplyForTiming_Roll_DoublesMikoAttackResult_WhenSingleOpponentIsPresent()
        {
            GameDatabase database = CreateDatabase();
            DuelState state = CreateDuelState();

            state.abilitiesById["p_miko"] = new AbilityInstance
            {
                instanceId = "p_miko",
                abilityDefId = "ability.miko.assassin",
                attack = 4,
                baseRoll = 4,
                attackResult = 4
            };
            state.abilitiesById["e_rat"] = new AbilityInstance
            {
                instanceId = "e_rat",
                abilityDefId = "ability.ratkin",
                attack = 2,
                baseRoll = 2,
                attackResult = 2
            };
            state.clashes[0].playerAbilityIds.Add("p_miko");
            state.clashes[0].opponentAbilityIds.Add("e_rat");

            var runner = new AbilityTimedEffectRunner(database);
            AbilityTimedEffectRunResult result = runner.ApplyForTiming(state, DuelEffectTiming.Roll);

            Assert.AreEqual(1, result.appliedCount);
            Assert.AreEqual(0, result.failedCount);
            Assert.AreEqual(8, state.abilitiesById["p_miko"].attackResult);
        }

        [Test]
        public void ApplyForTiming_Roll_DoesNotStackPreviousRollTimedModifiers()
        {
            GameDatabase database = CreateDatabase();
            DuelState state = CreateDuelState();

            state.abilitiesById["p_miko"] = new AbilityInstance
            {
                instanceId = "p_miko",
                abilityDefId = "ability.miko.assassin",
                attack = 4,
                baseRoll = 4,
                attackResult = 4
            };
            state.abilitiesById["e_rat"] = new AbilityInstance
            {
                instanceId = "e_rat",
                abilityDefId = "ability.ratkin",
                attack = 2,
                baseRoll = 2,
                attackResult = 2
            };
            state.clashes[0].playerAbilityIds.Add("p_miko");
            state.clashes[0].opponentAbilityIds.Add("e_rat");

            var runner = new AbilityTimedEffectRunner(database);
            runner.ApplyForTiming(state, DuelEffectTiming.Roll);
            state.abilitiesById["p_miko"].attackResult = 4;
            runner.ApplyForTiming(state, DuelEffectTiming.Roll);

            Assert.AreEqual(8, state.abilitiesById["p_miko"].attackResult);
        }

        [Test]
        public void ApplyForTiming_Roll_DwarfCannon_ReducesOpponentAttackResultOnSameClash()
        {
            GameDatabase database = CreateDatabase();
            DuelState state = CreateDuelState();

            state.abilitiesById["p_dwarf"] = new AbilityInstance
            {
                instanceId = "p_dwarf",
                abilityDefId = "ability.dwarf.cannon",
                attack = 4,
                baseRoll = 3,
                attackResult = 3
            };
            state.abilitiesById["e_a"] = new AbilityInstance
            {
                instanceId = "e_a",
                abilityDefId = "ability.ratkin",
                attack = 2,
                baseRoll = 2,
                attackResult = 2
            };
            state.abilitiesById["e_b"] = new AbilityInstance
            {
                instanceId = "e_b",
                abilityDefId = "ability.ratkin",
                attack = 2,
                baseRoll = 1,
                attackResult = 1
            };
            state.clashes[0].playerAbilityIds.Add("p_dwarf");
            state.clashes[0].opponentAbilityIds.Add("e_a");
            state.clashes[0].opponentAbilityIds.Add("e_b");

            var runner = new AbilityTimedEffectRunner(database);
            AbilityTimedEffectRunResult result = runner.ApplyForTiming(state, DuelEffectTiming.Roll);

            Assert.AreEqual(2, result.appliedCount);
            Assert.AreEqual(1, state.abilitiesById["e_a"].attackResult);
            Assert.AreEqual(1, state.abilitiesById["e_b"].attackResult);
        }

        [Test]
        public void ApplyForTiming_TurnEnd_ReservistInBag_AddsDuelAttackModifier()
        {
            GameDatabase database = CreateDatabase();
            DuelState state = CreateDuelState();

            state.abilitiesById["p_reserve"] = new AbilityInstance
            {
                instanceId = "p_reserve",
                abilityDefId = "ability.reservist",
                attack = 2,
                baseRoll = 0,
                attackResult = 0
            };
            state.bagAbilityIds.Add("p_reserve");

            var runner = new AbilityTimedEffectRunner(database);
            AbilityTimedEffectRunResult result = runner.ApplyForTiming(state, DuelEffectTiming.TurnEnd);

            Assert.AreEqual(1, result.appliedCount);
            Assert.AreEqual(0, result.failedCount);
            Assert.AreEqual(1, state.abilitiesById["p_reserve"].attackModifiers.Count);
            Assert.AreEqual(2, state.abilitiesById["p_reserve"].attackModifiers[0].value);
        }

        static GameDatabase CreateDatabase()
        {
            var database = new GameDatabase();
            database.abilitiesById["ability.miko.assassin"] = new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 0,
                cooldown = 0,
                damage = 4,
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

            database.abilitiesById["ability.dwarf.cannon"] = new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 0,
                cooldown = 0,
                damage = 4,
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
                                scope = "SameClashAbilities",
                                side = "Opponent",
                                mode = "Add",
                                value = -1
                            }
                        }
                    }
                }
            };

            database.abilitiesById["ability.reservist"] = new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 0,
                cooldown = 0,
                damage = 2,
                effects = new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = "TurnEnd",
                        condition = new ConditionDef
                        {
                            type = "IsInBag"
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

            database.abilitiesById["ability.ratkin"] = new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 0,
                cooldown = 0,
                damage = 2,
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
            AddClashes(state, 3);

            state.abilitiesById.Clear();
            state.bagAbilityIds.Clear();
            return state;
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


