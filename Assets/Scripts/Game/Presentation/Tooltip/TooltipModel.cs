using System.Collections.Generic;

namespace Game.UI.Tooltip
{
    public enum TooltipKind
    {
        Simple
    }

    public readonly struct TooltipKeywordEntry
    {
        public readonly string titleKey;
        public readonly string bodyKey;
        public readonly Dictionary<string, object> arguments;

        public TooltipKeywordEntry(string titleKey, string bodyKey, Dictionary<string, object> arguments = null)
        {
            this.titleKey = titleKey;
            this.bodyKey = bodyKey;
            this.arguments = arguments;
        }
    }

    public readonly struct TooltipModel
    {
        public readonly string title;
        public readonly string body;
        public readonly TooltipKind kind;
        public readonly IReadOnlyList<TooltipKeywordEntry> keywordEntries;

        public TooltipModel(
            string title,
            string body,
            TooltipKind kind,
            IReadOnlyList<TooltipKeywordEntry> keywordEntries = null)
        {
            this.title = title;
            this.body = body;
            this.kind = kind;
            this.keywordEntries = keywordEntries;
        }
    }

}
