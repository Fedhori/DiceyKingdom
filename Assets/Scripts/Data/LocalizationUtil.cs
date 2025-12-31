using UnityEngine.Localization;

public static class LocalizationUtil
{
    public static string GetPinName(string id)
    {
        return new LocalizedString("pin", $"{id}.name").GetLocalizedString();
    }

    public static string GetItemName(string id)
    {
        return new LocalizedString("token", $"{id}.name").GetLocalizedString();
    }
    
    public static string SoldString = new LocalizedString("game", "game.sold.label").GetLocalizedString();
}
