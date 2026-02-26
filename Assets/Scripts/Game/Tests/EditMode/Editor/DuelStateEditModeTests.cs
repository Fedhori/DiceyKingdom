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

            Assert.NotNull(state.combats);
            Assert.NotNull(state.abilitiesById);
            Assert.NotNull(state.loadoutAbilityIds);
            Assert.NotNull(state.opponentLoadoutAbilityIds);
        }

        [Test]
        public void DuelState_DefaultInitialization_DoesNotCreateImplicitCombats()
        {
            var state = new DuelState();

            Assert.AreEqual(0, state.combats.Count);
        }

        [Test]
        public void AbilityInstance_DefaultInitialization_BaseModFinalFieldsAreAccessible()
        {
            var ability = new AbilityInstance();

            ability.baseRoll = 2;
            ability.powerResult = 5;
            ability.powerResultModifiers.Add(new NumericModifier
            {
                operation = NumericModifierOperation.Add,
                value = 3,
                sourceId = "test.source"
            });

            Assert.AreEqual(2, ability.baseRoll);
            Assert.AreEqual(5, ability.powerResult);
            Assert.NotNull(ability.powerResultModifiers);
            Assert.AreEqual(1, ability.powerResultModifiers.Count);
            Assert.AreEqual(3, ability.powerResultModifiers[0].value);
            Assert.AreEqual("test.source", ability.powerResultModifiers[0].sourceId);
        }
    }
}

