using Game.Application.Battle;
using Game.Domain.Battle;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class BattlePhaseRunnerEditModeTests
    {
        [Test]
        public void AdvanceToNextPhase_FollowsExpectedOrder()
        {
            var state = new BattleState();
            var runner = new BattlePhaseRunner(state);

            Assert.IsTrue(runner.StartBattle());
            Assert.AreEqual(BattlePhase.Recall, runner.currentPhase);

            Assert.IsTrue(runner.AdvanceToNextPhase());
            Assert.AreEqual(BattlePhase.EnemyDeploy, runner.currentPhase);

            Assert.IsTrue(runner.AdvanceToNextPhase());
            Assert.AreEqual(BattlePhase.PlayerDeploy, runner.currentPhase);

            Assert.IsTrue(runner.AdvanceToNextPhase());
            Assert.AreEqual(BattlePhase.Roll, runner.currentPhase);

            Assert.IsTrue(runner.AdvanceToNextPhase());
            Assert.AreEqual(BattlePhase.Tactics, runner.currentPhase);

            Assert.IsTrue(runner.AdvanceToNextPhase());
            Assert.AreEqual(BattlePhase.Resolve, runner.currentPhase);
        }

        [Test]
        public void AdvanceToNextPhase_FromResolve_ReturnsToRecallAndIncrementsTurn()
        {
            var state = new BattleState();
            var runner = new BattlePhaseRunner(state);

            Assert.IsTrue(runner.StartBattle());

            Assert.IsTrue(runner.AdvanceToNextPhase()); // EnemyDeploy
            Assert.IsTrue(runner.AdvanceToNextPhase()); // PlayerDeploy
            Assert.IsTrue(runner.AdvanceToNextPhase()); // Roll
            Assert.IsTrue(runner.AdvanceToNextPhase()); // Tactics
            Assert.IsTrue(runner.AdvanceToNextPhase()); // Resolve

            int turnBefore = state.turnIndex;

            Assert.IsTrue(runner.AdvanceToNextPhase()); // Recall
            Assert.AreEqual(BattlePhase.Recall, runner.currentPhase);
            Assert.AreEqual(turnBefore + 1, state.turnIndex);
        }

        [Test]
        public void TryRetreat_SucceedsOnlyInPlayerDeployWithPositiveStability()
        {
            var state = new BattleState
            {
                stability = 1
            };
            var runner = new BattlePhaseRunner(state);

            Assert.IsTrue(runner.StartBattle());
            Assert.IsTrue(runner.AdvanceToNextPhase()); // EnemyDeploy
            Assert.IsTrue(runner.AdvanceToNextPhase()); // PlayerDeploy

            bool retreated = runner.TryRetreat();

            Assert.IsTrue(retreated);
            Assert.IsTrue(state.isBattleEnded);
            Assert.AreEqual(0, state.stability);
            Assert.AreEqual(BattlePhaseFailureReason.None, runner.LastFailureReason);
        }

        [Test]
        public void TryRetreat_FailsWhenPhaseIsNotPlayerDeploy()
        {
            var state = new BattleState
            {
                stability = 1
            };
            var runner = new BattlePhaseRunner(state);

            Assert.IsTrue(runner.StartBattle()); // Recall

            bool retreated = runner.TryRetreat();

            Assert.IsFalse(retreated);
            Assert.IsFalse(state.isBattleEnded);
            Assert.AreEqual(1, state.stability);
            Assert.AreEqual(BattlePhaseFailureReason.InvalidPhase, runner.LastFailureReason);
        }

        [Test]
        public void TryRetreat_FailsWhenStabilityIsZero()
        {
            var state = new BattleState
            {
                stability = 0
            };
            var runner = new BattlePhaseRunner(state);

            Assert.IsTrue(runner.StartBattle());
            Assert.IsTrue(runner.AdvanceToNextPhase()); // EnemyDeploy
            Assert.IsTrue(runner.AdvanceToNextPhase()); // PlayerDeploy

            bool retreated = runner.TryRetreat();

            Assert.IsFalse(retreated);
            Assert.IsFalse(state.isBattleEnded);
            Assert.AreEqual(0, state.stability);
            Assert.AreEqual(BattlePhaseFailureReason.StabilityInsufficient, runner.LastFailureReason);
        }
    }
}
