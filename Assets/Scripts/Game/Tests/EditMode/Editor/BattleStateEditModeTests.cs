using Game.Domain.Battle;
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
            troop.modifiers.Add(new TroopModifierEntry
            {
                delta = 3,
                sourceId = "test.source"
            });

            Assert.AreEqual(2, troop.baseRoll);
            Assert.AreEqual(5, troop.attackResult);
            Assert.NotNull(troop.modifiers);
            Assert.AreEqual(1, troop.modifiers.Count);
            Assert.AreEqual(3, troop.modifiers[0].delta);
            Assert.AreEqual("test.source", troop.modifiers[0].sourceId);
        }
    }
}
