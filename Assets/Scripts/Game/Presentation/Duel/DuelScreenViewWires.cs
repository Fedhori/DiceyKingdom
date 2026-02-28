using System;
using System.Linq;
using Game.Application.Duel;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.Presentation.Duel
{
    public sealed class DuelScreenViewWires
    {
        public bool ValidateCombatZonesSerialized(
            DuelCombatZoneView[] combatZones,
            DuelAbilityCardView abilityCardPrefab,
            string stage,
            UnityEngine.Object context)
        {
            bool hasValidZones = combatZones != null &&
                combatZones.Length == 3 &&
                combatZones.All(zone => zone != null);
            bool hasCardPrefab = abilityCardPrefab != null;
            if (hasValidZones && hasCardPrefab)
            {
                return true;
            }

            int length = combatZones == null ? 0 : combatZones.Length;
            Debug.LogError(
                $"[DuelScreenController] Invalid serialized references at {stage}. " +
                $"combatZonesLength={length}, combatZonesAllAssigned={hasValidZones}, abilityCardPrefabAssigned={hasCardPrefab}. " +
                "Auto-assignment is disabled.",
                context);
            return false;
        }

        public DuelScreenView CreateView(
            Image backgroundImage,
            Image topBarImage,
            TMP_Text turnText,
            TMP_Text enemyHealthText,
            TMP_Text playerHealthText,
            Button combatStartButton,
            Button surrenderButton,
            RectTransform enemyLoadoutRow,
            RectTransform playerLoadoutRow,
            RectTransform enemyPassiveRow,
            RectTransform playerPassiveRow,
            DuelCombatZoneView[] combatZones,
            DuelAbilityCardView abilityCardPrefab,
            DuelUiQueryService uiQueryService,
            Func<string, Sprite> resolveAbilityIcon)
        {
            return new DuelScreenView(
                backgroundImage,
                topBarImage,
                turnText,
                enemyHealthText,
                playerHealthText,
                combatStartButton,
                surrenderButton,
                enemyLoadoutRow,
                playerLoadoutRow,
                enemyPassiveRow,
                playerPassiveRow,
                combatZones,
                abilityCardPrefab,
                uiQueryService,
                resolveAbilityIcon);
        }

        public void WireCallbacks(
            Button combatStartButton,
            UnityAction onCombatStartClicked,
            Button surrenderButton,
            UnityAction onSurrenderClicked,
            DuelScreenView view,
            Action<int> onZoneClicked)
        {
            if (combatStartButton != null)
            {
                combatStartButton.onClick.RemoveListener(onCombatStartClicked);
                combatStartButton.onClick.AddListener(onCombatStartClicked);
            }

            if (surrenderButton != null)
            {
                surrenderButton.onClick.RemoveListener(onSurrenderClicked);
                surrenderButton.onClick.AddListener(onSurrenderClicked);
            }

            view?.WireZoneCallbacks(onZoneClicked);
        }

        public void UnwireCallbacks(
            Button combatStartButton,
            UnityAction onCombatStartClicked,
            Button surrenderButton,
            UnityAction onSurrenderClicked,
            DuelScreenView view)
        {
            if (combatStartButton != null)
            {
                combatStartButton.onClick.RemoveListener(onCombatStartClicked);
            }

            if (surrenderButton != null)
            {
                surrenderButton.onClick.RemoveListener(onSurrenderClicked);
            }

            view?.UnwireZoneCallbacks();
        }
    }
}
