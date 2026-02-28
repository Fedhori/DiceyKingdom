using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using System.Security.Cryptography;




namespace Game.Data
{
    public class SaOptions
    {
        public bool forceRefresh = false;            
        public bool refreshIfAppVersionChanged = true;
        public bool verifyHash = true;               
        public bool cleanStale = false;              
    }

    [Serializable] class SaState { public string appVersion; public string manifestHash; }




    public static class SaCache
    {
        [Serializable] class Manifest { public string appVersion; public Entry[] files; }
        [Serializable] class Entry { public string path; public long size; public string md5; }

        static bool inited;
        static Manifest manifest;

        
        static Task initTask;
        static int initStarted;
        static TaskCompletionSource<bool> readyTcs = CreateReadyTcs();

        public static Task Ready => readyTcs.Task; 
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic()
        {
            inited = false;
            manifest = null;
            initTask = null;
            initStarted = 0;
            readyTcs = CreateReadyTcs();
        }

        
        public static Task InitAsync(SaOptions opt, Action<float> onProgress = null)
        {
            if (opt == null)
                throw new ArgumentNullException(nameof(opt), "[SACache] InitAsync requires explicit SaOptions.");

            if (Volatile.Read(ref initStarted) == 0)
                if (Interlocked.Exchange(ref initStarted, 1) == 0)
                    initTask = InitImplAsync(opt, onProgress);

            
            if (inited) onProgress?.Invoke(1f);
            return initTask ?? Task.CompletedTask;
        }

        
        public static string Path(string relativePath)
        {
            string path = "";
            if (UnityEngine.Application.isEditor)
                path = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, relativePath);
            else
                path = System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, relativePath);
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

        
        public static async Task<string> ReadTextAsync(string relativePath)
        {
            await Ready;
            var dst = Path(relativePath);
            if (!File.Exists(dst))
                await CopyOneAsync(relativePath); 
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

        

        static async Task InitImplAsync(SaOptions opt, Action<float> onProgress)
        {
            try
            {
                
                var manifestJson = await LoadSaAsync("sa_manifest.json");
                manifest = JsonConvert.DeserializeObject<Manifest>(manifestJson);
                if (manifest?.files == null || manifest.files.Length == 0)
                {
                    inited = true;
                    onProgress?.Invoke(1f);
                    readyTcs.TrySetResult(true);
                    return;
                }

                
                var state = LoadState();
                var manifestHash = MD5String(manifestJson);
                bool needRefresh = opt.forceRefresh
                    || (opt.refreshIfAppVersionChanged && state?.appVersion != UnityEngine.Application.version)
                    || (state?.manifestHash != manifestHash);

                
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

                
                if (opt.cleanStale) CleanStaleFiles();

                
                SaveState(new SaState { appVersion = UnityEngine.Application.version, manifestHash = manifestHash });

                inited = true;
                onProgress?.Invoke(1f);

                readyTcs.TrySetResult(true); 
            }
            catch (Exception ex)
            {
                readyTcs.TrySetException(ex); 
                throw;
            }
        }

        static TaskCompletionSource<bool> CreateReadyTcs()
        {
            return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        static async Task CopyOneAsync(Entry e)
        {
            
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

        
        static async Task CopyOneAsync(string relativePath)
        {
            var e = new Entry { path = relativePath };
            await CopyOneAsync(e);
        }

        static void CleanStaleFiles()
        {
            var root = UnityEngine.Application.persistentDataPath.Replace("\\", "/");
            var white = manifest.files.Select(f => (root + "/" + f.path).Replace("//", "/")).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                var norm = f.Replace("\\", "/");
                if (norm.EndsWith("/sa_state.json")) continue; 
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
            var sa = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, relativePath).Replace("\\", "/");
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
            var sa = System.IO.Path.Combine(UnityEngine.Application.streamingAssetsPath, relativePath).Replace("\\", "/");
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

}
