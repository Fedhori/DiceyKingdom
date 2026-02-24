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
        public void ApplyForTiming_Roll_DoublesMikoPowerResult_WhenSingleOpponentIsPresent()
        {
            GameDatabase database = CreateDatabase();
            DuelState state = CreateDuelState();

            state.abilitiesById["p_miko"] = new AbilityInstance
            {
                instanceId = "p_miko",
                abilityDefId = "ability.miko.assassin",
                abilityType = AbilityType.Attack,
                power = 4,
                baseRoll = 4,
                powerResult = 4
            };
            state.abilitiesById["e_rat"] = new AbilityInstance
            {
                instanceId = "e_rat",
                abilityDefId = "ability.ratkin",
                abilityType = AbilityType.Attack,
                power = 2,
                baseRoll = 2,
                powerResult = 2
            };
            state.combats[0].playerAbilityIds.Add("p_miko");
            state.combats[0].opponentAbilityIds.Add("e_rat");

            var runner = new AbilityTimedEffectRunner(database);
            AbilityTimedEffectRunResult result = runner.ApplyForTiming(state, DuelEffectTiming.Roll);

            Assert.AreEqual(1, result.appliedCount);
            Assert.AreEqual(0, result.failedCount);
            Assert.AreEqual(8, state.abilitiesById["p_miko"].powerResult);
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
                abilityType = AbilityType.Attack,
                power = 4,
                baseRoll = 4,
                powerResult = 4
            };
            state.abilitiesById["e_rat"] = new AbilityInstance
            {
                instanceId = "e_rat",
                abilityDefId = "ability.ratkin",
                abilityType = AbilityType.Attack,
                power = 2,
                baseRoll = 2,
                powerResult = 2
            };
            state.combats[0].playerAbilityIds.Add("p_miko");
            state.combats[0].opponentAbilityIds.Add("e_rat");

            var runner = new AbilityTimedEffectRunner(database);
            runner.ApplyForTiming(state, DuelEffectTiming.Roll);
            state.abilitiesById["p_miko"].powerResult = 4;
            runner.ApplyForTiming(state, DuelEffectTiming.Roll);

            Assert.AreEqual(8, state.abilitiesById["p_miko"].powerResult);
        }

        [Test]
        public void ApplyForTiming_Roll_DwarfCannon_ReducesOpponentPowerResultOnSameCombat()
        {
            GameDatabase database = CreateDatabase();
            DuelState state = CreateDuelState();

            state.abilitiesById["p_dwarf"] = new AbilityInstance
            {
                instanceId = "p_dwarf",
                abilityDefId = "ability.dwarf.cannon",
                abilityType = AbilityType.Attack,
                power = 4,
                baseRoll = 3,
                powerResult = 3
            };
            state.abilitiesById["e_a"] = new AbilityInstance
            {
                instanceId = "e_a",
                abilityDefId = "ability.ratkin",
                abilityType = AbilityType.Attack,
                power = 2,
                baseRoll = 2,
                powerResult = 2
            };
            state.abilitiesById["e_b"] = new AbilityInstance
            {
                instanceId = "e_b",
                abilityDefId = "ability.ratkin",
                abilityType = AbilityType.Attack,
                power = 2,
                baseRoll = 1,
                powerResult = 1
            };
            state.combats[0].playerAbilityIds.Add("p_dwarf");
            state.combats[0].opponentAbilityIds.Add("e_a");
            state.combats[0].opponentAbilityIds.Add("e_b");

            var runner = new AbilityTimedEffectRunner(database);
            AbilityTimedEffectRunResult result = runner.ApplyForTiming(state, DuelEffectTiming.Roll);

            Assert.AreEqual(2, result.appliedCount);
            Assert.AreEqual(1, state.abilitiesById["e_a"].powerResult);
            Assert.AreEqual(1, state.abilitiesById["e_b"].powerResult);
        }

        [Test]
        public void ApplyForTiming_TurnEnd_ReservistInLoadout_AddsDuelPowerModifier()
        {
            GameDatabase database = CreateDatabase();
            DuelState state = CreateDuelState();

            state.abilitiesById["p_loadout"] = new AbilityInstance
            {
                instanceId = "p_loadout",
                abilityDefId = "ability.reservist",
                abilityType = AbilityType.Attack,
                power = 2,
                baseRoll = 0,
                powerResult = 0
            };
            state.loadoutAbilityIds.Add("p_loadout");

            var runner = new AbilityTimedEffectRunner(database);
            AbilityTimedEffectRunResult result = runner.ApplyForTiming(state, DuelEffectTiming.TurnEnd);

            Assert.AreEqual(1, result.appliedCount);
            Assert.AreEqual(0, result.failedCount);
            Assert.AreEqual(1, state.abilitiesById["p_loadout"].powerModifiers.Count);
            Assert.AreEqual(2, state.abilitiesById["p_loadout"].powerModifiers[0].value);
        }

        static GameDatabase CreateDatabase()
        {
            var database = new GameDatabase();
            database.abilitiesById["ability.miko.assassin"] = new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 0,
                cooldown = 0,
                power = 4,
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
                                op = "ModifyPowerResult",
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
                power = 4,
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
                                scope = "SameCombatAbilities",
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
                power = 2,
                effects = new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = "TurnEnd",
                        condition = new ConditionDef
                        {
                            type = "IsInLoadout"
                        },
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = "AddPowerModifier",
                                target = "Power",
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
                power = 2,
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

            AddCombats(state, 3);
            state.abilitiesById.Clear();
            state.loadoutAbilityIds.Clear();
            return state;
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



