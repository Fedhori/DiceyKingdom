using Game.Domain.Duel;
using Game.Infrastructure.Data;
using Game.Presentation.Debug;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class DuelDebugPanelFormatterEditModeTests
    {
        [Test]
        public void FormatClash_ValidField_IncludesTotalPowerAndAbilityCounts()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatClash(state, 0);

            StringAssert.Contains("Clash 0 (clash.0)", line);
            StringAssert.Contains("TotalPower P:6 E:8", line);
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
        public void FormatSelectedAbility_WhenNoSelection_ReturnsNone()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatSelectedAbility(state, string.Empty);

            Assert.AreEqual("Selected Ability: (none)", line);
        }

        [Test]
        public void FormatSelectedAbility_WhenAbilityIsMissing_ReturnsMissing()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatSelectedAbility(state, "ability.missing");

            Assert.AreEqual("Selected Ability: (missing)", line);
        }

        [Test]
        public void FormatSelectedAbility_WhenAbilityIsInBag_ReturnsBagLocation()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatSelectedAbility(state, "bag_1");

            StringAssert.Contains("Selected Ability: ability.bag", line);
            StringAssert.Contains("Power Result:2", line);
            StringAssert.Contains("bag", line);
        }

        [Test]
        public void FormatBagAbilities_WhenBagAbilityExists_IncludesAbilityDefIdOnly()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatBagAbilities(state, "bag_1");

            StringAssert.Contains("Selected Ability: ability.bag", line);
            StringAssert.Contains("Bag Abilities (1):", line);
            StringAssert.Contains("- ability.bag | Type:Attack | Power:2 | Power Result:2 | CD:- <selected>", line);
            StringAssert.DoesNotContain("bag_1", line);
        }

        [Test]
        public void FormatSelectedAbility_WhenAbilityIsDeployed_ReturnsClashLocation()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string playerLine = DuelDebugPanelFormatter.FormatSelectedAbility(state, "p_1");
            string opponentLine = DuelDebugPanelFormatter.FormatSelectedAbility(state, "e_1");

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
        public void FormatSelectedClash_WhenValid_ReturnsAbilityCounts()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatSelectedClash(state, 0);

            Assert.AreEqual("Selected Clash: 0 (clash.0) | Abilities P:1 E:1", line);
        }

        [Test]
        public void FormatAbilityEffects_WhenAbilityHasEffect_ReturnsReadableEffectsLabel()
        {
            DuelState state = CreateDuelStateForFormatterTests();
            AbilityInstance opponentAbility = state.abilitiesById["e_1"];
            opponentAbility.power = 5;

            var abilityDef = new AbilityDef
            {
                type = AbilityType.Attack.ToString(),
                buildCost = 0,
                cooldown = 0,
                power = 2,
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
                                op = "AddPowerModifier",
                                value = 2
                            }
                        }
                    }
                }
            };

            var database = new GameDatabase();
            database.abilitiesById["ability.ratkin"] = abilityDef;

            opponentAbility.abilityDefId = "ability.ratkin";

            string line = DuelDebugPanelFormatter.FormatAbilityEffects(database, opponentAbility.abilityDefId);

            Assert.AreEqual("Turn End: Add Power Modifier(+2)", line);
        }

        static DuelState CreateDuelStateForFormatterTests()
        {
            var state = new DuelState();
            AddClashes(state, 3);

            state.abilitiesById.Clear();
            state.bagAbilityIds.Clear();

            ClashState field0 = state.clashes[0];
            field0.clashId = "clash.0";
            field0.slotLimit = 2;
            field0.playerAbilityIds.Clear();
            field0.opponentAbilityIds.Clear();
            field0.totalPowerBonusPlayer = 2;
            field0.totalPowerBonusOpponent = 3;

            state.abilitiesById["p_1"] = new AbilityInstance
            {
                instanceId = "p_1",
                abilityDefId = "ability.player",
                power = 4,
                powerResult = 4
            };
            state.abilitiesById["e_1"] = new AbilityInstance
            {
                instanceId = "e_1",
                abilityDefId = "ability.opponent",
                power = 5,
                powerResult = 5
            };
            state.abilitiesById["bag_1"] = new AbilityInstance
            {
                instanceId = "bag_1",
                abilityDefId = "ability.bag",
                power = 2,
                powerResult = 2
            };

            field0.playerAbilityIds.Add("p_1");
            field0.opponentAbilityIds.Add("e_1");
            state.bagAbilityIds.Add("bag_1");

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
    }
}



