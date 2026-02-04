using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectP.Common.Editor
{
    [InitializeOnLoad]
    public static class ProjectPCommonSetup
    {
        static bool hasRun;

        static ProjectPCommonSetup()
        {
            EditorApplication.delayCall += TrySetup;
        }

        static void TrySetup()
        {
            if (hasRun)
            {
                return;
            }

            hasRun = true;

            if (Application.isBatchMode)
            {
                return;
            }

            var packageRoot = GetPackageRoot();
            if (string.IsNullOrEmpty(packageRoot))
            {
                Debug.LogWarning("[ProjectP.Common] package root not found. Setup skipped.");
                return;
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogWarning("[ProjectP.Common] project root not found. Setup skipped.");
                return;
            }

            EnsureDirectories(projectRoot);
            EnsureDocs(projectRoot, packageRoot);

            AssetDatabase.Refresh();
        }

        static string GetPackageRoot()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(ProjectPCommonSetup).Assembly);
            return info?.resolvedPath;
        }

        static void EnsureDirectories(string projectRoot)
        {
            CreateIfMissing(Path.Combine(projectRoot, "Docs"));

            CreateIfMissing(Path.Combine(projectRoot, "Assets", "Scenes"));
            CreateIfMissing(Path.Combine(projectRoot, "Assets", "Scripts"));
            CreateIfMissing(Path.Combine(projectRoot, "Assets", "Prefabs"));
            CreateIfMissing(Path.Combine(projectRoot, "Assets", "Resources"));
            CreateIfMissing(Path.Combine(projectRoot, "Assets", "Resources", "Music"));
            CreateIfMissing(Path.Combine(projectRoot, "Assets", "Resources", "SFX"));
            CreateIfMissing(Path.Combine(projectRoot, "Assets", "Sprites"));
        }

        static void EnsureDocs(string projectRoot, string packageRoot)
        {
            var docsRoot = Path.Combine(projectRoot, "Docs");
            var templateRoot = Path.Combine(packageRoot, "DocsTemplates");
            var packageDocsRoot = Path.Combine(packageRoot, "Docs");

            CopyIfMissing(Path.Combine(packageDocsRoot, "GENERAL_RULES.md"), Path.Combine(docsRoot, "GENERAL_RULES.md"));
            CopyIfMissing(Path.Combine(templateRoot, "GAME_STRUCTURE.md"), Path.Combine(docsRoot, "GAME_STRUCTURE.md"));
            CopyIfMissing(Path.Combine(templateRoot, "PROJECT_MAP.md"), Path.Combine(docsRoot, "PROJECT_MAP.md"));
        }

        static void CreateIfMissing(string path)
        {
            if (Directory.Exists(path))
            {
                return;
            }

            Directory.CreateDirectory(path);
        }

        static void CopyIfMissing(string sourcePath, string destPath)
        {
            if (File.Exists(destPath))
            {
                return;
            }

            if (!File.Exists(sourcePath))
            {
                Debug.LogWarning($"[ProjectP.Common] template not found: {sourcePath}");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destPath) ?? string.Empty);
            File.Copy(sourcePath, destPath, overwrite: false);
        }
    }
}
