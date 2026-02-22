using Game.Domain.Battle;
using Game.Infrastructure.Data;
using Game.Presentation.Debug;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class BattleDebugPanelFormatterEditModeTests
    {
        [Test]
        public void FormatBattlefield_ValidField_IncludesTotalAttackAndTroopCounts()
        {
            BattleState state = CreateBattleStateForFormatterTests();

            string line = BattleDebugPanelFormatter.FormatBattlefield(state, 0);

            StringAssert.Contains("Battlefield 0 (bf_0)", line);
            StringAssert.Contains("TotalAttack P:6 E:8", line);
            StringAssert.Contains("Troops P:1 E:1", line);
            StringAssert.Contains("Slot:2", line);
            StringAssert.DoesNotContain("Players:", line);
            StringAssert.DoesNotContain("Enemies:", line);
        }

        [Test]
        public void FormatSelectedTroop_WhenNoSelection_ReturnsNone()
        {
            BattleState state = CreateBattleStateForFormatterTests();

            string line = BattleDebugPanelFormatter.FormatSelectedTroop(state, string.Empty);

            Assert.AreEqual("Selected Troop: (none)", line);
        }

        [Test]
        public void FormatSelectedTroop_WhenTroopIsMissing_ReturnsMissing()
        {
            BattleState state = CreateBattleStateForFormatterTests();

            string line = BattleDebugPanelFormatter.FormatSelectedTroop(state, "missing_troop");

            Assert.AreEqual("Selected Troop: (missing)", line);
        }

        [Test]
        public void FormatSelectedTroop_WhenTroopIsInCamp_ReturnsCampLocation()
        {
            BattleState state = CreateBattleStateForFormatterTests();

            string line = BattleDebugPanelFormatter.FormatSelectedTroop(state, "camp_1");

            StringAssert.Contains("Selected Troop: troop.camp", line);
            StringAssert.Contains("Attack Result:2", line);
            StringAssert.Contains("camp", line);
        }

        [Test]
        public void FormatCampTroops_WhenCampTroopExists_IncludesCampDefIdOnly()
        {
            BattleState state = CreateBattleStateForFormatterTests();

            string line = BattleDebugPanelFormatter.FormatCampTroops(state, "camp_1");

            StringAssert.Contains("Selected Troop: troop.camp", line);
            StringAssert.Contains("Camp Troops (1):", line);
            StringAssert.Contains("- troop.camp | Attack:2 | Attack Result:2 <selected>", line);
            StringAssert.DoesNotContain("camp_1", line);
        }

        [Test]
        public void FormatSelectedTroop_WhenTroopIsDeployed_ReturnsBattlefieldLocation()
        {
            BattleState state = CreateBattleStateForFormatterTests();

            string playerLine = BattleDebugPanelFormatter.FormatSelectedTroop(state, "p_1");
            string enemyLine = BattleDebugPanelFormatter.FormatSelectedTroop(state, "e_1");

            StringAssert.Contains("player@0", playerLine);
            StringAssert.Contains("enemy@0", enemyLine);
        }

        [Test]
        public void FormatSelectedBattlefield_WhenIndexIsOutOfRange_ReturnsNone()
        {
            BattleState state = CreateBattleStateForFormatterTests();

            string line = BattleDebugPanelFormatter.FormatSelectedBattlefield(state, -1);

            Assert.AreEqual("Selected Battlefield: (none)", line);
        }

        [Test]
        public void FormatSelectedBattlefield_WhenBattlefieldIsNull_ReturnsMissing()
        {
            BattleState state = CreateBattleStateForFormatterTests();
            state.battlefields[1] = null;

            string line = BattleDebugPanelFormatter.FormatSelectedBattlefield(state, 1);

            Assert.AreEqual("Selected Battlefield: 1 (missing)", line);
        }

        [Test]
        public void FormatSelectedBattlefield_WhenValid_ReturnsTroopCounts()
        {
            BattleState state = CreateBattleStateForFormatterTests();

            string line = BattleDebugPanelFormatter.FormatSelectedBattlefield(state, 0);

            Assert.AreEqual("Selected Battlefield: 0 (bf_0) | Troops P:1 E:1", line);
        }

        [Test]
        public void FormatTroopEffects_WhenTroopHasEffect_ReturnsReadableEffectsLabel()
        {
            BattleState state = CreateBattleStateForFormatterTests();
            TroopInstance enemyTroop = state.troopsById["e_1"];
            enemyTroop.attack = 5;

            var troopDef = new TroopDef
            {
                attack = 2,
                nameLocKey = "troop_ratkin_name",
                descLocKey = "troop_ratkin_desc",
                effects = new System.Collections.Generic.List<TimedEffectDef>
                {
                    new TimedEffectDef
                    {
                        timing = "TurnEnd",
                        ops = new System.Collections.Generic.List<EffectOpDef>
                        {
                            new EffectOpDef
                            {
                                op = "AddAttackModifier",
                                value = 2
                            }
                        }
                    }
                }
            };

            var database = new GameDatabase();
            database.troopsById["troop_ratkin"] = troopDef;

            enemyTroop.troopDefId = "troop_ratkin";

            string line = BattleDebugPanelFormatter.FormatTroopEffects(database, enemyTroop.troopDefId);

            Assert.AreEqual("Turn End: Add Attack Modifier(+2)", line);
        }

        static BattleState CreateBattleStateForFormatterTests()
        {
            var state = new BattleState();

            state.troopsById.Clear();
            state.campTroopIds.Clear();

            BattlefieldState field0 = state.battlefields[0];
            field0.battlefieldId = "bf_0";
            field0.slotLimit = 2;
            field0.playerTroopIds.Clear();
            field0.enemyTroopIds.Clear();
            field0.totalAttackBonusPlayer = 2;
            field0.totalAttackBonusEnemy = 3;

            state.troopsById["p_1"] = new TroopInstance
            {
                instanceId = "p_1",
                troopDefId = "troop.player",
                attack = 4,
                attackResult = 4
            };
            state.troopsById["e_1"] = new TroopInstance
            {
                instanceId = "e_1",
                troopDefId = "troop.enemy",
                attack = 5,
                attackResult = 5
            };
            state.troopsById["camp_1"] = new TroopInstance
            {
                instanceId = "camp_1",
                troopDefId = "troop.camp",
                attack = 2,
                attackResult = 2
            };

            field0.playerTroopIds.Add("p_1");
            field0.enemyTroopIds.Add("e_1");
            state.campTroopIds.Add("camp_1");

            return state;
        }
    }
}
