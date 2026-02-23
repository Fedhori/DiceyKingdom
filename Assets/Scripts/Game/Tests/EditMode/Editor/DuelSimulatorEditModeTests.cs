using System.Collections.Generic;
using Game.Domain.Duel;
using Game.Domain.Modifiers;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class DuelSimulatorEditModeTests
    {
        [Test]
        public void ComputeAttackResult_AppliesAddThenPercentBonusSumAndFloors()
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

            int attackResult = DuelSimulator.ComputeAttackResult(2, modifiers);

            Assert.AreEqual(9, attackResult);
        }

        [Test]
        public void ComputeAttackResult_UsesFloorWhenResultHasFraction()
        {
            var modifiers = new List<NumericModifier>
            {
                new NumericModifier
                {
                    operation = NumericModifierOperation.PercentBonus,
                    value = 50
                }
            };

            int attackResult = DuelSimulator.ComputeAttackResult(3, modifiers);

            Assert.AreEqual(4, attackResult);
        }

        [Test]
        public void ComputeAttackResult_ClampsToOneAtTheEnd()
        {
            var modifiers = new List<NumericModifier>
            {
                new NumericModifier
                {
                    operation = NumericModifierOperation.Add,
                    value = -5
                }
            };

            int attackResult = DuelSimulator.ComputeAttackResult(1, modifiers);

            Assert.AreEqual(1, attackResult);
        }

        [Test]
        public void RollAction_UsesRangeFromOneToAttack()
        {
            var action = new ActionInstance
            {
                attack = 6
            };
            var fakeRollSource = new FakeRollSource(4);

            DuelSimulator.RollAction(action, fakeRollSource);

            Assert.AreEqual(1, fakeRollSource.lastMinInclusive);
            Assert.AreEqual(6, fakeRollSource.lastMaxInclusive);
            Assert.AreEqual(4, action.baseRoll);
            Assert.AreEqual(4, action.attackResult);
        }

        [Test]
        public void RollAction_IncludesAttackModifiersBeforeRolling()
        {
            var action = new ActionInstance
            {
                attack = 4,
                attackModifiers = new List<NumericModifier>
                {
                    new NumericModifier
                    {
                        operation = NumericModifierOperation.Add,
                        value = 2
                    }
                }
            };
            var fakeRollSource = new FakeRollSource(6);

            DuelSimulator.RollAction(action, fakeRollSource);

            Assert.AreEqual(1, fakeRollSource.lastMinInclusive);
            Assert.AreEqual(6, fakeRollSource.lastMaxInclusive);
            Assert.AreEqual(6, action.baseRoll);
        }

        [Test]
        public void ComputeTotalAttack_UsesAttackResultSumPlusClashBonus()
        {
            var clash = new ClashState
            {
                playerActionIds = new List<string> { "p1", "p2" },
                opponentActionIds = new List<string> { "e1" },
                totalAttackBonusPlayer = 2,
                totalAttackBonusOpponent = 3
            };

            var actionsById = new Dictionary<string, ActionInstance>
            {
                { "p1", CreateAction("p1", 4) },
                { "p2", CreateAction("p2", 1) },
                { "e1", CreateAction("e1", 2) }
            };

            int playerStrength = DuelSimulator.ComputeTotalAttack(clash, actionsById, true);
            int opponentStrength = DuelSimulator.ComputeTotalAttack(clash, actionsById, false);

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

            state.actionsById["p0"] = CreateAction("p0", 1);
            state.actionsById["e0"] = CreateAction("e0", 5);
            state.actionsById["p1"] = CreateAction("p1", 10);
            state.actionsById["e1"] = CreateAction("e1", 1);

            state.actionsById["p0"].attackModifiers.Add(new NumericModifier
            {
                layer = ModifierLayer.Duel,
                operation = NumericModifierOperation.Add,
                value = 1
            });

            state.clashes[0].playerActionIds.Add("p0");
            state.clashes[0].opponentActionIds.Add("e0");
            state.clashes[1].playerActionIds.Add("p1");
            state.clashes[1].opponentActionIds.Add("e1");

            int resolvedCount = DuelSimulator.ClashResolveClashesInOrder(state);

            Assert.AreEqual(3, resolvedCount);
            Assert.IsFalse(state.isDuelEnded);
            Assert.AreEqual(1, state.playerHealth);
            Assert.AreEqual(5, state.opponentHealth);
            Assert.AreEqual(1, state.actionsById["p0"].attackModifiers.Count);
        }

        [Test]
        public void ClashResolveClashesInOrder_DoesNotMutateHealthWhenResolving()
        {
            var state = new DuelState
            {
                playerHealth = 5,
                opponentHealth = 5
            };

            state.actionsById["p0"] = CreateAction("p0", 4);
            state.actionsById["e0"] = CreateAction("e0", 2);
            state.actionsById["p1"] = CreateAction("p1", 2);
            state.actionsById["e1"] = CreateAction("e1", 2);
            state.actionsById["p2"] = CreateAction("p2", 2);
            state.actionsById["e2"] = CreateAction("e2", 3);

            state.clashes[0].playerActionIds.Add("p0");
            state.clashes[0].opponentActionIds.Add("e0");
            state.clashes[1].playerActionIds.Add("p1");
            state.clashes[1].opponentActionIds.Add("e1");
            state.clashes[2].playerActionIds.Add("p2");
            state.clashes[2].opponentActionIds.Add("e2");

            int resolvedCount = DuelSimulator.ClashResolveClashesInOrder(state);

            Assert.AreEqual(3, resolvedCount);
            Assert.IsFalse(state.isDuelEnded);
            Assert.AreEqual(5, state.playerHealth);
            Assert.AreEqual(5, state.opponentHealth);
        }

        static ActionInstance CreateAction(string actionId, int attackResultValue)
        {
            return new ActionInstance
            {
                actionDefId = actionId,
                attack = 6,
                baseRoll = attackResultValue,
                attackResult = attackResultValue
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
