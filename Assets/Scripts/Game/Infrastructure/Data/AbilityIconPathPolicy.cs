using System;

namespace Game.Infrastructure.Data
{
    public static class AbilityIconPathPolicy
    {
        public const string IconFolderPath = "Data/icons";
        public const string FileExtension = ".png";
        public const string DefaultIconId = "icon.default";
        public const string DefaultIconPath = IconFolderPath + "/" + DefaultIconId + FileExtension;

        public static bool TryBuildPath(string iconId, out string relativePath)
        {
            relativePath = string.Empty;
            if (string.IsNullOrWhiteSpace(iconId))
            {
                return false;
            }

            if (iconId.Contains("..", StringComparison.Ordinal) ||
                iconId.IndexOf('/') >= 0 ||
                iconId.IndexOf('\\') >= 0 ||
                iconId.IndexOf(':') >= 0)
            {
                return false;
            }

            relativePath = $"{IconFolderPath}/{iconId}{FileExtension}";
            return true;
        }
    }
}
