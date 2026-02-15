using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using System.Security.Cryptography;

/// <summary>
/// Core class that defines sa responsibilities.
/// </summary>
public class SaOptions
{
    public bool forceRefresh = false;            // 媛뺤젣 ??뼱?곌린
    public bool refreshIfAppVersionChanged = true;
    public bool verifyHash = true;               // ?댁떆 寃利???遺덉씪移섏떆 媛깆떊
    public bool cleanStale = false;              // persistent??遺덊븘???뚯씪 ??젣
}

[Serializable] class SaState { public string appVersion; public string manifestHash; }

/// <summary>
/// Caches StreamingAssets payloads into persistent storage for cross-platform runtime access.
/// </summary>
public static class SaCache
{
    [Serializable] class Manifest { public string appVersion; public Entry[] files; }
    [Serializable] class Entry { public string path; public long size; public string md5; }

    static bool inited;
    static Manifest manifest;

    // --- Ready 寃뚯씠??---
    static Task initTask;
    static int initStarted;
    static TaskCompletionSource<bool> readyTcs = CreateReadyTcs();

    public static Task Ready => readyTcs.Task; // ?몃??먯꽌 湲곕떎由??ъ씤??
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic()
    {
        inited = false;
        manifest = null;
        initTask = null;
        initStarted = 0;
        readyTcs = CreateReadyTcs();
    }

    // ???쒖옉 ??1???몄텧(以묐났 ?덉쟾)
    public static Task InitAsync(SaOptions opt, Action<float> onProgress = null)
    {
        if (opt == null)
            throw new ArgumentNullException(nameof(opt), "[SACache] InitAsync requires explicit SaOptions.");

        if (Volatile.Read(ref initStarted) == 0)
            if (Interlocked.Exchange(ref initStarted, 1) == 0)
                initTask = InitImplAsync(opt, onProgress);

        // ?대? ?쒖옉??寃쎌슦: ?꾨즺?섎㈃ onProgress=1.0 ??踰??몄텧(?좏깮)
        if (inited) onProgress?.Invoke(1f);
        return initTask ?? Task.CompletedTask;
    }

    // ??긽 persistent 寃쎈줈 諛섑솚 (遺紐??대뜑???ш린???앹꽦)
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

    // ---- 鍮꾨룞湲?API: Ready ?湲?+ ?놁쑝硫?利됱떆 蹂듭궗 ----
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

    // ---------- ?대? 援ы쁽 ----------

    static async Task InitImplAsync(SaOptions opt, Action<float> onProgress)
    {
        try
        {
            // 1) 留ㅻ땲?섏뒪???쎄린
            var manifestJson = await LoadSaAsync("sa_manifest.json");
            manifest = JsonConvert.DeserializeObject<Manifest>(manifestJson);
            if (manifest?.files == null || manifest.files.Length == 0)
            {
                inited = true;
                onProgress?.Invoke(1f);
                readyTcs.TrySetResult(true);
                return;
            }

            // 2) state 鍮꾧탳 (踰꾩쟾/留ㅻ땲?섏뒪???댁떆)
            var state = LoadState();
            var manifestHash = MD5String(manifestJson);
            bool needRefresh = opt.forceRefresh
                || (opt.refreshIfAppVersionChanged && state?.appVersion != Application.version)
                || (state?.manifestHash != manifestHash);

            // 3) 蹂듭궗
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

            // 4) ?꾩슂?놁뼱吏??뚯씪 ?뺣━(?듭뀡)
            if (opt.cleanStale) CleanStaleFiles();

            // 5) state ???
            SaveState(new SaState { appVersion = Application.version, manifestHash = manifestHash });

            inited = true;
            onProgress?.Invoke(1f);

            readyTcs.TrySetResult(true); // Ready ?듭?
        }
        catch (Exception ex)
        {
            readyTcs.TrySetException(ex); // ?ㅽ뙣 ?꾪뙆
            throw;
        }
    }

    static TaskCompletionSource<bool> CreateReadyTcs()
    {
        return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    static async Task CopyOneAsync(Entry e)
    {
        // ?먮뵒???쒖뒪???뚯씪 ?ㅽ궢(?덉쟾?μ튂)
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

    // string ?ㅻ쾭濡쒕뱶(利됱떆 蹂듭궗??
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
            if (norm.EndsWith("/sa_state.json")) continue; // ?곹깭 ?뚯씪 ?쒖쇅
            if (!white.Contains(norm)) try { File.Delete(f); } catch { }
        }
    }

    static SaState LoadState()
    {
        var p = Path("sa_state.json");
        return File.Exists(p) ? JsonConvert.DeserializeObject<SaState>(File.ReadAllText(p)) : null;
    }

    static void SaveState(SaState s) => File.WriteAllText(Path("sa_state.json"), JsonConvert.SerializeObject(s));

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

