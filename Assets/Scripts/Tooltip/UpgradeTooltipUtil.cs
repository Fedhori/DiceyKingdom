using System.Collections.Generic;
using System.Text;
using Data;
using GameStats;
using UnityEngine;
using UnityEngine.Localization;

public static class UpgradeTooltipUtil
{
    public static TooltipModel BuildModel(UpgradeInstance upgrade, TooltipButtonConfig buttonConfig = null)
    {
        if (upgrade == null)
            return new TooltipModel(string.Empty, string.Empty, TooltipKind.Upgrade, buttonConfig: buttonConfig);

        string title = LocalizationUtil.GetUpgradeName(upgrade.Id);
        if (string.IsNullOrEmpty(title))
            title = upgrade.Id;

        string body = BuildBody(upgrade);
        var keywords = TooltipKeywordUtil.BuildForUpgrade(upgrade);

        return new TooltipModel(
            title,
            body,
            TooltipKind.Upgrade,
            upgrade.Rarity,
            keywords,
            buttonConfig
        );
    }

    static string BuildBody(UpgradeInstance upgrade)
    {
        if (upgrade == null)
            return string.Empty;

        var effects = upgrade.Effects;
        if (effects == null || effects.Count == 0)
            return string.Empty;

        var lines = new List<string>();
        for (int i = 0; i < effects.Count; i++)
        {
            var key = $"{upgrade.Id}.effect{i}";
            var loc = new LocalizedString("upgrade", key);
            var args = BuildArgs(effects[i]);
            if (args != null)
                loc.Arguments = new object[] { args };

            lines.Add(loc.GetLocalizedString());
        }

        if (lines.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        for (int i = 0; i < lines.Count; i++)
        {
            if (i > 0)
                sb.Append('\n');
            sb.Append(lines[i]);
        }

        return sb.ToString();
    }

    static object BuildArgs(ItemEffectDto effect)
    {
        if (effect == null)
            return null;

        float value = effect.value;
        if (effect.effectMode == StatOpKind.Mult)
            value = 1f + value;

        var dict = new Dictionary<string, object>
        {
            ["value0"] = value.ToString("0.##")
        };

        return dict;
    }
}
