using System;
using System.Collections.Generic;
using System.Text;
using Data;
using GameStats;
using UnityEngine;
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
            TooltipKind.Item,
            item.DamageMultiplier
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

    static void AppendStatLines(ItemInstance item, List<string> lines)
    {
        if (item == null)
            return;

        float finalDamage = GetFinalDamage(item);
        if (finalDamage > 0f)
        {
            float multiplier = item.DamageMultiplier;
            var args = new Dictionary<string, object>
            {
                ["damage"] = finalDamage.ToString("0.##"),
                ["multiplier"] = multiplier.ToString("0.##")
            };
            lines.Add(BuildStatLine("tooltip.damage.description", args));
        }

        float finalAttackSpeed = GetFinalAttackSpeed(item);
        if (finalAttackSpeed > 0f)
        {
            var args = new Dictionary<string, object>
            {
                ["value"] = finalAttackSpeed.ToString("0.##")
            };
            lines.Add(BuildStatLine("tooltip.attackSpeed.description", args));
        }

        if (item.Pierce > 0)
        {
            var args = new Dictionary<string, object>
            {
                ["value"] = item.Pierce.ToString("0")
            };
            lines.Add(BuildStatLine("tooltip.pierce.description", args));
        }

        if (item.StatusType == BlockStatusType.Freeze && item.StatusDuration > 0f)
        {
            var args = new Dictionary<string, object>
            {
                ["seconds"] = item.StatusDuration.ToString("0.##")
            };
            lines.Add(BuildStatLine("tooltip.status.freeze.description", args));
        }
    }

    static float GetFinalDamage(ItemInstance item)
    {
        if (item == null || item.DamageMultiplier <= 0f)
            return 0f;

        float power = GetPlayerPower();

        float raw = item.DamageMultiplier * power;
        return Mathf.Max(1f, Mathf.Floor(raw));
    }

    static float GetPlayerPower()
    {
        float power = 0f;
        var player = PlayerManager.Instance?.Current;
        if (player != null)
            power = Mathf.Max(0f, (float)player.Power);

        return power;
    }

    static float GetFinalAttackSpeed(ItemInstance item)
    {
        if (item == null || item.AttackSpeed <= 0f)
            return 0f;

        return item.AttackSpeed;
    }

    static string BuildStatLine(string key, Dictionary<string, object> args)
    {
        var loc = new LocalizedString("tooltip", key);
        if (args != null)
            loc.Arguments = new object[] { args };

        return loc.GetLocalizedString();
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
                if (e.effectMode == StatOpKind.Mult)
                {
                    dict[key] = (e.value * 100f).ToString("0.##");
                }
                else
                {
                    dict[key] = e.value.ToString("0.##");
                }
            }
        }

        if (rule?.condition != null && rule.condition.count > 0)
            dict["count"] = rule.condition.count;

        if (item is { PelletCount: > 1 })
            dict["pelletCount"] = item.PelletCount;

        if (item is { PierceBonus: > 0 })
            dict["pierceBonus"] = item.PierceBonus;

        return dict.Count == 0 ? null : dict;
    }
}
