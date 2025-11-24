
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Tooltip
{
    public class TooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TooltipKind kind = TooltipKind.SimpleText;
        [SerializeField] private TooltipContent content = new TooltipContent();
        [SerializeField] private Button pinButton;
        private void Start()
        {
            if (pinButton != null) pinButton.onClick.AddListener(() => TooltipManager.Instance.PinCurrentTooltip(kind));
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            TooltipManager.Instance?.ShowTooltip(eventData.position, content, kind, this);
        }

        public void OnPointerExit(PointerEventData eventData)
        { 
            TooltipManager.Instance?.HideTooltip(kind);
        }

        private void OnDisable()
        {
            TooltipManager.Instance?.HideTooltip(kind);
        }

        private void OnDestroy()
        {
            TooltipManager.Instance?.HideTooltip(kind);
        }

        public void SetSimpleTooltipContent(string refKey, string stringKey,  Dictionary<string, object> arguments = null)
        {
            content.refKey = refKey;
            content.stringKey = stringKey;
            content.arguments = arguments;
        }
    }
}