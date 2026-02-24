using UnityEngine;

namespace Game.Presentation.Battle
{
    [CreateAssetMenu(
        fileName = "BattleAnimationConfig",
        menuName = "Game/Battle/Battle Animation Config",
        order = 1000)]
    public sealed class BattleAnimationConfig : ScriptableObject
    {
        [Min(0f)]
        public float rollDuration = 0.35f;

        [Min(0f)]
        public float resolvePerCombatDuration = 0.55f;

        [Min(0f)]
        public float resolveCombatGap = 0.15f;

        [Min(0f)]
        public float turnTransitionDuration = 0.30f;
    }
}
