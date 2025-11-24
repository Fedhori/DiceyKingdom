
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace UI.Tooltip
{
    public class SimpleTextTooltipView : TooltipView
    {
        [SerializeField] private TMP_Text textMeshPro;

        public override void SetData(TooltipContent data)
        {
            var template = new LocalizedString(data.refKey, data.stringKey); 
            if (data.arguments != null) template.Arguments = new object[] { data.arguments };
            
            textMeshPro.text = template.GetLocalizedString();
        }
    }
}
