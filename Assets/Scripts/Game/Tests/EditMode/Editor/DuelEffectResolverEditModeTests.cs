using System.Collections.Generic;
using Game.Application.Duel.Effects;
using Game.Domain.Duel;
using Game.Domain.Modifiers;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class DuelEffectClashResolverEditModeTests
    {
        [Test]
        public void ModifyAttackResult_RecomputesCurrentAttackResult()
        {
            var state = CreateDuelState();
            state.abilitiesById["p1"] = new AbilityInstance
            {
                abilityDefId = "p1",
                attack = 6,
                baseRoll = 3,
                attackResult = 3
            };

            var resolver = new DuelEffectClashResolver();
            var command = new DuelEffectCommand
            {
                opCode = DuelEffectOpCode.ModifyAttackResult,
                abilityId = "p1",
                modifierOperation = NumericModifierOperation.Add,
                amount = 2
            };

            DuelEffectResult result = resolver.Apply(state, command);

            Assert.IsTrue(result.isSuccess);
            Assert.AreEqual(5, state.abilitiesById["p1"].attackResult);
            Assert.AreEqual(1, state.abilitiesById["p1"].attackResultModifiers.Count);
        }

        [Test]
        public void AddAttackModifier_AffectsRollAttackRange()
        {
            var state = CreateDuelState();
            state.abilitiesById["p1"] = new AbilityInstance
            {
                abilityDefId = "p1",
                attack = 4
            };

            var resolver = new DuelEffectClashResolver();
            DuelEffectResult result = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.AddAttackModifier,
                    abilityId = "p1",
                    modifierOperation = NumericModifierOperation.Add,
                    amount = 2
                });

            Assert.IsTrue(result.isSuccess);

            var fakeRollSource = new FakeRollSource(6);
            DuelSimulator.RollAbility(state.abilitiesById["p1"], fakeRollSource);

            Assert.AreEqual(6, fakeRollSource.lastMaxInclusive);
            Assert.AreEqual(6, state.abilitiesById["p1"].baseRoll);
        }

        [Test]
        public void MoveAbility_Succeeds_WhenTargetHasSpace()
        {
            var state = CreateDuelState();
            state.abilitiesById["p1"] = CreateAbility("p1", 2);
            state.clashes[0].playerAbilityIds.Add("p1");

            var resolver = new DuelEffectClashResolver();
            DuelEffectResult result = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.MoveAbility,
                    abilityId = "p1",
                    toClashIndex = 1
                });

            Assert.IsTrue(result.isSuccess);
            Assert.IsFalse(state.clashes[0].playerAbilityIds.Contains("p1"));
            Assert.IsTrue(state.clashes[1].playerAbilityIds.Contains("p1"));
        }

        [Test]
        public void MoveAbility_Fails_WhenSlotLimitExceeded()
        {
            var state = CreateDuelState();
            state.abilitiesById["p1"] = CreateAbility("p1", 2);
            state.abilitiesById["p2"] = CreateAbility("p2", 2);
            state.clashes[0].playerAbilityIds.Add("p1");
            state.clashes[1].playerAbilityIds.Add("p2");
            state.clashes[1].slotLimit = 1;

            var resolver = new DuelEffectClashResolver();
            DuelEffectResult result = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.MoveAbility,
                    abilityId = "p1",
                    toClashIndex = 1
                });

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual(DuelEffectFailureReason.SlotLimitExceeded, result.failureReason);
            Assert.IsTrue(state.clashes[0].playerAbilityIds.Contains("p1"));
            Assert.IsFalse(state.clashes[1].playerAbilityIds.Contains("p1"));
        }

        [Test]
        public void MoveOpponentAbility_Succeeds_WhenTargetHasSpace()
        {
            var state = CreateDuelState();
            state.abilitiesById["e1"] = CreateAbility("e1", 3);
            state.clashes[0].opponentAbilityIds.Add("e1");

            var resolver = new DuelEffectClashResolver();
            DuelEffectResult result = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.MoveOpponentAbility,
                    abilityId = "e1",
                    toClashIndex = 2
                });

            Assert.IsTrue(result.isSuccess);
            Assert.IsFalse(state.clashes[0].opponentAbilityIds.Contains("e1"));
            Assert.IsTrue(state.clashes[2].opponentAbilityIds.Contains("e1"));
        }

        [Test]
        public void ModifyTotalAttack_UpdatesRequestedSide()
        {
            var state = CreateDuelState();
            var resolver = new DuelEffectClashResolver();

            DuelEffectResult playerResult = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.ModifyTotalAttack,
                    clashIndex = 0,
                    isPlayerSide = true,
                    amount = 2
                });

            DuelEffectResult opponentResult = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.ModifyTotalAttack,
                    clashIndex = 0,
                    isPlayerSide = false,
                    amount = -1
                });

            Assert.IsTrue(playerResult.isSuccess);
            Assert.IsTrue(opponentResult.isSuccess);
            Assert.AreEqual(2, state.clashes[0].totalAttackBonusPlayer);
            Assert.AreEqual(-1, state.clashes[0].totalAttackBonusOpponent);
        }

        [Test]
        public void Apply_FailsWhenUnsupportedOpCode()
        {
            var state = CreateDuelState();
            var resolver = new DuelEffectClashResolver();
            DuelEffectResult result = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = (DuelEffectOpCode)999
                });

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual(DuelEffectFailureReason.UnsupportedOpCode, result.failureReason);
        }

        [Test]
        public void ModifyHealth_EndsDuelAndClearsDuelLayerModifiers()
        {
            var state = CreateDuelState();
            state.playerHealth = 1;
            state.abilitiesById["p1"] = new AbilityInstance
            {
                abilityDefId = "p1",
                attack = 4,
                attackModifiers = new List<NumericModifier>
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
            };

            var resolver = new DuelEffectClashResolver();
            DuelEffectResult result = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.ModifyHealth,
                    isPlayerSide = true,
                    amount = -2
                });

            Assert.IsTrue(result.isSuccess);
            Assert.IsTrue(state.isDuelEnded);
            Assert.AreEqual(-1, state.playerHealth);
            Assert.AreEqual(1, state.abilitiesById["p1"].attackModifiers.Count);
            Assert.AreEqual(ModifierLayer.Permanent, state.abilitiesById["p1"].attackModifiers[0].layer);
        }

        [Test]
        public void ApplyAll_ContinuesAfterFailure()
        {
            var state = CreateDuelState();
            var resolver = new DuelEffectClashResolver();
            List<DuelEffectResult> results = resolver.ApplyAll(
                state,
                new List<DuelEffectCommand>
                {
                    new DuelEffectCommand
                    {
                        opCode = DuelEffectOpCode.MoveAbility,
                        abilityId = "missing",
                        toClashIndex = 1
                    },
                    new DuelEffectCommand
                    {
                        opCode = DuelEffectOpCode.ModifyTotalAttack,
                        clashIndex = 0,
                        isPlayerSide = true,
                        amount = 2
                    }
                });

            Assert.AreEqual(2, results.Count);
            Assert.IsFalse(results[0].isSuccess);
            Assert.IsTrue(results[1].isSuccess);
            Assert.AreEqual(2, state.clashes[0].totalAttackBonusPlayer);
        }

        [Test]
        public void Apply_FailsWhenDuelAlreadyEnded()
        {
            var state = CreateDuelState();
            state.isDuelEnded = true;

            var resolver = new DuelEffectClashResolver();
            DuelEffectResult result = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.ModifyHealth,
                    amount = -1
                });

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual(DuelEffectFailureReason.DuelEnded, result.failureReason);
        }

        static DuelState CreateDuelState()
        {
            var state = new DuelState
            {
                playerHealth = 10,
                opponentHealth = 10
            };

            AddClashes(state, 3);
            return state;
        }

        static void AddClashes(DuelState state, int count)
        {
            state.clashes.Clear();
            for (int i = 0; i < count; i++)
            {
                state.clashes.Add(new ClashState());
            }
        }

        static AbilityInstance CreateAbility(string abilityId, int attackResult)
        {
            return new AbilityInstance
            {
                abilityDefId = abilityId,
                attack = 6,
                baseRoll = attackResult,
                attackResult = attackResult
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


