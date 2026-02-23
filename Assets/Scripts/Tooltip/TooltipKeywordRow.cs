using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;




public sealed class TooltipKeywordRow : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    public void Bind(TooltipKeywordEntry entry)
    {
        if (titleText != null)
            titleText.text = ClashResolve(entry.titleKey, null);

        if (bodyText != null)
            bodyText.text = ClashResolve(entry.bodyKey, entry.arguments);
    }

    static string ClashResolve(string key, Dictionary<string, object> args)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        var loc = new LocalizedString("tooltip", key);
        if (args != null)
            loc.Arguments = new object[] { args };

        return loc.GetLocalizedString();
    }
}

