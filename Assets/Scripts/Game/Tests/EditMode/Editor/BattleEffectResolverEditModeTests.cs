using System.Collections.Generic;
using Game.Application.Battle.Effects;
using Game.Domain.Battle;
using Game.Domain.Modifiers;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class BattleEffectResolverEditModeTests
    {
        [Test]
        public void ModifyAttackResult_RecomputesCurrentAttackResult()
        {
            var state = CreateBattleState();
            state.troopsById["p1"] = new TroopInstance
            {
                troopDefId = "p1",
                attack = 6,
                baseRoll = 3,
                attackResult = 3
            };

            var resolver = new BattleEffectResolver();
            var command = new BattleEffectCommand
            {
                opCode = BattleEffectOpCode.ModifyAttackResult,
                troopId = "p1",
                modifierOperation = NumericModifierOperation.Add,
                amount = 2
            };

            BattleEffectResult result = resolver.Apply(state, command);

            Assert.IsTrue(result.isSuccess);
            Assert.AreEqual(5, state.troopsById["p1"].attackResult);
            Assert.AreEqual(1, state.troopsById["p1"].attackResultModifiers.Count);
        }

        [Test]
        public void AddAttackModifier_AffectsRollAttackRange()
        {
            var state = CreateBattleState();
            state.troopsById["p1"] = new TroopInstance
            {
                troopDefId = "p1",
                attack = 4
            };

            var resolver = new BattleEffectResolver();
            BattleEffectResult result = resolver.Apply(
                state,
                new BattleEffectCommand
                {
                    opCode = BattleEffectOpCode.AddAttackModifier,
                    troopId = "p1",
                    modifierOperation = NumericModifierOperation.Add,
                    amount = 2
                });

            Assert.IsTrue(result.isSuccess);

            var fakeRollSource = new FakeRollSource(6);
            BattleSimulator.RollTroop(state.troopsById["p1"], fakeRollSource);

            Assert.AreEqual(6, fakeRollSource.lastMaxInclusive);
            Assert.AreEqual(6, state.troopsById["p1"].baseRoll);
        }

        [Test]
        public void MoveTroop_Succeeds_WhenTargetHasSpace()
        {
            var state = CreateBattleState();
            state.troopsById["p1"] = CreateTroop("p1", 2);
            state.battlefields[0].playerTroopIds.Add("p1");

            var resolver = new BattleEffectResolver();
            BattleEffectResult result = resolver.Apply(
                state,
                new BattleEffectCommand
                {
                    opCode = BattleEffectOpCode.MoveTroop,
                    troopId = "p1",
                    toBattlefieldIndex = 1
                });

            Assert.IsTrue(result.isSuccess);
            Assert.IsFalse(state.battlefields[0].playerTroopIds.Contains("p1"));
            Assert.IsTrue(state.battlefields[1].playerTroopIds.Contains("p1"));
        }

        [Test]
        public void MoveTroop_Fails_WhenSlotLimitExceeded()
        {
            var state = CreateBattleState();
            state.troopsById["p1"] = CreateTroop("p1", 2);
            state.troopsById["p2"] = CreateTroop("p2", 2);
            state.battlefields[0].playerTroopIds.Add("p1");
            state.battlefields[1].playerTroopIds.Add("p2");
            state.battlefields[1].slotLimit = 1;

            var resolver = new BattleEffectResolver();
            BattleEffectResult result = resolver.Apply(
                state,
                new BattleEffectCommand
                {
                    opCode = BattleEffectOpCode.MoveTroop,
                    troopId = "p1",
                    toBattlefieldIndex = 1
                });

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual(BattleEffectFailureReason.SlotLimitExceeded, result.failureReason);
            Assert.IsTrue(state.battlefields[0].playerTroopIds.Contains("p1"));
            Assert.IsFalse(state.battlefields[1].playerTroopIds.Contains("p1"));
        }

        [Test]
        public void MoveEnemyTroop_Succeeds_WhenTargetHasSpace()
        {
            var state = CreateBattleState();
            state.troopsById["e1"] = CreateTroop("e1", 3);
            state.battlefields[0].enemyTroopIds.Add("e1");

            var resolver = new BattleEffectResolver();
            BattleEffectResult result = resolver.Apply(
                state,
                new BattleEffectCommand
                {
                    opCode = BattleEffectOpCode.MoveEnemyTroop,
                    troopId = "e1",
                    toBattlefieldIndex = 2
                });

            Assert.IsTrue(result.isSuccess);
            Assert.IsFalse(state.battlefields[0].enemyTroopIds.Contains("e1"));
            Assert.IsTrue(state.battlefields[2].enemyTroopIds.Contains("e1"));
        }

        [Test]
        public void ModifyTotalAttack_UpdatesRequestedSide()
        {
            var state = CreateBattleState();
            var resolver = new BattleEffectResolver();

            BattleEffectResult playerResult = resolver.Apply(
                state,
                new BattleEffectCommand
                {
                    opCode = BattleEffectOpCode.ModifyTotalAttack,
                    battlefieldIndex = 0,
                    isPlayerSide = true,
                    amount = 2
                });

            BattleEffectResult enemyResult = resolver.Apply(
                state,
                new BattleEffectCommand
                {
                    opCode = BattleEffectOpCode.ModifyTotalAttack,
                    battlefieldIndex = 0,
                    isPlayerSide = false,
                    amount = -1
                });

            Assert.IsTrue(playerResult.isSuccess);
            Assert.IsTrue(enemyResult.isSuccess);
            Assert.AreEqual(2, state.battlefields[0].totalAttackBonusPlayer);
            Assert.AreEqual(-1, state.battlefields[0].totalAttackBonusEnemy);
        }

        [Test]
        public void TransformOutcome_AppliesRiskyAndSafeRules()
        {
            var state = CreateBattleState();
            var resolver = new BattleEffectResolver();
            var context = new BattleEffectContext
            {
                hasOutcome = true,
                outcome = BattleOutcome.Victory
            };

            BattleEffectResult riskyResult = resolver.Apply(
                state,
                new BattleEffectCommand
                {
                    opCode = BattleEffectOpCode.TransformOutcome,
                    transformKind = BattleOutcomeTransformKind.Risky
                },
                context);

            Assert.IsTrue(riskyResult.isSuccess);
            Assert.AreEqual(BattleOutcome.GreatVictory, context.outcome);

            context.outcome = BattleOutcome.GreatDefeat;

            BattleEffectResult safeResult = resolver.Apply(
                state,
                new BattleEffectCommand
                {
                    opCode = BattleEffectOpCode.TransformOutcome,
                    transformKind = BattleOutcomeTransformKind.Safe
                },
                context);

            Assert.IsTrue(safeResult.isSuccess);
            Assert.AreEqual(BattleOutcome.Defeat, context.outcome);
        }

        [Test]
        public void ModifyMorale_EndsBattleAndClearsBattleLayerModifiers()
        {
            var state = CreateBattleState();
            state.playerMorale = 1;
            state.troopsById["p1"] = new TroopInstance
            {
                troopDefId = "p1",
                attack = 4,
                attackModifiers = new List<NumericModifier>
                {
                    new NumericModifier
                    {
                        layer = ModifierLayer.Battle,
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

            var resolver = new BattleEffectResolver();
            BattleEffectResult result = resolver.Apply(
                state,
                new BattleEffectCommand
                {
                    opCode = BattleEffectOpCode.ModifyMorale,
                    isPlayerSide = true,
                    amount = -2
                });

            Assert.IsTrue(result.isSuccess);
            Assert.IsTrue(state.isBattleEnded);
            Assert.AreEqual(-1, state.playerMorale);
            Assert.AreEqual(1, state.troopsById["p1"].attackModifiers.Count);
            Assert.AreEqual(ModifierLayer.Permanent, state.troopsById["p1"].attackModifiers[0].layer);
        }

        [Test]
        public void ApplyAll_ContinuesAfterFailure()
        {
            var state = CreateBattleState();
            var resolver = new BattleEffectResolver();
            List<BattleEffectResult> results = resolver.ApplyAll(
                state,
                new List<BattleEffectCommand>
                {
                    new BattleEffectCommand
                    {
                        opCode = BattleEffectOpCode.MoveTroop,
                        troopId = "missing",
                        toBattlefieldIndex = 1
                    },
                    new BattleEffectCommand
                    {
                        opCode = BattleEffectOpCode.ModifyTotalAttack,
                        battlefieldIndex = 0,
                        isPlayerSide = true,
                        amount = 2
                    }
                });

            Assert.AreEqual(2, results.Count);
            Assert.IsFalse(results[0].isSuccess);
            Assert.IsTrue(results[1].isSuccess);
            Assert.AreEqual(2, state.battlefields[0].totalAttackBonusPlayer);
        }

        [Test]
        public void Apply_FailsWhenBattleAlreadyEnded()
        {
            var state = CreateBattleState();
            state.isBattleEnded = true;

            var resolver = new BattleEffectResolver();
            BattleEffectResult result = resolver.Apply(
                state,
                new BattleEffectCommand
                {
                    opCode = BattleEffectOpCode.ModifyMorale,
                    amount = -1
                });

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual(BattleEffectFailureReason.BattleEnded, result.failureReason);
        }

        [Test]
        public void TransformOutcome_FailsWithoutOutcomeContext()
        {
            var state = CreateBattleState();
            var resolver = new BattleEffectResolver();
            BattleEffectResult result = resolver.Apply(
                state,
                new BattleEffectCommand
                {
                    opCode = BattleEffectOpCode.TransformOutcome,
                    transformKind = BattleOutcomeTransformKind.Risky
                });

            Assert.IsFalse(result.isSuccess);
            Assert.AreEqual(BattleEffectFailureReason.MissingOutcomeContext, result.failureReason);
        }

        static BattleState CreateBattleState()
        {
            return new BattleState
            {
                playerMorale = 10,
                enemyMorale = 10
            };
        }

        static TroopInstance CreateTroop(string troopId, int attackResult)
        {
            return new TroopInstance
            {
                troopDefId = troopId,
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
