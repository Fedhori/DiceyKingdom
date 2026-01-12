using System.Collections.Generic;
using System.Text;
using Data;
using UnityEngine;
using UnityEngine.Localization;

public static class UpgradeTooltipUtil
{
    public static TooltipModel BuildModel(UpgradeInstance upgrade)
    {
        if (upgrade == null)
            return new TooltipModel(string.Empty, string.Empty, null, TooltipKind.Item);

        string title = LocalizationUtil.GetUpgradeName(upgrade.Id);
        if (string.IsNullOrEmpty(title))
            title = upgrade.Id;

        string body = BuildBody(upgrade);
        var icon = SpriteCache.GetUpgradeSprite(upgrade.Id);

        return new TooltipModel(
            title,
            body,
            icon,
            TooltipKind.Item,
            0f,
            ItemRarity.Common,
            "강화"
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

        var dict = new Dictionary<string, object>
        {
            ["value0"] = effect.value.ToString("0.##")
        };

        return dict;
    }
}
