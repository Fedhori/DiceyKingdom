using System;
using System.IO;
using UnityEngine;

namespace Game.Infrastructure.Data
{
    public sealed class SaCacheGameDataSource : IGameDataSource
    {
        public bool Exists(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return false;
            }

            return File.Exists(BuildStreamingAssetsPath(relativePath)) ||
                   File.Exists(BuildPersistentPath(relativePath));
        }

        public bool TryReadText(string relativePath, out string json, out string errorMessage)
        {
            json = string.Empty;
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                errorMessage = "Path is empty.";
                return false;
            }

            string fullPath = ResolveExistingPath(relativePath);
            if (string.IsNullOrEmpty(fullPath))
            {
                errorMessage = $"File does not exist: {relativePath}";
                return false;
            }

            try
            {
                json = File.ReadAllText(fullPath);
                if (string.IsNullOrEmpty(json))
                {
                    errorMessage = $"File is empty: {relativePath}";
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        static string ResolveExistingPath(string relativePath)
        {
            string streamingPath = BuildStreamingAssetsPath(relativePath);
            if (File.Exists(streamingPath))
            {
                return streamingPath;
            }

            string persistentPath = BuildPersistentPath(relativePath);
            if (File.Exists(persistentPath))
            {
                return persistentPath;
            }

            return string.Empty;
        }

        static string BuildStreamingAssetsPath(string relativePath)
        {
            return Path.Combine(UnityEngine.Application.streamingAssetsPath, Normalize(relativePath));
        }

        static string BuildPersistentPath(string relativePath)
        {
            return Path.Combine(UnityEngine.Application.persistentDataPath, Normalize(relativePath));
        }

        static string Normalize(string relativePath)
        {
            return relativePath.Replace('\\', '/');
        }
    }
}
