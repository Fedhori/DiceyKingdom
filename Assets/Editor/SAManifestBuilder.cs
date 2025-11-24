#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class SAManifestBuilder
{
    [MenuItem("Tools/Build StreamingAssets Manifest")]
    public static void BuildMenu() => BuildManifest();

    public static void BuildManifest()
    {
        bool Exclude(string p)
        {
            var f = p.Replace("\\","/").ToLowerInvariant();
            if (f.EndsWith("/sa_manifest.json")) return true; // 자기 자신 제외
            if (f.EndsWith(".meta")) return true;             // ★ 폴더/파일 메타 제외
            if (f.EndsWith(".ds_store")) return true;         // mac
            if (f.EndsWith("thumbs.db")) return true;         // win
            return false;
        }
        
        var saRoot = Path.Combine(Application.dataPath, "StreamingAssets");
        if (!Directory.Exists(saRoot)) Directory.CreateDirectory(saRoot);

        var files = Directory.EnumerateFiles(saRoot, "*", SearchOption.AllDirectories)
            .Where(p => !Exclude(p))
            .Select(full =>
            {
                var rel = full.Replace(saRoot + Path.DirectorySeparatorChar, "")
                              .Replace("\\", "/");
                var info = new FileInfo(full);
                return new Entry {
                    path = rel,
                    size = info.Length,
                    md5  = MD5Of(full)
                };
            })
            .ToArray();

        var manifest = new Manifest {
            appVersion = Application.version,
            generatedAt = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            files = files
        };

        var json = JsonUtility.ToJson(manifest, true);
        File.WriteAllText(Path.Combine(saRoot, "sa_manifest.json"), json);
        AssetDatabase.Refresh();
        Debug.Log($"[SAManifestBuilder] {files.Length} files listed.");
    }

    static string MD5Of(string path)
    {
        using var md5 = MD5.Create();
        using var fs = File.OpenRead(path);
        var hash = md5.ComputeHash(fs);
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }

    [System.Serializable] public class Manifest { public string appVersion; public string generatedAt; public Entry[] files; }
    [System.Serializable] public class Entry { public string path; public long size; public string md5; }
}

// 빌드 직전에 자동 생성되게
public class SAManifestPreprocess : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report) => SAManifestBuilder.BuildManifest();
}
#endif
