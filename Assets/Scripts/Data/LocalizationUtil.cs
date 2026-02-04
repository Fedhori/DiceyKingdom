using System.Collections.Generic;
using UnityEngine.Localization;

public static class LocalizationUtil
{
    public static string Get(string table, string key, Dictionary<string, object> args = null)
    {
        if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(key))
            return string.Empty;

        var reference = new LocalizedString(table, key);
        if (args != null)
            reference.Arguments = new object[] { args };
        return reference.GetLocalizedString();
    }

    public static LocalizedString Build(string table, string key, Dictionary<string, object> args = null)
    {
        var reference = new LocalizedString(table, key);
        if (args != null)
            reference.Arguments = new object[] { args };
        return reference;
    }
}
