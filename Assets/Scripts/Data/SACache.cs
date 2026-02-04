using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using System.Security.Cryptography;

public class SaOptions
{
    public bool forceRefresh = false;            // 강제 덮어쓰기
    public bool refreshIfAppVersionChanged = true;
    public bool verifyHash = true;               // 해시 검증 후 불일치시 갱신
    public bool cleanStale = false;              // persistent에 불필요 파일 삭제
}

[Serializable] class SaState { public string appVersion; public string manifestHash; }

public static class SaCache
{
    [Serializable] class Manifest { public string appVersion; public Entry[] files; }
    [Serializable] class Entry { public string path; public long size; public string md5; }

    static bool inited;
    static Manifest manifest;

    // --- Ready 게이트 ---
    static Task initTask;
    static int initStarted;
    static readonly TaskCompletionSource<bool> ReadyTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public static Task Ready => ReadyTcs.Task; // 외부에서 기다릴 포인트

    // 앱 시작 시 1회 호출(중복 안전)
    public static Task InitAsync(SaOptions opt = null, Action<float> onProgress = null)
    {
        if (Volatile.Read(ref initStarted) == 0)
            if (Interlocked.Exchange(ref initStarted, 1) == 0)
                initTask = InitImplAsync(opt, onProgress);

        // 이미 시작된 경우: 완료되면 onProgress=1.0 한 번 호출(선택)
        if (inited) onProgress?.Invoke(1f);
        return initTask ?? Task.CompletedTask;
    }

    // 항상 persistent 경로 반환 (부모 폴더는 여기서 생성)
    public static string Path(string relativePath)
    {
        string path = "";
        if (Application.isEditor)
            path = System.IO.Path.Combine(Application.streamingAssetsPath, relativePath);
        else
            path = System.IO.Path.Combine(Application.persistentDataPath, relativePath);
        var dir = System.IO.Path.GetDirectoryName(path);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return path;
    }
    
    public static string ReadText(string relativePath)
    {
        return File.ReadAllText(Path(relativePath));
    }

    public static byte[] ReadBytes(string relativePath)
    {
        return File.ReadAllBytes(Path(relativePath));
    }

    public static bool Exists(string relativePath)
    {
        return File.Exists(Path(relativePath));
    }

    // ---- 비동기 API: Ready 대기 + 없으면 즉시 복사 ----
    public static async Task<string> ReadTextAsync(string relativePath)
    {
        await Ready;
        var dst = Path(relativePath);
        if (!File.Exists(dst))
            await CopyOneAsync(relativePath); // copy-on-demand
        if (!File.Exists(dst))
            throw new FileNotFoundException($"[SACache] missing: {dst}");
        return File.ReadAllText(dst);
    }

    public static async Task<byte[]> ReadBytesAsync(string relativePath)
    {
        await Ready;
        var dst = Path(relativePath);
        if (!File.Exists(dst))
            await CopyOneAsync(relativePath);
        if (!File.Exists(dst))
            throw new FileNotFoundException($"[SACache] missing: {dst}");
        return File.ReadAllBytes(dst);
    }

    // ---------- 내부 구현 ----------

