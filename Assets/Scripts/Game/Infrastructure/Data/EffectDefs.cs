using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Infrastructure.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class TimedEffectDef
    {
        [JsonProperty("timing", Required = Required.Always)]
        public string timing = string.Empty;

        [JsonProperty("condition", Required = Required.Default)]
        public ConditionDef condition;

        [JsonProperty("ops", Required = Required.Always)]
        public List<EffectOpDef> ops = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class ConditionDef
    {
        [JsonProperty("type", Required = Required.Always)]
        public string type = string.Empty;

        [JsonProperty("value", Required = Required.Default)]
        public int? value;

        [JsonProperty("count", Required = Required.Default)]
        public int? count;

        [JsonProperty("tag", Required = Required.Default)]
        public string tag;
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EffectBlockDef
    {
        [JsonProperty("ops", Required = Required.Always)]
        public List<EffectOpDef> ops = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EffectOpDef
    {
        [JsonProperty("op", Required = Required.Always)]
        public string op = string.Empty;

        [JsonProperty("side", Required = Required.Default)]
        public string side;

        [JsonProperty("scope", Required = Required.Default)]
        public string scope;

        [JsonProperty("mode", Required = Required.Default)]
        public string mode;

        [JsonProperty("target", Required = Required.Default)]
        public string target;

        [JsonProperty("layer", Required = Required.Default)]
        public string layer;

        [JsonProperty("value", Required = Required.Default)]
        public int? value;

        [JsonProperty("amount", Required = Required.Default)]
        public int? amount;

        [JsonProperty("delta", Required = Required.Default)]
        public int? delta;

        [JsonProperty("transformKind", Required = Required.Default)]
        public string transformKind;

        [JsonProperty("keepAttackResult", Required = Required.Default)]
        public bool? keepAttackResult;

        [JsonProperty("textLocKey", Required = Required.Default)]
        public string textLocKey;

        public bool TryGetAmount(out int resolvedValue)
        {
            if (value.HasValue)
            {
                resolvedValue = value.Value;
                return true;
            }

            if (amount.HasValue)
            {
                resolvedValue = amount.Value;
                return true;
            }

            if (delta.HasValue)
            {
                resolvedValue = delta.Value;
                return true;
            }

            resolvedValue = 0;
            return false;
        }
    }
}
