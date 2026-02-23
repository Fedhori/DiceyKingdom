using Game.Domain.Duel;
using Game.Infrastructure.Data;
using Game.Presentation.Debug;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class DuelDebugPanelFormatterEditModeTests
    {
        [Test]
        public void FormatClash_ValidField_IncludesTotalAttackAndActionCounts()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatClash(state, 0);

            StringAssert.Contains("Clash 0 (clash.0)", line);
            StringAssert.Contains("TotalAttack P:6 E:8", line);
            StringAssert.Contains("Actions P:1 E:1", line);
            StringAssert.Contains("Slot:2", line);
            StringAssert.DoesNotContain("Players:", line);
            StringAssert.DoesNotContain("Enemies:", line);
        }

        [Test]
        public void FormatClash_WithDatabase_IncludesGreatVictoryAndVictoryDamage()
        {
            DuelState state = CreateDuelStateForFormatterTests();
            var database = new GameDatabase();
            database.clashesById["clash.0"] = new ClashDef
            {
                slotLimit = 2,
                outcomeEffects = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<EffectBlockDef>>
                {
                    ["GreatVictory"] = new System.Collections.Generic.List<EffectBlockDef>
                    {
                        new EffectBlockDef
                        {
                            ops = new System.Collections.Generic.List<EffectOpDef>
                            {
                                new EffectOpDef
                                {
                                    op = "ModifyHealth",
                                    side = "Opponent",
                                    delta = -2
                                }
                            }
                        }
                    },
                    ["Victory"] = new System.Collections.Generic.List<EffectBlockDef>
                    {
                        new EffectBlockDef
                        {
                            ops = new System.Collections.Generic.List<EffectOpDef>
                            {
                                new EffectOpDef
                                {
                                    op = "ModifyHealth",
                                    side = "Opponent",
                                    delta = -1
                                }
                            }
                        }
                    }
                }
            };

            string line = DuelDebugPanelFormatter.FormatClash(state, 0, database);

            StringAssert.Contains("Damage GV:2 V:1", line);
        }

        [Test]
        public void FormatSelectedAction_WhenNoSelection_ReturnsNone()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatSelectedAction(state, string.Empty);

            Assert.AreEqual("Selected Action: (none)", line);
        }

        [Test]
        public void FormatSelectedAction_WhenActionIsMissing_ReturnsMissing()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatSelectedAction(state, "missing_action");

            Assert.AreEqual("Selected Action: (missing)", line);
        }

        [Test]
        public void FormatSelectedAction_WhenActionIsInActionHolder_ReturnsActionHolderLocation()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatSelectedAction(state, "actionHolder_1");

            StringAssert.Contains("Selected Action: action.actionHolder", line);
            StringAssert.Contains("Attack Result:2", line);
            StringAssert.Contains("actionHolder", line);
        }

        [Test]
        public void FormatActionHolderActions_WhenActionHolderActionExists_IncludesActionHolderDefIdOnly()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatActionHolderActions(state, "actionHolder_1");

            StringAssert.Contains("Selected Action: action.actionHolder", line);
            StringAssert.Contains("ActionHolder Actions (1):", line);
            StringAssert.Contains("- action.actionHolder | Attack:2 | Attack Result:2 <selected>", line);
            StringAssert.DoesNotContain("actionHolder_1", line);
        }

        [Test]
        public void FormatSelectedAction_WhenActionIsDeployed_ReturnsClashLocation()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string playerLine = DuelDebugPanelFormatter.FormatSelectedAction(state, "p_1");
            string opponentLine = DuelDebugPanelFormatter.FormatSelectedAction(state, "e_1");

            StringAssert.Contains("player@0", playerLine);
            StringAssert.Contains("opponent@0", opponentLine);
        }

        [Test]
        public void FormatSelectedClash_WhenIndexIsOutOfRange_ReturnsNone()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatSelectedClash(state, -1);

            Assert.AreEqual("Selected Clash: (none)", line);
        }

        [Test]
        public void FormatSelectedClash_WhenClashIsNull_ReturnsMissing()
        {
            DuelState state = CreateDuelStateForFormatterTests();
            state.clashes[1] = null;

            string line = DuelDebugPanelFormatter.FormatSelectedClash(state, 1);

            Assert.AreEqual("Selected Clash: 1 (missing)", line);
        }

        [Test]
        public void FormatSelectedClash_WhenValid_ReturnsActionCounts()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatSelectedClash(state, 0);

            Assert.AreEqual("Selected Clash: 0 (clash.0) | Actions P:1 E:1", line);
        }

        [Test]
        public void FormatActionEffects_WhenActionHasEffect_ReturnsReadableEffectsLabel()
        {
            DuelState state = CreateDuelStateForFormatterTests();
            ActionInstance opponentAction = state.actionsById["e_1"];
            opponentAction.attack = 5;

            var actionDef = new ActionDef
            {
                attack = 2,
                nameLocKey = "action.ratkin_name",
                descLocKey = "action.ratkin_desc",
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
            database.actionsById["action.ratkin"] = actionDef;

            opponentAction.actionDefId = "action.ratkin";

            string line = DuelDebugPanelFormatter.FormatActionEffects(database, opponentAction.actionDefId);

            Assert.AreEqual("Turn End: Add Attack Modifier(+2)", line);
        }

        static DuelState CreateDuelStateForFormatterTests()
        {
            var state = new DuelState();

            state.actionsById.Clear();
            state.actionHolderActionIds.Clear();

            ClashState field0 = state.clashes[0];
            field0.clashId = "clash.0";
            field0.slotLimit = 2;
            field0.playerActionIds.Clear();
            field0.opponentActionIds.Clear();
            field0.totalAttackBonusPlayer = 2;
            field0.totalAttackBonusOpponent = 3;

            state.actionsById["p_1"] = new ActionInstance
            {
                instanceId = "p_1",
                actionDefId = "action.player",
                attack = 4,
                attackResult = 4
            };
            state.actionsById["e_1"] = new ActionInstance
            {
                instanceId = "e_1",
                actionDefId = "action.opponent",
                attack = 5,
                attackResult = 5
            };
            state.actionsById["actionHolder_1"] = new ActionInstance
            {
                instanceId = "actionHolder_1",
                actionDefId = "action.actionHolder",
                attack = 2,
                attackResult = 2
            };

            field0.playerActionIds.Add("p_1");
            field0.opponentActionIds.Add("e_1");
            state.actionHolderActionIds.Add("actionHolder_1");

            return state;
        }
    }
}
