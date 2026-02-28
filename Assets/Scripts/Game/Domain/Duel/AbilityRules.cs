namespace Game.Domain.Duel
{
    public static class AbilityRules
    {
        public static int GetDefaultCooldownTurns(AbilityType abilityType)
        {
            return abilityType == AbilityType.Passive
                ? 0
                : 1;
        }

        public static int GetMinimumCooldownTurns(AbilityType abilityType)
        {
            return abilityType == AbilityType.Passive
                ? 0
                : 1;
        }
    }
}
