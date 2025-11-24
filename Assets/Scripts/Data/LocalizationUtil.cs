using UnityEngine.Localization;

public static class LocalizationUtil
{
    public static string GetTowerName(string id)
    {
        return new LocalizedString("game", $"{id}.name").GetLocalizedString();
    }
    
    public static string GetTowerDesc(string id)
    {
        return new LocalizedString("game", $"{id}.desc").GetLocalizedString();
    }
}