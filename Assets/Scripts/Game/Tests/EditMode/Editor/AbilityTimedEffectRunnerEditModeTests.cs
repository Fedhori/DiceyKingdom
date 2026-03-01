using System.Collections.Generic;
using Game.Application.Duel.Effects;
using Game.Domain.Duel;
using Game.Domain.Modifiers;
using Game.Infrastructure.Data;
using Game.Infrastructure.Data.Effects;
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
                abilityDefId = "ability.miko_assassin",
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
                abilityDefId = "ability.miko_assassin",
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
        public void ApplyForTiming_Formation_Underdog_GainsPowerPercentBonus_WhenOpponentCountIsGreaterThanSelf()
        {
            GameDatabase database = CreateDatabase();
            DuelState state = CreateDuelState();

            state.abilitiesById["p_underdog"] = new AbilityInstance
            {
                instanceId = "p_underdog",
                abilityDefId = "ability.underdog_test",
                abilityType = AbilityType.Attack,
                power = 5,
                baseRoll = 0,
                powerResult = 0
            };
            state.abilitiesById["e_a"] = new AbilityInstance
            {
                instanceId = "e_a",
                abilityDefId = "ability.ratkin",
                abilityType = AbilityType.Attack,
                power = 2,
                baseRoll = 0,
                powerResult = 0
            };
            state.abilitiesById["e_b"] = new AbilityInstance
            {
                instanceId = "e_b",
                abilityDefId = "ability.ratkin",
                abilityType = AbilityType.Attack,
                power = 2,
                baseRoll = 0,
                powerResult = 0
            };

            state.combats[0].playerAbilityIds.Add("p_underdog");
            state.combats[0].opponentAbilityIds.Add("e_a");
            state.combats[0].opponentAbilityIds.Add("e_b");

            var runner = new AbilityTimedEffectRunner(database);
            AbilityTimedEffectRunResult result = runner.ApplyForTiming(state, DuelEffectTiming.Formation);

            Assert.AreEqual(1, result.appliedCount);
            Assert.AreEqual(0, result.failedCount);
            Assert.AreEqual(1, state.abilitiesById["p_underdog"].powerModifiers.Count);
            NumericModifier modifier = state.abilitiesById["p_underdog"].powerModifiers[0];
            Assert.AreEqual(NumericModifierOperation.PercentBonus, modifier.operation);
            Assert.AreEqual(100, modifier.value);
        }

        [Test]
        public void ApplyForTiming_Formation_Underdog_ReevaluatesAndRemovesPreviousModifier_WhenConditionChanges()
        {
            GameDatabase database = CreateDatabase();
            DuelState state = CreateDuelState();

            state.abilitiesById["p_underdog"] = new AbilityInstance
            {
                instanceId = "p_underdog",
                abilityDefId = "ability.underdog_test",
                abilityType = AbilityType.Attack,
                power = 5,
                baseRoll = 0,
                powerResult = 0
            };
            state.abilitiesById["p_ally"] = new AbilityInstance
            {
                instanceId = "p_ally",
                abilityDefId = "ability.ratkin",
                abilityType = AbilityType.Attack,
                power = 2,
                baseRoll = 0,
                powerResult = 0
            };
            state.abilitiesById["e_a"] = new AbilityInstance
            {
                instanceId = "e_a",
                abilityDefId = "ability.ratkin",
                abilityType = AbilityType.Attack,
                power = 2,
                baseRoll = 0,
                powerResult = 0
            };
            state.abilitiesById["e_b"] = new AbilityInstance
            {
                instanceId = "e_b",
                abilityDefId = "ability.ratkin",
                abilityType = AbilityType.Attack,
                power = 2,
                baseRoll = 0,
                powerResult = 0
            };

            state.combats[0].playerAbilityIds.Add("p_underdog");
            state.combats[0].opponentAbilityIds.Add("e_a");
            state.combats[0].opponentAbilityIds.Add("e_b");

            var runner = new AbilityTimedEffectRunner(database);
            runner.ApplyForTiming(state, DuelEffectTiming.Formation);
            Assert.AreEqual(1, state.abilitiesById["p_underdog"].powerModifiers.Count);

            state.combats[0].playerAbilityIds.Add("p_ally");
            runner.ApplyForTiming(state, DuelEffectTiming.Formation);
            Assert.AreEqual(0, state.abilitiesById["p_underdog"].powerModifiers.Count);

            state.combats[0].playerAbilityIds.Remove("p_ally");
            runner.ApplyForTiming(state, DuelEffectTiming.Formation);
            Assert.AreEqual(1, state.abilitiesById["p_underdog"].powerModifiers.Count);
        }

        [Test]
        public void ApplyForTiming_Roll_DwarfCannon_ReducesOpponentPowerResultOnSameCombat()
        {
            GameDatabase database = CreateDatabase();
            DuelState state = CreateDuelState();

            state.abilitiesById["p_dwarf"] = new AbilityInstance
            {
                instanceId = "p_dwarf",
                abilityDefId = "ability.dwarf_cannon",
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

        [Test]
        public void ApplyForTiming_Deploy_WithSourceFilter_TriggersOnlyRequestedAbility()
        {
            GameDatabase database = CreateDatabase();
            DuelState state = CreateDuelState();

            state.abilitiesById["p_deploy"] = new AbilityInstance
            {
                instanceId = "p_deploy",
                abilityDefId = "ability.deploy_banner",
                abilityType = AbilityType.Attack,
                power = 2,
                baseRoll = 0,
                powerResult = 0
            };
            state.abilitiesById["p_other"] = new AbilityInstance
            {
                instanceId = "p_other",
                abilityDefId = "ability.deploy_banner",
                abilityType = AbilityType.Attack,
                power = 2,
                baseRoll = 0,
                powerResult = 0
            };

            state.combats[0].playerAbilityIds.Add("p_deploy");
            state.combats[1].playerAbilityIds.Add("p_other");

            var runner = new AbilityTimedEffectRunner(database);
            AbilityTimedEffectRunResult result = runner.ApplyForTiming(
                state,
                DuelEffectTiming.Deploy,
                new[] { "p_deploy" });

            Assert.AreEqual(1, result.appliedCount);
            Assert.AreEqual(0, result.failedCount);
            Assert.AreEqual(2, state.combats[0].totalPowerBonusPlayer);
            Assert.AreEqual(0, state.combats[1].totalPowerBonusPlayer);
        }

        [Test]
        public void ApplyForTiming_TurnEnd_ModifyTotalPowerOnLoadoutTarget_FailsAndSkipsCombatMutation()
        {
            GameDatabase database = CreateDatabase();
            DuelState state = CreateDuelState();

            state.abilitiesById["p_loadout"] = new AbilityInstance
            {
                instanceId = "p_loadout",
                abilityDefId = "ability.loadout_totalpower_invalid",
                abilityType = AbilityType.Passive,
                cooldownTurns = 0,
                cooldownRemaining = 0,
                power = 0,
                baseRoll = 0,
                powerResult = 0
            };
            state.loadoutAbilityIds.Add("p_loadout");

            var runner = new AbilityTimedEffectRunner(database);
            AbilityTimedEffectRunResult result = runner.ApplyForTiming(state, DuelEffectTiming.TurnEnd);

            Assert.AreEqual(0, result.appliedCount);
            Assert.AreEqual(1, result.failedCount);
            Assert.AreEqual(0, state.combats[0].totalPowerBonusPlayer);
        }

        [Test]
        public void ApplyForTiming_TurnEnd_ModifyHealthWithoutSide_UsesSelfSideForPlayerAndOpponent()
        {
            GameDatabase database = CreateDatabase();
            DuelState state = CreateDuelState();

            state.playerHealth = 5;
            state.opponentHealth = 7;
            state.abilitiesById["p_passive"] = new AbilityInstance
            {
                instanceId = "p_passive",
                abilityDefId = "ability.passive_self_heal",
                abilityType = AbilityType.Passive,
                cooldownTurns = 0,
                cooldownRemaining = 0,
                power = 0,
                baseRoll = 0,
                powerResult = 0
            };
            state.abilitiesById["e_passive"] = new AbilityInstance
            {
                instanceId = "e_passive",
                abilityDefId = "ability.passive_self_heal",
                abilityType = AbilityType.Passive,
                cooldownTurns = 0,
                cooldownRemaining = 0,
                power = 0,
                baseRoll = 0,
                powerResult = 0
            };
            state.loadoutAbilityIds.Add("p_passive");
            state.opponentLoadoutAbilityIds.Add("e_passive");

            var runner = new AbilityTimedEffectRunner(database);
            AbilityTimedEffectRunResult result = runner.ApplyForTiming(state, DuelEffectTiming.TurnEnd);

            Assert.AreEqual(2, result.appliedCount);
            Assert.AreEqual(0, result.failedCount);
            Assert.AreEqual(6, state.playerHealth);
            Assert.AreEqual(8, state.opponentHealth);
        }

        [Test]
        public void ApplyForTiming_ModifyHealthDamage_EmitsHealthLostAndTriggersBerserk()
        {
            GameDatabase database = CreateDatabase();
            DuelState state = CreateDuelState();

            state.playerHealth = 10;
            state.abilitiesById["p_berserk"] = new AbilityInstance
            {
                instanceId = "p_berserk",
                abilityDefId = "ability.berserk",
                abilityType = AbilityType.Attack,
                cooldownTurns = 1,
                cooldownRemaining = 1,
                power = 6
            };
            state.abilitiesById["p_blood_tax"] = new AbilityInstance
            {
                instanceId = "p_blood_tax",
                abilityDefId = "ability.blood_tax",
                abilityType = AbilityType.Passive,
                cooldownTurns = 0,
                cooldownRemaining = 0,
                power = 0
            };
            state.loadoutAbilityIds.Add("p_berserk");
            state.loadoutAbilityIds.Add("p_blood_tax");

            var runner = new AbilityTimedEffectRunner(database);
            AbilityTimedEffectRunResult result = runner.ApplyForTiming(state, DuelEffectTiming.TurnEnd);

            Assert.AreEqual(1, result.appliedCount);
            Assert.AreEqual(0, result.failedCount);
            Assert.AreEqual(9, state.playerHealth);
            Assert.AreEqual(1, state.abilitiesById["p_berserk"].powerModifiers.Count);
            Assert.AreEqual(3, state.abilitiesById["p_berserk"].powerModifiers[0].value);
        }

        static GameDatabase CreateDatabase()
        {
            var database = new GameDatabase();
            database.abilitiesById["ability.miko_assassin"] = new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 0,
                cooldown = 1,
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

            database.abilitiesById["ability.dwarf_cannon"] = new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 0,
                cooldown = 1,
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

            database.abilitiesById["ability.underdog_test"] = new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 0,
                cooldown = 1,
                power = 5,
                effects = new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = "Formation",
                        condition = new ConditionDef
                        {
                            type = "OpponentCountGreaterThanSelf"
                        },
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = "AddPowerModifier",
                                scope = "Self",
                                target = "Power",
                                layer = "Duel",
                                mode = "PercentBonus",
                                value = 100
                            }
                        }
                    }
                }
            };

            database.abilitiesById["ability.reservist"] = new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 0,
                cooldown = 1,
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
                cooldown = 1,
                power = 2,
                effects = new List<TimedEffectDef>()
            };

            database.abilitiesById["ability.deploy_banner"] = new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 0,
                cooldown = 1,
                power = 2,
                effects = new List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = "Deploy",
                        condition = new ConditionDef
                        {
                            type = "Always"
                        },
                        ops = new List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = "ModifyTotalPower",
                                scope = "Self",
                                side = "Player",
                                value = 2
                            }
                        }
                    }
                }
            };

            database.abilitiesById["ability.loadout_totalpower_invalid"] = new AbilityDef
            {
                type = AbilityType.Passive.ToString(),
                buildCost = 0,
                cooldown = 0,
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
                                op = "ModifyTotalPower",
                                scope = "Self",
                                side = "Player",
                                value = 2
                            }
                        }
                    }
                }
            };

            database.abilitiesById["ability.passive_self_heal"] = new AbilityDef
            {
                type = AbilityType.Passive.ToString(),
                buildCost = 0,
                cooldown = 0,
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
                                scope = "Self",
                                value = 1
                            }
                        }
                    }
                }
            };

            database.abilitiesById["ability.berserk"] = new AbilityDef
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

            database.abilitiesById["ability.blood_tax"] = new AbilityDef
            {
                type = AbilityType.Passive.ToString(),
                buildCost = 0,
                cooldown = 0,
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
                                scope = "Self",
                                value = -1
                            }
                        }
                    }
                }
            };

            return database;
        }

        static DuelState CreateDuelState()
        {
            var state = new DuelState
            {
                playerHealth = 10,
                opponentHealth = 10,
                maxPlayerHealth = 10,
                maxOpponentHealth = 10
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



