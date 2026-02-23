using System.Collections.Generic;
using Game.Domain.Duel;
using Game.Domain.Modifiers;
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
        public void RollAbility_UsesRangeFromOneToAttack()
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
        public void RollAbility_IncludespowerModifiersBeforeRolling()
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
        public void ComputeTotalPower_UsespowerResultSumPlusClashBonus()
        {
            var clash = new ClashState
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

            int playerStrength = DuelSimulator.ComputeTotalPower(clash, abilitiesById, true);
            int opponentStrength = DuelSimulator.ComputeTotalPower(clash, abilitiesById, false);

            Assert.AreEqual(7, playerStrength);
            Assert.AreEqual(5, opponentStrength);
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
        public void ClashResolveClashesInOrder_ClashResolvesAllClashesWithoutApplyingHealthDelta()
        {
            var state = new DuelState
            {
                playerHealth = 1,
                opponentHealth = 5
            };
            AddClashes(state, 3);

            state.abilitiesById["p0"] = CreateAbility("p0", 1);
            state.abilitiesById["e0"] = CreateAbility("e0", 5);
            state.abilitiesById["p1"] = CreateAbility("p1", 10);
            state.abilitiesById["e1"] = CreateAbility("e1", 1);

            state.abilitiesById["p0"].powerModifiers.Add(new NumericModifier
            {
                layer = ModifierLayer.Duel,
                operation = NumericModifierOperation.Add,
                value = 1
            });

            state.clashes[0].playerAbilityIds.Add("p0");
            state.clashes[0].opponentAbilityIds.Add("e0");
            state.clashes[1].playerAbilityIds.Add("p1");
            state.clashes[1].opponentAbilityIds.Add("e1");

            int resolvedCount = DuelSimulator.ClashResolveClashesInOrder(state);

            Assert.AreEqual(3, resolvedCount);
            Assert.IsFalse(state.isDuelEnded);
            Assert.AreEqual(1, state.playerHealth);
            Assert.AreEqual(5, state.opponentHealth);
            Assert.AreEqual(1, state.abilitiesById["p0"].powerModifiers.Count);
        }

        [Test]
        public void ClashResolveClashesInOrder_DoesNotMutateHealthWhenResolving()
        {
            var state = new DuelState
            {
                playerHealth = 5,
                opponentHealth = 5
            };
            AddClashes(state, 3);

            state.abilitiesById["p0"] = CreateAbility("p0", 4);
            state.abilitiesById["e0"] = CreateAbility("e0", 2);
            state.abilitiesById["p1"] = CreateAbility("p1", 2);
            state.abilitiesById["e1"] = CreateAbility("e1", 2);
            state.abilitiesById["p2"] = CreateAbility("p2", 2);
            state.abilitiesById["e2"] = CreateAbility("e2", 3);

            state.clashes[0].playerAbilityIds.Add("p0");
            state.clashes[0].opponentAbilityIds.Add("e0");
            state.clashes[1].playerAbilityIds.Add("p1");
            state.clashes[1].opponentAbilityIds.Add("e1");
            state.clashes[2].playerAbilityIds.Add("p2");
            state.clashes[2].opponentAbilityIds.Add("e2");

            int resolvedCount = DuelSimulator.ClashResolveClashesInOrder(state);

            Assert.AreEqual(3, resolvedCount);
            Assert.IsFalse(state.isDuelEnded);
            Assert.AreEqual(5, state.playerHealth);
            Assert.AreEqual(5, state.opponentHealth);
        }

        static AbilityInstance CreateAbility(string abilityId, int powerResultValue)
        {
            return new AbilityInstance
            {
                abilityDefId = abilityId,
                power = 6,
                baseRoll = powerResultValue,
                powerResult = powerResultValue
            };
        }

        static void AddClashes(DuelState state, int count)
        {
            state.clashes.Clear();
            for (int i = 0; i < count; i++)
            {
                state.clashes.Add(new ClashState());
            }
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


