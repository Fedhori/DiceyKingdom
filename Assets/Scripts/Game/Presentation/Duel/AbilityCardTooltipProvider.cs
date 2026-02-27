using Game.UI.Tooltip;
using UnityEngine;

namespace Game.Presentation.Duel
{
    public sealed class AbilityCardTooltipProvider : MonoBehaviour, ITooltipContentProvider
    {
        string title = string.Empty;
        string body = string.Empty;

        public void SetContent(string title, string body)
        {
            this.title = title ?? string.Empty;
            this.body = body ?? string.Empty;
        }

        public bool TryBuildTooltipModel(out TooltipModel model)
        {
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
            {
                model = default;
                return false;
            }

            model = new TooltipModel(title, body, TooltipKind.Simple);
            return true;
        }
    }
}
