using System.Collections.Generic;
using Game.Application.Duel.Effects;
using Game.Domain.Duel;
using Game.Domain.Modifiers;
using Game.Infrastructure.Data;
using Game.Infrastructure.Data.Effects;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class DuelEffectCombatResolverEditModeTests
    {
        [Test]
        public void ModifyPowerResult_RecomputesCurrentPowerResult()
        {
            var state = CreateDuelState();
            state.abilitiesById["p1"] = new AbilityInstance
            {
                abilityDefId = "p1",
                abilityType = AbilityType.Attack,
                power = 6,
                baseRoll = 3,
                powerResult = 3
            };

            var resolver = new DuelEffectCombatResolver();
            var command = new DuelEffectCommand
            {
                opCode = DuelEffectOpCode.ModifyPowerResult,
                abilityId = "p1",
                modifierOperation = NumericModifierOperation.Add,
                amount = 2
            };

            DuelEffectResult result = resolver.Apply(state, command);

            Assert.IsTrue(result.isSuccess);
            Assert.AreEqual(5, state.abilitiesById["p1"].powerResult);
            Assert.AreEqual(1, state.abilitiesById["p1"].powerResultModifiers.Count);
        }

        [Test]
        public void AddPowerModifier_AffectsRollPowerRange()
        {
            var state = CreateDuelState();
            state.abilitiesById["p1"] = new AbilityInstance
            {
                abilityDefId = "p1",
                abilityType = AbilityType.Attack,
                power = 4
            };

            var resolver = new DuelEffectCombatResolver();
            DuelEffectResult result = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.AddPowerModifier,
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
            state.combats[0].playerAbilityIds.Add("p1");

            var resolver = new DuelEffectCombatResolver();
            DuelEffectResult result = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.MoveAbility,
                    abilityId = "p1",
                    toCombatIndex = 1
                });

            Assert.IsTrue(result.isSuccess);
            Assert.IsFalse(state.combats[0].playerAbilityIds.Contains("p1"));
            Assert.IsTrue(state.combats[1].playerAbilityIds.Contains("p1"));
        }

        [Test]
        public void MoveAbility_Fails_WhenMaxPlayerAssignmentsExceeded()
        {
            var state = CreateDuelState();
            state.abilitiesById["p1"] = CreateAbility("p1", 2);
            state.abilitiesById["p2"] = CreateAbility("p2", 2);
            state.combats[0].playerAbilityIds.Add("p1");
            state.combats[1].playerAbilityIds.Add("p2");
            state.combats[1].maxPlayerAssignments = 1;

            var resolver = new DuelEffectCombatResolver();
            DuelEffectResult result = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.MoveAbility,
                    abilityId = "p1",
                    toCombatIndex = 1
                });

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual(DuelEffectFailureReason.SlotLimitExceeded, result.failureReason);
            Assert.IsTrue(state.combats[0].playerAbilityIds.Contains("p1"));
            Assert.IsFalse(state.combats[1].playerAbilityIds.Contains("p1"));
        }

        [Test]
        public void MoveOpponentAbility_Succeeds_WhenTargetHasSpace()
        {
            var state = CreateDuelState();
            state.abilitiesById["e1"] = CreateAbility("e1", 3);
            state.combats[0].opponentAbilityIds.Add("e1");

            var resolver = new DuelEffectCombatResolver();
            DuelEffectResult result = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.MoveOpponentAbility,
                    abilityId = "e1",
                    toCombatIndex = 2
                });

            Assert.IsTrue(result.isSuccess);
            Assert.IsFalse(state.combats[0].opponentAbilityIds.Contains("e1"));
            Assert.IsTrue(state.combats[2].opponentAbilityIds.Contains("e1"));
        }

        [Test]
        public void ModifyTotalPower_UpdatesRequestedSide()
        {
            var state = CreateDuelState();
            var resolver = new DuelEffectCombatResolver();

            DuelEffectResult playerResult = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.ModifyTotalPower,
                    combatIndex = 0,
                    isPlayerSide = true,
                    amount = 2
                });

            DuelEffectResult opponentResult = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.ModifyTotalPower,
                    combatIndex = 0,
                    isPlayerSide = false,
                    amount = -1
                });

            Assert.IsTrue(playerResult.isSuccess);
            Assert.IsTrue(opponentResult.isSuccess);
            Assert.AreEqual(2, state.combats[0].totalPowerBonusPlayer);
            Assert.AreEqual(-1, state.combats[0].totalPowerBonusOpponent);
        }

        [Test]
        public void Apply_FailsWhenUnsupportedOpCode()
        {
            var state = CreateDuelState();
            var resolver = new DuelEffectCombatResolver();
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
                abilityType = AbilityType.Attack,
                power = 4,
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
            };

            var resolver = new DuelEffectCombatResolver();
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
            Assert.AreEqual(1, state.abilitiesById["p1"].powerModifiers.Count);
            Assert.AreEqual(ModifierLayer.Permanent, state.abilitiesById["p1"].powerModifiers[0].layer);
        }

        [Test]
        public void ApplyAll_ContinuesAfterFailure()
        {
            var state = CreateDuelState();
            var resolver = new DuelEffectCombatResolver();
            List<DuelEffectResult> results = resolver.ApplyAll(
                state,
                new List<DuelEffectCommand>
                {
                    new DuelEffectCommand
                    {
                        opCode = DuelEffectOpCode.MoveAbility,
                        abilityId = "missing",
                        toCombatIndex = 1
                    },
                    new DuelEffectCommand
                    {
                        opCode = DuelEffectOpCode.ModifyTotalPower,
                        combatIndex = 0,
                        isPlayerSide = true,
                        amount = 2
                    }
                });

            Assert.AreEqual(2, results.Count);
            Assert.IsFalse(results[0].isSuccess);
            Assert.IsTrue(results[1].isSuccess);
            Assert.AreEqual(2, state.combats[0].totalPowerBonusPlayer);
        }

        [Test]
        public void Apply_FailsWhenDuelAlreadyEnded()
        {
            var state = CreateDuelState();
            state.isDuelEnded = true;

            var resolver = new DuelEffectCombatResolver();
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

            AddCombats(state, 3);
            return state;
        }

        static void AddCombats(DuelState state, int count)
        {
            state.combats.Clear();
            for (int i = 0; i < count; i++)
            {
                state.combats.Add(new CombatState());
            }
        }

        static AbilityInstance CreateAbility(string abilityId, int powerResult)
        {
            return new AbilityInstance
            {
                abilityDefId = abilityId,
                abilityType = AbilityType.Attack,
                power = 6,
                baseRoll = powerResult,
                powerResult = powerResult
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


