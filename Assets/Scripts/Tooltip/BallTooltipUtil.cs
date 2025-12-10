using UnityEngine;
using System.Collections.Generic;
using System.Text;
using Data;

public static class BallTooltipUtil
{
    public static TooltipModel BuildModel(BallInstance ball)
    {
        if (ball == null)
        {
            return new TooltipModel(
                string.Empty,
                string.Empty,
                null,
                TooltipKind.Ball,
                0
            );
        }

        string id = ball.Id;

        string title = LocalizationUtil.GetBallName(id);
        Sprite icon = SpriteCache.GetBallSprite(id);

        string body = BuildBody(ball);

        return new TooltipModel(
            title,
            body,
            icon,
            TooltipKind.Ball,
            ball.BallScoreMultiplier
        );
    }

    static string BuildBody(BallInstance ball)
    {
        var lines = new List<string>();
        AppendRuleLines(ball, lines);

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

    static void AppendRuleLines(BallInstance ball, List<string> lines)
    {
        var rules = ball.Rules;
        if (rules == null || rules.Count == 0)
            return;

        for (int i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (rule == null)
                continue;

            var line = BuildRuleLine(ball, rule, i);
            if (!string.IsNullOrEmpty(line))
                lines.Add(line);
        }
    }

    static string BuildRuleLine(BallInstance ball, BallRuleDto rule, int ruleIndex)
    {
        var key = $"{ball.Id}.effect{ruleIndex}";
        var loc = new UnityEngine.Localization.LocalizedString("ball", key);

        var args = BuildRuleArgs(ball, rule);
        if (args != null)
            loc.Arguments = new object[] { args };

        return loc.GetLocalizedString();
    }

    static object BuildRuleArgs(BallInstance ball, BallRuleDto rule)
    {
        var dict = new Dictionary<string, object>();

        dict["criticalMultiplier"] = ball.BaseDto.criticalMultiplier;

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

        if (dict.Count == 0)
            return null;

        return dict;
    }
}
