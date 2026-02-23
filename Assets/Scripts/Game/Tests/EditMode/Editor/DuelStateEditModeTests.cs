using Game.Domain.Duel;
using Game.Domain.Modifiers;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class DuelStateEditModeTests
    {
        [Test]
        public void DuelState_DefaultInitialization_CollectionsAreNotNull()
        {
            var state = new DuelState();

            Assert.NotNull(state.clashes);
            Assert.NotNull(state.actionsById);
            Assert.NotNull(state.actionHolderActionIds);
            Assert.NotNull(state.opponentIntent);
        }

        [Test]
        public void DuelState_DefaultInitialization_ClashCountIsThree()
        {
            var state = new DuelState();

            Assert.AreEqual(DuelState.defaultClashCount, state.clashes.Count);
        }

        [Test]
        public void ActionInstance_DefaultInitialization_BaseModFinalFieldsAreAccessible()
        {
            var action = new ActionInstance();

            action.baseRoll = 2;
            action.attackResult = 5;
            action.attackResultModifiers.Add(new NumericModifier
            {
                operation = NumericModifierOperation.Add,
                value = 3,
                sourceId = "test.source"
            });

            Assert.AreEqual(2, action.baseRoll);
            Assert.AreEqual(5, action.attackResult);
            Assert.NotNull(action.attackResultModifiers);
            Assert.AreEqual(1, action.attackResultModifiers.Count);
            Assert.AreEqual(3, action.attackResultModifiers[0].value);
            Assert.AreEqual("test.source", action.attackResultModifiers[0].sourceId);
        }
    }
}
