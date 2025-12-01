using UnityEngine.Localization;

public static class LocalizationUtil
{
    public static string GetPinName(string id)
    {
        return new LocalizedString("pin", $"{id}.name").GetLocalizedString();
    }
}