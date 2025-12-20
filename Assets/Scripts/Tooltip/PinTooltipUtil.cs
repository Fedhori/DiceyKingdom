using System.Collections.Generic;
using System.Text;
using Data;
using UnityEngine.Localization;

public static class PinTooltipUtil
{
    public static TooltipModel BuildModel(PinInstance pin)
    {
        if (pin == null)
        {
            return new TooltipModel
            (
                string.Empty,
                string.Empty,
                null,
                TooltipKind.Pin,
                0
            );
        }

        var title = LocalizationUtil.GetPinName(pin.Id);
        var body = BuildBody(pin);
        var icon = SpriteCache.GetPinSprite(pin.Id);

        return new TooltipModel
        (
            title,
            body,
            icon,
            TooltipKind.Pin,
            pin.ScoreMultiplier
        );
    }

    static string BuildBody(PinInstance pin)
    {
        var lines = new List<string>();
        AppendRuleLines(pin, lines);

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

    static void AppendRuleLines(PinInstance pin, List<string> lines)
    {
        var rules = pin.Rules;
        if (rules == null || rules.Count == 0)
            return;

        for (int i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (rule == null)
                continue;

            if (rule.effects == null || rule.effects.Count == 0)
                continue;

            var line = BuildRuleLine(pin, rule, i);
            if (!string.IsNullOrEmpty(line))
                lines.Add(line);
        }
    }
    
    static string BuildRuleLine(PinInstance pin, PinRuleDto rule, int ruleIndex)
    {
        var key = $"{pin.Id}.effect{ruleIndex}";
        var loc = new LocalizedString("pin", key);

        var args = BuildRuleArgs(pin, rule, ruleIndex);
        if (args != null)
        {
            // SmartFormat + Dictionary 조합으로 {hits0}, {value0} 직접 사용
            loc.Arguments = new object[] { args };
        }

        var text = loc.GetLocalizedString();
        return text;
    }
    
    static object BuildRuleArgs(PinInstance pin, PinRuleDto rule, int index)
    {
        var dict = new Dictionary<string, object>();

        // 단일 condition: hits0
        if (rule.condition != null)
        {
            if(rule.condition.hits > 0)
                dict["hits"] = rule.condition.hits;
            
            // 이쪽에 새로 추가한 파라미터들을 넘긴다
        }

        // effects: valueN
        if (rule.effects != null)
        {
            for (int i = 0; i < rule.effects.Count; i++)
            {
                var e = rule.effects[i];
                if (e == null)
                    continue;

                string key = "";
                if (e.value != 0)
                {
                    key = $"value{i}";
                    float v = e.value;
                    dict[key] = v.ToString("0.##");
                }
                
                // 이쪽에 새로 추가한 파라미터들을 넘긴다
            }
        }

        if (dict.Count == 0)
            return null;

        return dict;
    }
}
