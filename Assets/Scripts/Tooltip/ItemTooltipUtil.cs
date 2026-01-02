using System;
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
                TooltipKind.Item
            );
        }

        string title = LocalizationUtil.GetItemName(item.Id);
        if (string.IsNullOrEmpty(title))
            title = item.Id;

        var body = BuildBody(item);
        var icon = SpriteCache.GetItemSprite(item.Id);

        return new TooltipModel(
            title,
            body,
            icon,
            TooltipKind.Item
        );
    }

    public static string BuildBody(ItemInstance item)
    {
        if (item == null)
            return string.Empty;

        var lines = new List<string>();
        AppendStatLines(item, lines);
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

            var line = BuildRuleLine(item, rule, i);
            if (!string.IsNullOrEmpty(line))
                lines.Add(line);
        }
    }

    static void AppendStatLines(ItemInstance item, List<string> lines)
    {
        if (item == null)
            return;

        if (item.DamageMultiplier > 0f)
        {
            var line = BuildStatLine("tooltip.damageMultiplier.description", item.DamageMultiplier);
            if (!string.IsNullOrEmpty(line))
                lines.Add(line);
        }

        if (item.AttackSpeed > 0f)
        {
            var line = BuildStatLine("tooltip.attackSpeed.description", item.AttackSpeed);
            if (!string.IsNullOrEmpty(line))
                lines.Add(line);
        }
    }

    static string BuildStatLine(string key, float value)
    {
        if (string.IsNullOrEmpty(key))
            return string.Empty;

        var loc = new LocalizedString("tooltip", key);
        var dict = new Dictionary<string, object>
        {
            ["value"] = value.ToString("0.##")
        };
        loc.Arguments = new object[] { dict };

        var line = loc.GetLocalizedString();
        if (string.IsNullOrEmpty(line) || string.Equals(line, key, StringComparison.Ordinal))
            return string.Empty;

        return line;
    }

    static string BuildRuleLine(ItemInstance item, ItemRuleDto rule, int ruleIndex)
    {
        var key = $"{item.Id}.effect{ruleIndex}";
        var loc = new LocalizedString("item", key);

        var args = BuildRuleArgs(item, rule);
        if (args != null)
            loc.Arguments = new object[] { args };

        return loc.GetLocalizedString();
    }


    static object BuildRuleArgs(ItemInstance item, ItemRuleDto rule)
    {
        var dict = new Dictionary<string, object>();

        if (rule != null && rule.effects != null)
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

        if (item is { PelletCount: > 1 })
            dict["pelletCount"] = item.PelletCount;

        if (item is { PierceBouns: > 0 })
            dict["pierceBonus"] = item.PierceBouns;

        return dict.Count == 0 ? null : dict;
    }
}
