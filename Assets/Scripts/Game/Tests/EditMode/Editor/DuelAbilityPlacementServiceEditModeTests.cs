using Game.Application.Duel;
using Game.Domain.Duel;
using Game.Infrastructure.Data;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class DuelAbilityPlacementServiceEditModeTests
    {
        [Test]
        public void TryMoveAndReturnAbility_PlayerAndOpponentUseSameRules()
        {
            var service = new DuelAbilityPlacementService();
            var state = new DuelState();
            state.combats.Add(new CombatState());

            state.abilitiesById["p0"] = CreateAttack("ability.player", cooldownRemaining: 0);
            state.abilitiesById["e0"] = CreateAttack("ability.enemy", cooldownRemaining: 0);
            state.loadoutAbilityIds.Add("p0");
            state.opponentLoadoutAbilityIds.Add("e0");

            bool playerMoved = service.TryMoveAbilityToCombat(
                state,
                "p0",
                0,
                DuelSide.Player,
                out string playerMoveFailure);
            bool opponentMoved = service.TryMoveAbilityToCombat(
                state,
                "e0",
                0,
                DuelSide.Opponent,
                out string opponentMoveFailure);

            Assert.IsTrue(playerMoved, playerMoveFailure);
            Assert.IsTrue(opponentMoved, opponentMoveFailure);
            Assert.IsTrue(state.combats[0].playerAbilityIds.Contains("p0"));
            Assert.IsTrue(state.combats[0].opponentAbilityIds.Contains("e0"));

            bool playerReturned = service.TryReturnAbilityToLoadout(
                state,
                "p0",
                DuelSide.Player,
                out string playerReturnFailure);
            bool opponentReturned = service.TryReturnAbilityToLoadout(
                state,
                "e0",
                DuelSide.Opponent,
                out string opponentReturnFailure);

            Assert.IsTrue(playerReturned, playerReturnFailure);
            Assert.IsTrue(opponentReturned, opponentReturnFailure);
            Assert.IsTrue(state.loadoutAbilityIds.Contains("p0"));
            Assert.IsTrue(state.opponentLoadoutAbilityIds.Contains("e0"));
            Assert.IsFalse(state.combats[0].playerAbilityIds.Contains("p0"));
            Assert.IsFalse(state.combats[0].opponentAbilityIds.Contains("e0"));
        }

        [Test]
        public void TryMoveAbilityToCombat_FailsWhenAbilityIsOnCooldown()
        {
            var service = new DuelAbilityPlacementService();
            var state = new DuelState();
            state.combats.Add(new CombatState());

            state.abilitiesById["p0"] = CreateAttack("ability.player", cooldownRemaining: 1);
            state.abilitiesById["e0"] = CreateAttack("ability.enemy", cooldownRemaining: 1);
            state.loadoutAbilityIds.Add("p0");
            state.opponentLoadoutAbilityIds.Add("e0");

            bool playerSuccess = service.TryMoveAbilityToCombat(
                state,
                "p0",
                0,
                DuelSide.Player,
                out string playerFailure);
            bool opponentSuccess = service.TryMoveAbilityToCombat(
                state,
                "e0",
                0,
                DuelSide.Opponent,
                out string opponentFailure);

            Assert.IsFalse(playerSuccess);
            Assert.IsFalse(opponentSuccess);
            StringAssert.Contains("cooldown", playerFailure);
            StringAssert.Contains("cooldown", opponentFailure);
        }

        [Test]
        public void AutoDeployRandomFromLoadout_SkipsCooldownAbilities()
        {
            var service = new DuelAbilityPlacementService();
            var state = new DuelState();
            state.combats.Add(new CombatState());
            state.combats.Add(new CombatState());

            state.abilitiesById["e0"] = CreateAttack("ability.enemy.0", cooldownRemaining: 0);
            state.abilitiesById["e1"] = CreateAttack("ability.enemy.1", cooldownRemaining: 2);
            state.opponentLoadoutAbilityIds.Add("e0");
            state.opponentLoadoutAbilityIds.Add("e1");

            DuelAutoDeployResult result = service.AutoDeployRandomFromLoadout(
                state,
                DuelSide.Opponent,
                new System.Random(7));

            int deployedCount = state.combats[0].opponentAbilityIds.Count + state.combats[1].opponentAbilityIds.Count;
            Assert.AreEqual(1, result.deployedCount);
            Assert.AreEqual(1, deployedCount);
            Assert.IsTrue(state.opponentLoadoutAbilityIds.Contains("e1"));
            Assert.IsFalse(state.opponentLoadoutAbilityIds.Contains("e0"));
        }

        static AbilityInstance CreateAttack(string defId, int cooldownRemaining)
        {
            return new AbilityInstance
            {
                abilityDefId = defId,
                abilityType = AbilityType.Attack,
                cooldownTurns = 1,
                cooldownRemaining = cooldownRemaining,
                power = 3,
                baseRoll = 0,
                powerResult = 0
            };
        }
    }
}
