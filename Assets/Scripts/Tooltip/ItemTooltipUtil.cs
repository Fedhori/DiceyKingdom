using System.Collections.Generic;
using System.Text;
using Data;
using UnityEngine.Localization;

public static class ItemTooltipUtil
{
    public static TooltipModel BuildModel(ItemInstance item)
    {
        if (item == null)
        {
            return new TooltipModel(
                string.Empty,
                string.Empty,
                null,
                TooltipKind.Token
            );
        }

        string title = LocalizationUtil.GetTokenName(item.Id);
        if (string.IsNullOrEmpty(title))
            title = item.Id;

        var body = BuildBody(item);
        var icon = SpriteCache.GetItemSprite(item.Id);

        return new TooltipModel(
            title,
            body,
            icon,
            TooltipKind.Token
        );
    }

    public static string BuildBody(ItemInstance item)
    {
        if (item == null)
            return string.Empty;

        var lines = new List<string>();
        AppendRuleLines(item, lines);

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

    static void AppendRuleLines(ItemInstance item, List<string> lines)
    {
        var rules = item.Rules;
        if (rules == null || rules.Count == 0)
            return;

        for (int i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (rule == null)
                continue;

            if (rule.effects == null || rule.effects.Count == 0)
                continue;

            var line = BuildRuleLine(item, rule, i);
            if (!string.IsNullOrEmpty(line))
                lines.Add(line);
        }
    }

    static string BuildRuleLine(ItemInstance item, ItemRuleDto rule, int ruleIndex)
    {
        var key = $"{item.Id}.effect{ruleIndex}";
        var loc = new LocalizedString("token", key);

        var args = BuildRuleArgs(rule);
        if (args != null)
            loc.Arguments = new object[] { args };

        return loc.GetLocalizedString();
    }

    static object BuildRuleArgs(ItemRuleDto rule)
    {
        var dict = new Dictionary<string, object>();

        if (rule.effects != null)
        {
            for (int i = 0; i < rule.effects.Count; i++)
            {
                var e = rule.effects[i];
                if (e == null)
                    continue;

                string key = $"value{i}";
                dict[key] = e.value.ToString("0.##");
            }
        }

        return dict.Count == 0 ? null : dict;
    }
}
