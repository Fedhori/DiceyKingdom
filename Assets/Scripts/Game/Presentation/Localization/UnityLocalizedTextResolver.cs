using System;
using UnityEngine;
using UnityEngine.Localization;

namespace Game.Presentation.Localization
{
    public sealed class UnityLocalizedTextResolver : ILocalizedTextResolver
    {
        public string Resolve(string tableName, string key, object arguments = null)
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
                    return resolved;
                }
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[Localization] Failed to resolve localized text. table='{tableName}', key='{key}', reason='{exception.Message}'");
            }

            Debug.LogError($"[Localization] Missing localized text. table='{tableName}', key='{key}'");
            return $"[missing:{key}]";
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
    }
}
