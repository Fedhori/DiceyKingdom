using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

public static class AdventurerRuleTextArgsBuilder
{
    public static Dictionary<string, object> Build(AdventurerRuleDef rule)
    {
        var args = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["triggerType"] = string.Empty,
            ["conditionType"] = string.Empty,
            ["effectType"] = string.Empty,
            ["effectValue"] = 0,
            ["conditionThreshold"] = 0,
            ["conditionCount"] = 0,
            ["effectCount"] = 0,
            ["effectDiePickRule"] = string.Empty,
            ["effectThreshold"] = 0,
            ["effectBonusPerMatch"] = 0
        };

        if (rule == null)
            return args;

        args["triggerType"] = rule.trigger?.type?.Trim() ?? string.Empty;
        args["conditionType"] = rule.condition?.type?.Trim() ?? string.Empty;
        args["effectType"] = rule.effect?.effectType?.Trim() ?? string.Empty;
        args["effectValue"] = ToInt(rule.effect?.value);
        args["conditionThreshold"] = GetInt(rule.condition?.conditionParams, "threshold", 0);
        args["conditionCount"] = GetInt(rule.condition?.conditionParams, "count", 0);
        args["effectCount"] = GetInt(rule.effect?.effectParams, "count", 0);
        args["effectDiePickRule"] = GetString(rule.effect?.effectParams, "diePickRule");
        args["effectThreshold"] = GetInt(rule.effect?.effectParams, "threshold", 0);
        args["effectBonusPerMatch"] = GetInt(rule.effect?.effectParams, "bonusPerMatch", 0);
        return args;
    }

    static int ToInt(double? value)
    {
        if (!value.HasValue)
            return 0;

        return (int)Math.Round(value.Value, MidpointRounding.AwayFromZero);
    }

    static int GetInt(JObject source, string key, int defaultValue)
    {
        if (source == null || string.IsNullOrWhiteSpace(key))
            return defaultValue;

        var token = source[key];
        if (token == null || token.Type == JTokenType.Null)
            return defaultValue;

        if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
            return (int)Math.Round(token.Value<double>(), MidpointRounding.AwayFromZero);

        if (int.TryParse(token.ToString().Trim(), out int parsed))
            return parsed;

        return defaultValue;
    }

    static string GetString(JObject source, string key)
    {
        if (source == null || string.IsNullOrWhiteSpace(key))
            return string.Empty;

        var token = source[key];
        if (token == null || token.Type == JTokenType.Null)
            return string.Empty;

        return token.ToString().Trim();
    }
}
