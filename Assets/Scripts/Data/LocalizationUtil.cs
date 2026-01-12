using UnityEngine.Localization;

public static class LocalizationUtil
{
    public static string GetItemName(string id)
    {
        return new LocalizedString("item", $"{id}.name").GetLocalizedString();
    }

    public static string GetUpgradeName(string id)
    {
        return new LocalizedString("upgrade", $"{id}.name").GetLocalizedString();
    }
    
    public static string SoldString = new LocalizedString("game", "game.sold.label").GetLocalizedString();
}
