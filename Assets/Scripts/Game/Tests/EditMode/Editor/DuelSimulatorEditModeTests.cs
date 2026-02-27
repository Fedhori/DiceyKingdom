using System.Collections.Generic;
using Game.Domain.Duel;
using Game.Domain.Modifiers;
using Game.Infrastructure.Data;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class DuelSimulatorEditModeTests
    {
        [Test]
        public void ComputePowerResult_AppliesAddThenPercentBonusSumAndFloors()
        {
            var modifiers = new List<NumericModifier>
            {
                new NumericModifier
                {
                    operation = NumericModifierOperation.Add,
                    value = 1
                },
                new NumericModifier
                {
                    operation = NumericModifierOperation.PercentBonus,
                    value = 100
                },
                new NumericModifier
                {
                    operation = NumericModifierOperation.PercentBonus,
                    value = 100
                }
            };

            int powerResult = DuelSimulator.ComputePowerResult(2, modifiers);

            Assert.AreEqual(9, powerResult);
        }

        [Test]
        public void ComputePowerResult_UsesFloorWhenResultHasFraction()
        {
            var modifiers = new List<NumericModifier>
            {
                new NumericModifier
                {
                    operation = NumericModifierOperation.PercentBonus,
                    value = 50
                }
            };

            int powerResult = DuelSimulator.ComputePowerResult(3, modifiers);

            Assert.AreEqual(4, powerResult);
        }

        [Test]
        public void ComputePowerResult_ClampsToOneAtTheEnd()
        {
            var modifiers = new List<NumericModifier>
            {
                new NumericModifier
                {
                    operation = NumericModifierOperation.Add,
                    value = -5
                }
            };

            int powerResult = DuelSimulator.ComputePowerResult(1, modifiers);

            Assert.AreEqual(1, powerResult);
        }

        [Test]
        public void RollAbility_UsesRangeFromOneToPower()
        {
            var ability = new AbilityInstance
            {
                power = 6
            };
            var fakeRollSource = new FakeRollSource(4);

            DuelSimulator.RollAbility(ability, fakeRollSource);

            Assert.AreEqual(1, fakeRollSource.lastMinInclusive);
            Assert.AreEqual(6, fakeRollSource.lastMaxInclusive);
            Assert.AreEqual(4, ability.baseRoll);
            Assert.AreEqual(4, ability.powerResult);
        }

        [Test]
        public void RollAbility_IncludesPowerModifiersBeforeRolling()
        {
            var ability = new AbilityInstance
            {
                power = 4,
                powerModifiers = new List<NumericModifier>
                {
                    new NumericModifier
                    {
                        operation = NumericModifierOperation.Add,
                        value = 2
                    }
                }
            };
            var fakeRollSource = new FakeRollSource(6);

            DuelSimulator.RollAbility(ability, fakeRollSource);

            Assert.AreEqual(1, fakeRollSource.lastMinInclusive);
            Assert.AreEqual(6, fakeRollSource.lastMaxInclusive);
            Assert.AreEqual(6, ability.baseRoll);
        }

        [Test]
        public void RollAbility_AppliesRollMinPercentAsUniformLowerBound()
        {
            var ability = new AbilityInstance
            {
                power = 12,
                rollMinPercent = 50
            };
            var fakeRollSource = new FakeRollSource(6);

            DuelSimulator.RollAbility(ability, fakeRollSource);

            Assert.AreEqual(6, fakeRollSource.lastMinInclusive);
            Assert.AreEqual(12, fakeRollSource.lastMaxInclusive);
            Assert.AreEqual(6, ability.baseRoll);
            Assert.AreEqual(6, ability.powerResult);
        }

        [Test]
        public void ComputeTotalPower_UsesPowerResultSumPlusCombatBonus()
        {
            var combat = new CombatState
            {
                playerAbilityIds = new List<string> { "p1", "p2" },
                opponentAbilityIds = new List<string> { "e1" },
                totalPowerBonusPlayer = 2,
                totalPowerBonusOpponent = 3
            };

            var abilitiesById = new Dictionary<string, AbilityInstance>
            {
                { "p1", CreateAbility("p1", 4) },
                { "p2", CreateAbility("p2", 1) },
                { "e1", CreateAbility("e1", 2) }
            };

            int playerTotalPower = DuelSimulator.ComputeTotalPower(combat, abilitiesById, true);
            int opponentTotalPower = DuelSimulator.ComputeTotalPower(combat, abilitiesById, false);

            Assert.AreEqual(7, playerTotalPower);
            Assert.AreEqual(5, opponentTotalPower);
        }

        [Test]
        public void ComputeOutcome_ReturnsExpectedResult()
        {
            Assert.AreEqual(DuelOutcome.Victory, DuelSimulator.ComputeOutcome(10, 4));
            Assert.AreEqual(DuelOutcome.Victory, DuelSimulator.ComputeOutcome(3, 0));
            Assert.AreEqual(DuelOutcome.Draw, DuelSimulator.ComputeOutcome(5, 5));
            Assert.AreEqual(DuelOutcome.Defeat, DuelSimulator.ComputeOutcome(3, 10));
        }

        [Test]
        public void ClearModifierLayer_RemovesOnlyRequestedLayer()
        {
            var state = new DuelState
            {
                abilitiesById = new Dictionary<string, AbilityInstance>
                {
                    ["p1"] = new AbilityInstance
                    {
                        abilityDefId = "ability.player",
                        powerModifiers = new List<NumericModifier>
                        {
                            new NumericModifier
                            {
                                layer = ModifierLayer.Duel,
                                operation = NumericModifierOperation.Add,
                                value = 1
                            },
                            new NumericModifier
                            {
                                layer = ModifierLayer.Permanent,
                                operation = NumericModifierOperation.Add,
                                value = 1
                            }
                        }
                    }
                }
            };

            int removedCount = DuelSimulator.ClearModifierLayer(state, ModifierLayer.Duel);

            Assert.AreEqual(1, removedCount);
            Assert.AreEqual(1, state.abilitiesById["p1"].powerModifiers.Count);
            Assert.AreEqual(ModifierLayer.Permanent, state.abilitiesById["p1"].powerModifiers[0].layer);
        }

        static AbilityInstance CreateAbility(string abilityId, int powerResultValue)
        {
            return new AbilityInstance
            {
                abilityDefId = abilityId,
                abilityType = AbilityType.Attack,
                power = 6,
                baseRoll = powerResultValue,
                powerResult = powerResultValue
            };
        }

        sealed class FakeRollSource : IRollSource
        {
            readonly int nextValue;

            public int lastMinInclusive { get; private set; }
            public int lastMaxInclusive { get; private set; }

            public FakeRollSource(int nextValue)
            {
                this.nextValue = nextValue;
            }

            public int Next(int minInclusive, int maxInclusive)
            {
                lastMinInclusive = minInclusive;
                lastMaxInclusive = maxInclusive;
                return nextValue;
            }
        }
    }
}


