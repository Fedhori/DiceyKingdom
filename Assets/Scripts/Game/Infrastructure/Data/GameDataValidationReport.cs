using System.Collections.Generic;
using UnityEngine;

namespace Game.Infrastructure.Data
{
    public sealed class GameDataValidationReport
    {
        readonly List<GameDataValidationEntry> errors = new();

        public IReadOnlyList<GameDataValidationEntry> Errors => errors;
        public int ErrorCount => errors.Count;
        public bool HasErrors => errors.Count > 0;

        public void AddError(string code, string path, string id, string message)
        {
            errors.Add(new GameDataValidationEntry
            {
                code = code ?? string.Empty,
                path = path ?? string.Empty,
                id = id ?? string.Empty,
                message = message ?? string.Empty
            });
        }

        public void LogErrorsToConsole()
        {
            for (int i = 0; i < errors.Count; i++)
            {
                Debug.LogError($"[validate_data] {errors[i].ToLogLine()}");
            }
        }
    }
}
