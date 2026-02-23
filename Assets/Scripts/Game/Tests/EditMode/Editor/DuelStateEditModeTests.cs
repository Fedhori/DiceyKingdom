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
            Assert.NotNull(state.abilitiesById);
            Assert.NotNull(state.abilityHolderAbilityIds);
            Assert.NotNull(state.intent);
        }

        [Test]
        public void DuelState_DefaultInitialization_ClashCountIsThree()
        {
            var state = new DuelState();

            Assert.AreEqual(DuelState.defaultClashCount, state.clashes.Count);
        }

        [Test]
        public void AbilityInstance_DefaultInitialization_BaseModFinalFieldsAreAccessible()
        {
            var ability = new AbilityInstance();

            ability.baseRoll = 2;
            ability.attackResult = 5;
            ability.attackResultModifiers.Add(new NumericModifier
            {
                operation = NumericModifierOperation.Add,
                value = 3,
                sourceId = "test.source"
            });

            Assert.AreEqual(2, ability.baseRoll);
            Assert.AreEqual(5, ability.attackResult);
            Assert.NotNull(ability.attackResultModifiers);
            Assert.AreEqual(1, ability.attackResultModifiers.Count);
            Assert.AreEqual(3, ability.attackResultModifiers[0].value);
            Assert.AreEqual("test.source", ability.attackResultModifiers[0].sourceId);
        }
    }
}
