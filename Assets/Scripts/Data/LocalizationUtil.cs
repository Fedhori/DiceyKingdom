using UnityEngine.Localization;

public static class LocalizationUtil
{
    public static string GetPinName(string id)
    {
        return new LocalizedString("pin", $"{id}.name").GetLocalizedString();
    }
    
    public static string GetBallName(string id)
    {
        return new LocalizedString("ball", $"{id}.name").GetLocalizedString();
    }
    
    public static string SoldString = new LocalizedString("game", "game.sold.label").GetLocalizedString();
}