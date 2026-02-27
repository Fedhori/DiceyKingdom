using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Localization;

namespace Game.Presentation.Localization
{
    public sealed class UnityLocalizedTextResolver : ILocalizedTextResolver
    {
        static readonly Regex unresolvedPlaceholderPattern = new(@"\{0\.[^}]+\}", RegexOptions.Compiled);

        public string ResolveRequired(string tableName, string key, object arguments = null)
        {
            return ResolveInternal(tableName, key, arguments, warnIfMissing: false, returnMissingMarker: true);
        }

        public string ResolveOptional(string tableName, string key, object arguments = null, bool warnIfMissing = false)
        {
            return ResolveInternal(tableName, key, arguments, warnIfMissing, returnMissingMarker: false);
        }

        string ResolveInternal(
            string tableName,
            string key,
            object arguments,
            bool warnIfMissing,
            bool returnMissingMarker)
        {
            if (string.IsNullOrWhiteSpace(tableName) || string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            try
            {
                var localizedString = new LocalizedString(tableName, key);
                if (arguments != null)
                {
                    localizedString.Arguments = new object[] { arguments };
                }

                string resolved = localizedString.GetLocalizedString();
                if (!IsMissingResult(resolved, key))
                {
                    WarnIfUnresolvedPlaceholderRemains(tableName, key, arguments, resolved);
                    return resolved;
                }
            }
            catch (Exception exception)
            {
                if (warnIfMissing)
                {
                    Debug.LogWarning(
                        $"[Localization] Optional localized text resolve failed. table='{tableName}', key='{key}', reason='{exception.Message}'");
                    return string.Empty;
                }

                if (!returnMissingMarker)
                {
                    return string.Empty;
                }

                Debug.LogError(
                    $"[Localization] Failed to resolve localized text. table='{tableName}', key='{key}', reason='{exception.Message}'");
                return returnMissingMarker ? $"[missing:{key}]" : string.Empty;
            }

            if (warnIfMissing)
            {
                Debug.LogWarning($"[Localization] Optional localized text is missing. table='{tableName}', key='{key}'");
                return string.Empty;
            }

            if (!returnMissingMarker)
            {
                return string.Empty;
            }

            Debug.LogError($"[Localization] Missing localized text. table='{tableName}', key='{key}'");
            return returnMissingMarker ? $"[missing:{key}]" : string.Empty;
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

        static void WarnIfUnresolvedPlaceholderRemains(string tableName, string key, object arguments, string resolved)
        {
            if (arguments == null || string.IsNullOrWhiteSpace(resolved))
            {
                return;
            }

            if (!unresolvedPlaceholderPattern.IsMatch(resolved))
            {
                return;
            }

            Debug.LogWarning(
                $"[Localization] Unresolved placeholder remained after formatting. Ensure Smart Format entry is configured. table='{tableName}', key='{key}'");
        }
    }
}
