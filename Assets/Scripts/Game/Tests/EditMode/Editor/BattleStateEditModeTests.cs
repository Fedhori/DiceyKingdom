using Game.Domain.Battle;
using Game.Domain.Modifiers;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class BattleStateEditModeTests
    {
        [Test]
        public void BattleState_DefaultInitialization_CollectionsAreNotNull()
        {
            var state = new BattleState();

            Assert.NotNull(state.cooldowns);
            Assert.NotNull(state.battlefields);
            Assert.NotNull(state.troopsById);
            Assert.NotNull(state.campTroopIds);
            Assert.NotNull(state.enemyIntent);
        }

        [Test]
        public void BattleState_DefaultInitialization_BattlefieldCountIsThree()
        {
            var state = new BattleState();

            Assert.AreEqual(BattleState.defaultBattlefieldCount, state.battlefields.Count);
        }

        [Test]
        public void TroopInstance_DefaultInitialization_BaseModFinalFieldsAreAccessible()
        {
            var troop = new TroopInstance();

            troop.baseRoll = 2;
            troop.attackResult = 5;
            troop.attackResultModifiers.Add(new NumericModifier
            {
                operation = NumericModifierOperation.Add,
                value = 3,
                sourceId = "test.source"
            });

            Assert.AreEqual(2, troop.baseRoll);
            Assert.AreEqual(5, troop.attackResult);
            Assert.NotNull(troop.attackResultModifiers);
            Assert.AreEqual(1, troop.attackResultModifiers.Count);
            Assert.AreEqual(3, troop.attackResultModifiers[0].value);
            Assert.AreEqual("test.source", troop.attackResultModifiers[0].sourceId);
        }
    }
}
