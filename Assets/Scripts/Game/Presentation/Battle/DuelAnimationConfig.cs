using UnityEngine;

namespace Game.Presentation.Battle
{
    [CreateAssetMenu(
        fileName = "DuelAnimationConfig",
        menuName = "Game/Duel/Duel Animation Config",
        order = 1000)]
    public class DuelAnimationConfig : ScriptableObject
    {
        [Min(0f)]
        public float rollDuration = 0.35f;

        [Min(0f)]
        public float cardRollDuration = 0.5f;

        [Min(0f)]
        public float resolvePerCombatDuration = 0.55f;

        [Min(0f)]
        public float resolveCombatGap = 0.15f;
    }
}

