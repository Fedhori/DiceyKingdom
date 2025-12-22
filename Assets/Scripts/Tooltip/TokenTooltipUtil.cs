using System.Collections.Generic;
using System.Text;
using Data;
using UnityEngine.Localization;

public static class TokenTooltipUtil
{
    public static TooltipModel BuildModel(TokenInstance token)
    {
        if (token == null)
        {
            return new TooltipModel(
                string.Empty,
                string.Empty,
                null,
                TooltipKind.Token,
                0
            );
        }

        var title = LocalizationUtil.GetTokenName(token.Id);
        var body = BuildBody(token);
        var icon = SpriteCache.GetTokenSprite(token.Id);

        return new TooltipModel(
            title,
            body,
            icon,
            TooltipKind.Token,
            0
        );
    }

    public static string BuildBody(TokenInstance token)
    {
        var lines = new List<string>();
        AppendRuleLines(token, lines);

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

    static void AppendRuleLines(TokenInstance token, List<string> lines)
    {
        var rules = token.Rules;
        if (rules == null || rules.Count == 0)
            return;

        for (int i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (rule == null)
                continue;

            if (rule.effects == null || rule.effects.Count == 0)
                continue;

            var line = BuildRuleLine(token, rule, i);
            if (!string.IsNullOrEmpty(line))
                lines.Add(line);
        }
    }

    static string BuildRuleLine(TokenInstance token, TokenRuleDto rule, int ruleIndex)
    {
        var key = $"{token.Id}.effect{ruleIndex}";
        var loc = new LocalizedString("token", key);

        var args = BuildRuleArgs(rule);
        if (args != null)
            loc.Arguments = new object[] { args };

        return loc.GetLocalizedString();
    }

    static object BuildRuleArgs(TokenRuleDto rule)
    {
        var dict = new Dictionary<string, object>();

        if (rule.condition != null)
        {
            // 조건별 추가 파라미터가 생기면 여기에 채운다
        }

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
