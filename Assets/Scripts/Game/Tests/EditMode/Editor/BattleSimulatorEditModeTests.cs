using System.Collections.Generic;
using Game.Domain.Battle;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class BattleSimulatorEditModeTests
    {
        [Test]
        public void ComputeFinalFaceValue_AppliesAddThenPercentBonusSumAndFloors()
        {
            var modifiers = new List<TroopModifierEntry>
            {
                new TroopModifierEntry
                {
                    modifierType = TroopModifierType.Add,
                    delta = 1
                },
                new TroopModifierEntry
                {
                    modifierType = TroopModifierType.PercentBonus,
                    delta = 100
                },
                new TroopModifierEntry
                {
                    modifierType = TroopModifierType.PercentBonus,
                    delta = 100
                }
            };

            int finalValue = BattleSimulator.ComputeFinalFaceValue(2, modifiers);

            Assert.AreEqual(9, finalValue);
        }

        [Test]
        public void ComputeFinalFaceValue_UsesFloorWhenResultHasFraction()
        {
            var modifiers = new List<TroopModifierEntry>
            {
                new TroopModifierEntry
                {
                    modifierType = TroopModifierType.PercentBonus,
                    delta = 50
                }
            };

            int finalValue = BattleSimulator.ComputeFinalFaceValue(3, modifiers);

            Assert.AreEqual(4, finalValue);
        }

        [Test]
        public void ComputeFinalFaceValue_ClampsToOneAtTheEnd()
        {
            var modifiers = new List<TroopModifierEntry>
            {
                new TroopModifierEntry
                {
                    modifierType = TroopModifierType.Add,
                    delta = -5
                }
            };

            int finalValue = BattleSimulator.ComputeFinalFaceValue(1, modifiers);

            Assert.AreEqual(1, finalValue);
        }

        [Test]
        public void RollTroop_UsesRangeFromOneToPower()
        {
            var troop = new TroopInstance
            {
                power = 6
            };
            var fakeRollSource = new FakeRollSource(4);

            BattleSimulator.RollTroop(troop, fakeRollSource);

            Assert.AreEqual(1, fakeRollSource.lastMinInclusive);
            Assert.AreEqual(6, fakeRollSource.lastMaxInclusive);
            Assert.AreEqual(4, troop.baseRoll);
            Assert.AreEqual(4, troop.faceValueFinal);
        }

        [Test]
        public void ComputeCombatStrength_UsesFinalFaceValueSumPlusBattlefieldBonus()
        {
            var battlefield = new BattlefieldState
            {
                playerTroopIds = new List<string> { "p1", "p2" },
                enemyTroopIds = new List<string> { "e1" },
                combatStrengthBonusPlayer = 2,
                combatStrengthBonusEnemy = 3
            };

            var troopsById = new Dictionary<string, TroopInstance>
            {
                { "p1", CreateTroop("p1", 4) },
                { "p2", CreateTroop("p2", 1) },
                { "e1", CreateTroop("e1", 2) }
            };

            int playerStrength = BattleSimulator.ComputeCombatStrength(battlefield, troopsById, true);
            int enemyStrength = BattleSimulator.ComputeCombatStrength(battlefield, troopsById, false);

            Assert.AreEqual(7, playerStrength);
            Assert.AreEqual(5, enemyStrength);
        }

        [Test]
        public void ComputeOutcome_ReturnsExpectedResult()
        {
            Assert.AreEqual(BattleOutcome.GreatVictory, BattleSimulator.ComputeOutcome(10, 4));
            Assert.AreEqual(BattleOutcome.GreatVictory, BattleSimulator.ComputeOutcome(3, 0));
            Assert.AreEqual(BattleOutcome.Draw, BattleSimulator.ComputeOutcome(5, 5));
            Assert.AreEqual(BattleOutcome.GreatDefeat, BattleSimulator.ComputeOutcome(3, 10));
        }

        [Test]
        public void ResolveBattlefieldsInOrder_StopsImmediatelyWhenMoraleDropsToZero()
        {
            var state = new BattleState
            {
                playerMorale = 1,
                enemyMorale = 5
            };

            state.troopsById["p0"] = CreateTroop("p0", 1);
            state.troopsById["e0"] = CreateTroop("e0", 5);
            state.troopsById["p1"] = CreateTroop("p1", 10);
            state.troopsById["e1"] = CreateTroop("e1", 1);

            state.battlefields[0].playerTroopIds.Add("p0");
            state.battlefields[0].enemyTroopIds.Add("e0");
            state.battlefields[1].playerTroopIds.Add("p1");
            state.battlefields[1].enemyTroopIds.Add("e1");

            int resolvedCount = BattleSimulator.ResolveBattlefieldsInOrder(state);

            Assert.AreEqual(1, resolvedCount);
            Assert.IsTrue(state.isBattleEnded);
            Assert.LessOrEqual(state.playerMorale, 0);
            Assert.AreEqual(5, state.enemyMorale);
        }

        [Test]
        public void ResolveBattlefieldsInOrder_ProcessesAllBattlefieldsWhenBattleDoesNotEnd()
        {
            var state = new BattleState
            {
                playerMorale = 5,
                enemyMorale = 5
            };

            state.troopsById["p0"] = CreateTroop("p0", 4);
            state.troopsById["e0"] = CreateTroop("e0", 2);
            state.troopsById["p1"] = CreateTroop("p1", 2);
            state.troopsById["e1"] = CreateTroop("e1", 2);
            state.troopsById["p2"] = CreateTroop("p2", 2);
            state.troopsById["e2"] = CreateTroop("e2", 3);

            state.battlefields[0].playerTroopIds.Add("p0");
            state.battlefields[0].enemyTroopIds.Add("e0");
            state.battlefields[1].playerTroopIds.Add("p1");
            state.battlefields[1].enemyTroopIds.Add("e1");
            state.battlefields[2].playerTroopIds.Add("p2");
            state.battlefields[2].enemyTroopIds.Add("e2");

            int resolvedCount = BattleSimulator.ResolveBattlefieldsInOrder(state);

            Assert.AreEqual(3, resolvedCount);
            Assert.IsFalse(state.isBattleEnded);
            Assert.AreEqual(4, state.playerMorale);
            Assert.AreEqual(3, state.enemyMorale);
        }

        static TroopInstance CreateTroop(string troopId, int finalFaceValue)
        {
            return new TroopInstance
            {
                troopDefId = troopId,
                power = 6,
                baseRoll = finalFaceValue,
                faceValueFinal = finalFaceValue
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
