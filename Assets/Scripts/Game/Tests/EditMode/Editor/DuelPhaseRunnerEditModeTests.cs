using Game.Application.Duel;
using Game.Domain.Duel;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class DuelPhaseRunnerEditModeTests
    {
        [Test]
        public void AdvanceToNextPhase_FollowsExpectedOrder()
        {
            var state = new DuelState();
            var runner = new DuelPhaseRunner(state);

            Assert.IsTrue(runner.StartDuel());
            Assert.AreEqual(DuelPhase.Reset, runner.currentPhase);

            Assert.IsTrue(runner.AdvanceToNextPhase());
            Assert.AreEqual(DuelPhase.OpponentSetup, runner.currentPhase);

            Assert.IsTrue(runner.AdvanceToNextPhase());
            Assert.AreEqual(DuelPhase.PlayerSetup, runner.currentPhase);

            Assert.IsTrue(runner.AdvanceToNextPhase());
            Assert.AreEqual(DuelPhase.Roll, runner.currentPhase);

            Assert.IsTrue(runner.AdvanceToNextPhase());
            Assert.AreEqual(DuelPhase.Resolve, runner.currentPhase);

            Assert.IsTrue(runner.AdvanceToNextPhase());
            Assert.AreEqual(DuelPhase.Reset, runner.currentPhase);
        }

        [Test]
        public void AdvanceToNextPhase_FromResolve_ReturnsToResetAndIncrementsTurn()
        {
            var state = new DuelState();
            var runner = new DuelPhaseRunner(state);

            Assert.IsTrue(runner.StartDuel());

            Assert.IsTrue(runner.AdvanceToNextPhase()); // OpponentSetup
            Assert.IsTrue(runner.AdvanceToNextPhase()); // PlayerSetup
            Assert.IsTrue(runner.AdvanceToNextPhase()); // Roll
            Assert.IsTrue(runner.AdvanceToNextPhase()); // Resolve

            int turnBefore = state.turnIndex;

            Assert.IsTrue(runner.AdvanceToNextPhase()); // Reset
            Assert.AreEqual(DuelPhase.Reset, runner.currentPhase);
            Assert.AreEqual(turnBefore + 1, state.turnIndex);
        }

        [Test]
        public void TrySurrender_SucceedsOnlyInPlayerSetupWithPositiveHonor()
        {
            var state = new DuelState
            {
                honor = 1
            };
            var runner = new DuelPhaseRunner(state);

            Assert.IsTrue(runner.StartDuel());
            Assert.IsTrue(runner.AdvanceToNextPhase()); // OpponentSetup
            Assert.IsTrue(runner.AdvanceToNextPhase()); // PlayerSetup

            bool surrendered = runner.TrySurrender();

            Assert.IsTrue(surrendered);
            Assert.IsTrue(state.isDuelEnded);
            Assert.AreEqual(0, state.honor);
            Assert.AreEqual(DuelPhaseFailureReason.None, runner.LastFailureReason);
        }

        [Test]
        public void TrySurrender_FailsWhenPhaseIsNotPlayerSetup()
        {
            var state = new DuelState
            {
                honor = 1
            };
            var runner = new DuelPhaseRunner(state);

            Assert.IsTrue(runner.StartDuel()); // Reset

            bool surrendered = runner.TrySurrender();

            Assert.IsFalse(surrendered);
            Assert.IsFalse(state.isDuelEnded);
            Assert.AreEqual(1, state.honor);
            Assert.AreEqual(DuelPhaseFailureReason.InvalidPhase, runner.LastFailureReason);
        }

        [Test]
        public void TrySurrender_FailsWhenHonorIsZero()
        {
            var state = new DuelState
            {
                honor = 0
            };
            var runner = new DuelPhaseRunner(state);

            Assert.IsTrue(runner.StartDuel());
            Assert.IsTrue(runner.AdvanceToNextPhase()); // OpponentSetup
            Assert.IsTrue(runner.AdvanceToNextPhase()); // PlayerSetup

            bool surrendered = runner.TrySurrender();

            Assert.IsFalse(surrendered);
            Assert.IsFalse(state.isDuelEnded);
            Assert.AreEqual(0, state.honor);
            Assert.AreEqual(DuelPhaseFailureReason.HonorInsufficient, runner.LastFailureReason);
        }
    }
}
