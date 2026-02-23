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
            StringAssert.Contains("Abilities P:1 E:1", line);
            StringAssert.Contains("Slot:2", line);
            StringAssert.DoesNotContain("Players:", line);
            StringAssert.DoesNotContain("Enemies:", line);
        }

        [Test]
        public void FormatClash_WithDatabase_IncludesClashDamage()
        {
            DuelState state = CreateDuelStateForFormatterTests();
            var database = new GameDatabase();
            database.clashesById["clash.0"] = new ClashDef
            {
                slotLimit = 2,
                damage = 2
            };

            string line = DuelDebugPanelFormatter.FormatClash(state, 0, database);

            StringAssert.Contains("Damage:2", line);
        }

        [Test]
        public void FormatSelectedAction_WhenNoSelection_ReturnsNone()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatSelectedAction(state, string.Empty);

            Assert.AreEqual("Selected Ability: (none)", line);
        }

        [Test]
        public void FormatSelectedAction_WhenActionIsMissing_ReturnsMissing()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatSelectedAction(state, "missing_action");

            Assert.AreEqual("Selected Ability: (missing)", line);
        }

        [Test]
        public void FormatSelectedAction_WhenActionIsInAbilityHolder_ReturnsAbilityHolderLocation()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatSelectedAction(state, "abilityHolder_1");

            StringAssert.Contains("Selected Ability: ability.abilityHolder", line);
            StringAssert.Contains("Attack Result:2", line);
            StringAssert.Contains("bag", line);
        }

        [Test]
        public void FormatAbilityHolderActions_WhenAbilityHolderActionExists_IncludesAbilityHolderDefIdOnly()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatAbilityHolderActions(state, "abilityHolder_1");

            StringAssert.Contains("Selected Ability: ability.abilityHolder", line);
            StringAssert.Contains("Bag Abilities (1):", line);
            StringAssert.Contains("- ability.abilityHolder | Type:Attack | Damage:2 | Attack Result:2 | CD:- <selected>", line);
            StringAssert.DoesNotContain("abilityHolder_1", line);
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

            Assert.AreEqual("Selected Clash: 0 (clash.0) | Abilities P:1 E:1", line);
        }

        [Test]
        public void FormatActionEffects_WhenActionHasEffect_ReturnsReadableEffectsLabel()
        {
            DuelState state = CreateDuelStateForFormatterTests();
            AbilityInstance opponentAction = state.abilitiesById["e_1"];
            opponentAction.attack = 5;

            var actionDef = new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 0,
                cooldown = 0,
                damage = 2,
                nameLocKey = "ability.ratkin_name",
                descLocKey = "ability.ratkin_desc",
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
            database.abilitiesById["ability.ratkin"] = actionDef;

            opponentAction.abilityDefId = "ability.ratkin";

            string line = DuelDebugPanelFormatter.FormatActionEffects(database, opponentAction.abilityDefId);

            Assert.AreEqual("Turn End: Add Attack Modifier(+2)", line);
        }

        static DuelState CreateDuelStateForFormatterTests()
        {
            var state = new DuelState();

            state.abilitiesById.Clear();
            state.abilityHolderAbilityIds.Clear();

            ClashState field0 = state.clashes[0];
            field0.clashId = "clash.0";
            field0.slotLimit = 2;
            field0.playerActionIds.Clear();
            field0.opponentActionIds.Clear();
            field0.totalAttackBonusPlayer = 2;
            field0.totalAttackBonusOpponent = 3;

            state.abilitiesById["p_1"] = new AbilityInstance
            {
                instanceId = "p_1",
                abilityDefId = "ability.player",
                attack = 4,
                attackResult = 4
            };
            state.abilitiesById["e_1"] = new AbilityInstance
            {
                instanceId = "e_1",
                abilityDefId = "ability.opponent",
                attack = 5,
                attackResult = 5
            };
            state.abilitiesById["abilityHolder_1"] = new AbilityInstance
            {
                instanceId = "abilityHolder_1",
                abilityDefId = "ability.abilityHolder",
                attack = 2,
                attackResult = 2
            };

            field0.playerActionIds.Add("p_1");
            field0.opponentActionIds.Add("e_1");
            state.abilityHolderAbilityIds.Add("abilityHolder_1");

            return state;
        }
    }
}
