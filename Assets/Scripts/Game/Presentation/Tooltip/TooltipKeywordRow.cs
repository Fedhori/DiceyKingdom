using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;




namespace Game.UI.Tooltip
{
public sealed class TooltipKeywordRow : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    public void Bind(TooltipKeywordEntry entry)
    {
        if (titleText != null)
            titleText.text = ResolveLocalizedText(entry.titleKey, null);

        if (bodyText != null)
            bodyText.text = ResolveLocalizedText(entry.bodyKey, entry.arguments);
    }

    static string ResolveLocalizedText(string key, Dictionary<string, object> args)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        var loc = new LocalizedString("tooltip", key);
        if (args != null)
            loc.Arguments = new object[] { args };

        return loc.GetLocalizedString();
    }
}

}
