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
            state.actionsById["p1"] = new ActionInstance
            {
                actionDefId = "p1",
                attack = 6,
                baseRoll = 3,
                attackResult = 3
            };

            var resolver = new DuelEffectClashResolver();
            var command = new DuelEffectCommand
            {
                opCode = DuelEffectOpCode.ModifyAttackResult,
                actionId = "p1",
                modifierOperation = NumericModifierOperation.Add,
                amount = 2
            };

            DuelEffectResult result = resolver.Apply(state, command);

            Assert.IsTrue(result.isSuccess);
            Assert.AreEqual(5, state.actionsById["p1"].attackResult);
            Assert.AreEqual(1, state.actionsById["p1"].attackResultModifiers.Count);
        }

        [Test]
        public void AddAttackModifier_AffectsRollAttackRange()
        {
            var state = CreateDuelState();
            state.actionsById["p1"] = new ActionInstance
            {
                actionDefId = "p1",
                attack = 4
            };

            var resolver = new DuelEffectClashResolver();
            DuelEffectResult result = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.AddAttackModifier,
                    actionId = "p1",
                    modifierOperation = NumericModifierOperation.Add,
                    amount = 2
                });

            Assert.IsTrue(result.isSuccess);

            var fakeRollSource = new FakeRollSource(6);
            DuelSimulator.RollAction(state.actionsById["p1"], fakeRollSource);

            Assert.AreEqual(6, fakeRollSource.lastMaxInclusive);
            Assert.AreEqual(6, state.actionsById["p1"].baseRoll);
        }

        [Test]
        public void MoveAction_Succeeds_WhenTargetHasSpace()
        {
            var state = CreateDuelState();
            state.actionsById["p1"] = CreateAction("p1", 2);
            state.clashes[0].playerActionIds.Add("p1");

            var resolver = new DuelEffectClashResolver();
            DuelEffectResult result = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.MoveAction,
                    actionId = "p1",
                    toClashIndex = 1
                });

            Assert.IsTrue(result.isSuccess);
            Assert.IsFalse(state.clashes[0].playerActionIds.Contains("p1"));
            Assert.IsTrue(state.clashes[1].playerActionIds.Contains("p1"));
        }

        [Test]
        public void MoveAction_Fails_WhenSlotLimitExceeded()
        {
            var state = CreateDuelState();
            state.actionsById["p1"] = CreateAction("p1", 2);
            state.actionsById["p2"] = CreateAction("p2", 2);
            state.clashes[0].playerActionIds.Add("p1");
            state.clashes[1].playerActionIds.Add("p2");
            state.clashes[1].slotLimit = 1;

            var resolver = new DuelEffectClashResolver();
            DuelEffectResult result = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.MoveAction,
                    actionId = "p1",
                    toClashIndex = 1
                });

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual(DuelEffectFailureReason.SlotLimitExceeded, result.failureReason);
            Assert.IsTrue(state.clashes[0].playerActionIds.Contains("p1"));
            Assert.IsFalse(state.clashes[1].playerActionIds.Contains("p1"));
        }

        [Test]
        public void MoveOpponentAction_Succeeds_WhenTargetHasSpace()
        {
            var state = CreateDuelState();
            state.actionsById["e1"] = CreateAction("e1", 3);
            state.clashes[0].opponentActionIds.Add("e1");

            var resolver = new DuelEffectClashResolver();
            DuelEffectResult result = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.MoveOpponentAction,
                    actionId = "e1",
                    toClashIndex = 2
                });

            Assert.IsTrue(result.isSuccess);
            Assert.IsFalse(state.clashes[0].opponentActionIds.Contains("e1"));
            Assert.IsTrue(state.clashes[2].opponentActionIds.Contains("e1"));
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
        public void TransformOutcome_AppliesRiskyAndSafeRules()
        {
            var state = CreateDuelState();
            var resolver = new DuelEffectClashResolver();
            var context = new DuelEffectContext
            {
                hasOutcome = true,
                outcome = DuelOutcome.Victory
            };

            DuelEffectResult riskyResult = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.TransformOutcome,
                    transformKind = DuelOutcomeTransformKind.Risky
                },
                context);

            Assert.IsTrue(riskyResult.isSuccess);
            Assert.AreEqual(DuelOutcome.GreatVictory, context.outcome);

            context.outcome = DuelOutcome.GreatDefeat;

            DuelEffectResult safeResult = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.TransformOutcome,
                    transformKind = DuelOutcomeTransformKind.Safe
                },
                context);

            Assert.IsTrue(safeResult.isSuccess);
            Assert.AreEqual(DuelOutcome.Defeat, context.outcome);
        }

        [Test]
        public void ModifyHealth_EndsDuelAndClearsDuelLayerModifiers()
        {
            var state = CreateDuelState();
            state.playerHealth = 1;
            state.actionsById["p1"] = new ActionInstance
            {
                actionDefId = "p1",
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
            Assert.AreEqual(1, state.actionsById["p1"].attackModifiers.Count);
            Assert.AreEqual(ModifierLayer.Permanent, state.actionsById["p1"].attackModifiers[0].layer);
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
                        opCode = DuelEffectOpCode.MoveAction,
                        actionId = "missing",
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

        [Test]
        public void TransformOutcome_FailsWithoutOutcomeContext()
        {
            var state = CreateDuelState();
            var resolver = new DuelEffectClashResolver();
            DuelEffectResult result = resolver.Apply(
                state,
                new DuelEffectCommand
                {
                    opCode = DuelEffectOpCode.TransformOutcome,
                    transformKind = DuelOutcomeTransformKind.Risky
                });

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual(DuelEffectFailureReason.MissingOutcomeContext, result.failureReason);
        }

        static DuelState CreateDuelState()
        {
            return new DuelState
            {
                playerHealth = 10,
                opponentHealth = 10
            };
        }

        static ActionInstance CreateAction(string actionId, int attackResult)
        {
            return new ActionInstance
            {
                actionDefId = actionId,
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