    static async Task InitImplAsync(SaOptions opt, Action<float> onProgress)
    {
        try
        {
            opt ??= new SaOptions();

            // 1) 매니페스트 읽기
            var manifestJson = await LoadSaAsync("sa_manifest.json");
            manifest = JsonUtility.FromJson<Manifest>(manifestJson);
            if (manifest?.files == null || manifest.files.Length == 0)
            {
                inited = true;
                onProgress?.Invoke(1f);
                ReadyTcs.TrySetResult(true);
                return;
            }

            // 2) state 비교 (버전/매니페스트 해시)
            var state = LoadState();
            var manifestHash = MD5String(manifestJson);
            bool needRefresh = opt.forceRefresh
                || (opt.refreshIfAppVersionChanged && state?.appVersion != Application.version)
                || (state?.manifestHash != manifestHash);

            // 3) 복사
            float total = Math.Max(1, manifest.files.Length);
            for (int i = 0; i < manifest.files.Length; i++)
            {
                var e = manifest.files[i];
                bool overwrite = needRefresh;

                if (opt.verifyHash && !overwrite)
                {
                    var dst = Path(e.path);
                    if (File.Exists(dst))
                    {
                        var ok = (MD5OfFile(dst) == e.md5);
                        overwrite = !ok;
                    }
                    else overwrite = true;
                }

                if (overwrite) await CopyOneAsync(e);
                onProgress?.Invoke((i + 1) / total);
            }

            // 4) 필요없어진 파일 정리(옵션)
            if (opt.cleanStale) CleanStaleFiles();

            // 5) state 저장
            SaveState(new SaState { appVersion = Application.version, manifestHash = manifestHash });

            inited = true;
            onProgress?.Invoke(1f);

            ReadyTcs.TrySetResult(true); // Ready 통지
        }
        catch (Exception ex)
        {
            ReadyTcs.TrySetException(ex); // 실패 전파
            throw;
        }
    }

    static async Task CopyOneAsync(Entry e)
    {
        // 에디터/시스템 파일 스킵(안전장치)
        var lower = e.path.ToLowerInvariant();
        if (lower.EndsWith(".meta") || lower.EndsWith(".ds_store") || lower.EndsWith("thumbs.db"))
        {
            Debug.Log($"[SACache] skip editor file: {e.path}");
            return;
        }

        var bytes = await LoadSaBytesAsync(e.path);
        var dst = Path(e.path);
        File.WriteAllBytes(dst, bytes);
    }

    // string 오버로드(즉시 복사용)
    static async Task CopyOneAsync(string relativePath)
    {
        var e = new Entry { path = relativePath };
        await CopyOneAsync(e);
    }

    static void CleanStaleFiles()
    {
        var root = Application.persistentDataPath.Replace("\\", "/");
        var white = manifest.files.Select(f => (root + "/" + f.path).Replace("//", "/")).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var norm = f.Replace("\\", "/");
            if (norm.EndsWith("/sa_state.json")) continue; // 상태 파일 제외
            if (!white.Contains(norm)) try { File.Delete(f); } catch { }
        }
    }

    static SaState LoadState()
    {
        var p = Path("sa_state.json");
        return File.Exists(p) ? JsonUtility.FromJson<SaState>(File.ReadAllText(p)) : null;
    }

    static void SaveState(SaState s) => File.WriteAllText(Path("sa_state.json"), JsonUtility.ToJson(s));

    static async Task<string> LoadSaAsync(string relativePath)
    {
        var sa = System.IO.Path.Combine(Application.streamingAssetsPath, relativePath).Replace("\\", "/");
        if (sa.Contains("://") || sa.Contains("jar:"))
        {
            using var req = UnityWebRequest.Get(sa);
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();
            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"[SACache] UWR fail: {relativePath}\n{req.error}");
            return req.downloadHandler.text;
        }
        return File.ReadAllText(sa);
    }

    static async Task<byte[]> LoadSaBytesAsync(string relativePath)
    {
        var sa = System.IO.Path.Combine(Application.streamingAssetsPath, relativePath).Replace("\\", "/");
        if (sa.Contains("://") || sa.Contains("jar:"))
        {
            using var req = UnityWebRequest.Get(sa);
            var op = req.SendWebRequest();
            while (!op.isDone) await Task.Yield();
            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception($"[SACache] UWR fail: {relativePath}\n{req.error}");
            return req.downloadHandler.data;
        }
        return File.ReadAllBytes(sa);
    }

    static string MD5String(string s)
    {
        using var md5 = MD5.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(s);
        var hash = md5.ComputeHash(bytes);
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }

    static string MD5OfFile(string path)
    {
        using var md5 = MD5.Create();
        using var fs = File.OpenRead(path);
        var hash = md5.ComputeHash(fs);
        return string.Concat(hash.Select(b => b.ToString("x2")));
    }
}
