using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

namespace Game.UI.Tooltip
{
    public sealed class SimpleTooltipProvider : MonoBehaviour, ITooltipContentProvider
    {
        [SerializeField] string titleTable = "tooltip";
        [SerializeField] string titleKey;
        [SerializeField] string bodyTable = "tooltip";
        [SerializeField] string bodyKey;

        static readonly HashSet<string> warnedMissingKeys = new(StringComparer.Ordinal);

        public bool TryBuildTooltipModel(out TooltipModel model)
        {
            string title = ResolveOptionalLocalizedText(titleTable, titleKey);
            string body = ResolveOptionalLocalizedText(bodyTable, bodyKey);
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
            {
                model = default;
                return false;
            }

            model = new TooltipModel(title, body, TooltipKind.Simple);
            return true;
        }

        static string ResolveOptionalLocalizedText(string table, string key)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            try
            {
                var localizedString = new LocalizedString(table, key);
                string resolved = localizedString.GetLocalizedString();
                if (!IsMissingResult(resolved, key))
                {
                    return resolved;
                }

                WarnMissingOnce(table, key);
                return string.Empty;
            }
            catch (Exception exception)
            {
                WarnMissingOnce(table, key, exception.Message);
                return string.Empty;
            }
        }

        static bool IsMissingResult(string resolved, string key)
        {
            if (string.IsNullOrWhiteSpace(resolved))
            {
                return true;
            }

            if (string.Equals(resolved, key, StringComparison.Ordinal))
            {
                return true;
            }

            return resolved.IndexOf("No translation found", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static void WarnMissingOnce(string table, string key, string reason = "")
        {
            string cacheKey = $"{table}:{key}";
            if (!warnedMissingKeys.Add(cacheKey))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                Debug.LogWarning($"[SimpleTooltipProvider] Missing localized text. table='{table}', key='{key}'");
                return;
            }

            Debug.LogWarning(
                $"[SimpleTooltipProvider] Failed to resolve localized text. table='{table}', key='{key}', reason='{reason}'");
        }
    }
}
