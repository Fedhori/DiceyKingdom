using Game.Domain.Duel;
using Game.Infrastructure.Data;
using Game.Presentation.Debug;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class DuelDebugPanelFormatterEditModeTests
    {
        [Test]
        public void FormatCombat_ValidCombat_IncludesTotalPowerAndAbilityCounts()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatCombat(state, 0);

            StringAssert.Contains("Combat 0", line);
            StringAssert.Contains("TotalPower P:6 E:8", line);
            StringAssert.Contains("Abilities P:1 E:1", line);
            StringAssert.Contains("Cap:2", line);
            StringAssert.DoesNotContain("Players:", line);
            StringAssert.DoesNotContain("Enemies:", line);
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
        public void FormatSelectedAbility_WhenAbilityIsInLoadout_ReturnsLoadoutLocation()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatSelectedAbility(state, "loadout_1");

            StringAssert.Contains("Selected Ability: ability.loadout", line);
            StringAssert.Contains("Power Result:2", line);
            StringAssert.Contains("loadout", line);
        }

        [Test]
        public void FormatLoadoutAbilities_WhenLoadoutAbilityExists_IncludesAbilityDefIdOnly()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatLoadoutAbilities(state, "loadout_1");

            StringAssert.Contains("Selected Ability: ability.loadout", line);
            StringAssert.Contains("Loadout Abilities (1):", line);
            StringAssert.Contains("- ability.loadout | Type:Attack | Power:2 | Power Result:2 | CD:- <selected>", line);
            StringAssert.DoesNotContain("loadout_1", line);
        }

        [Test]
        public void FormatSelectedAbility_WhenAbilityIsDeployed_ReturnsCombatLocation()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string playerLine = DuelDebugPanelFormatter.FormatSelectedAbility(state, "p_1");
            string opponentLine = DuelDebugPanelFormatter.FormatSelectedAbility(state, "e_1");

            StringAssert.Contains("player@0", playerLine);
            StringAssert.Contains("opponent@0", opponentLine);
        }

        [Test]
        public void FormatSelectedCombat_WhenIndexIsOutOfRange_ReturnsNone()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatSelectedCombat(state, -1);

            Assert.AreEqual("Selected Combat: (none)", line);
        }

        [Test]
        public void FormatSelectedCombat_WhenCombatIsNull_ReturnsMissing()
        {
            DuelState state = CreateDuelStateForFormatterTests();
            state.combats[1] = null;

            string line = DuelDebugPanelFormatter.FormatSelectedCombat(state, 1);

            Assert.AreEqual("Selected Combat: 1 (missing)", line);
        }

        [Test]
        public void FormatSelectedCombat_WhenValid_ReturnsAbilityCounts()
        {
            DuelState state = CreateDuelStateForFormatterTests();

            string line = DuelDebugPanelFormatter.FormatSelectedCombat(state, 0);

            Assert.AreEqual("Selected Combat: 0 | Abilities P:1 E:1", line);
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

            Assert.AreEqual("Turnend: Addpowermodifier(+2)", line);
        }

        static DuelState CreateDuelStateForFormatterTests()
        {
            var state = new DuelState();
            AddCombats(state, 3);

            state.abilitiesById.Clear();
            state.loadoutAbilityIds.Clear();

            CombatState combat0 = state.combats[0];
            combat0.maxPlayerAssignments = 2;
            combat0.playerAbilityIds.Clear();
            combat0.opponentAbilityIds.Clear();
            combat0.totalPowerBonusPlayer = 2;
            combat0.totalPowerBonusOpponent = 3;

            state.abilitiesById["p_1"] = new AbilityInstance
            {
                instanceId = "p_1",
                abilityDefId = "ability.player",
                abilityType = AbilityType.Attack,
                power = 4,
                powerResult = 4
            };
            state.abilitiesById["e_1"] = new AbilityInstance
            {
                instanceId = "e_1",
                abilityDefId = "ability.opponent",
                abilityType = AbilityType.Attack,
                power = 5,
                powerResult = 5
            };
            state.abilitiesById["loadout_1"] = new AbilityInstance
            {
                instanceId = "loadout_1",
                abilityDefId = "ability.loadout",
                abilityType = AbilityType.Attack,
                power = 2,
                powerResult = 2
            };

            combat0.playerAbilityIds.Add("p_1");
            combat0.opponentAbilityIds.Add("e_1");
            state.loadoutAbilityIds.Add("loadout_1");

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
    }
}
