using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Battle
{
    [Obsolete("Use DuelScreenView instead.")]
    public sealed class BattleScreenView : DuelScreenView
    {
        public BattleScreenView(
            Image backgroundImage,
            Image topBarImage,
            TMP_Text turnText,
            TMP_Text enemyHealthText,
            TMP_Text playerHealthText,
            Button combatStartButton,
            Button surrenderButton,
            RectTransform enemyLoadoutRow,
            RectTransform playerLoadoutRow,
            BattleCombatZoneView[] combatZones,
            BattleAbilityCardView _,
            TMP_Text tooltipText,
            Image tooltipBackgroundImage,
            Func<string, Sprite> resolveAbilityIcon)
            : base(
                backgroundImage,
                topBarImage,
                turnText,
                enemyHealthText,
                playerHealthText,
                combatStartButton,
                surrenderButton,
                enemyLoadoutRow,
                playerLoadoutRow,
                combatZones,
                _,
                tooltipText,
                tooltipBackgroundImage,
                resolveAbilityIcon)
        {
        }
    }
}
