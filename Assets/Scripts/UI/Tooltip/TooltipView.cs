using UnityEngine;
using UnityEngine.UI;

namespace UI.Tooltip
{
    [RequireComponent(typeof(RectTransform))]
    public abstract class TooltipView : MonoBehaviour
    {
        public TooltipContent Content { get; private set; }
        public TooltipTarget SourceTarget { get; set; }
        public TooltipKind kind;
        public Button closeButton;

        public virtual void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(CloseTooltip);
        }

        public abstract void SetData(TooltipContent data);

        public void CloseTooltip()
        {
            TooltipManager.Instance.UnpinTooltip(this);
        }
    }
}

