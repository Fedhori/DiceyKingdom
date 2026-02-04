using System;
using System.Collections.Generic;
using System.Globalization;

public sealed class SaveValidationError
{
    public string FieldPath { get; }
    public string Expected { get; }
    public string Actual { get; }
    public string Code { get; }

    public SaveValidationError(string fieldPath, string expected, string actual, string code)
    {
        FieldPath = fieldPath ?? string.Empty;
        Expected = expected ?? string.Empty;
        Actual = actual ?? string.Empty;
        Code = code ?? string.Empty;
    }
}

public sealed class SaveValidationResult
{
    readonly List<SaveValidationError> errors = new();
    public IReadOnlyList<SaveValidationError> Errors => errors;
    public bool IsValid => errors.Count == 0;

    public void AddError(string fieldPath, string expected, string actual, string code)
    {
        errors.Add(new SaveValidationError(fieldPath, expected, actual, code));
    }
}

public static class SaveValidator
{
    public static SaveValidationResult Validate(SaveData data)
    {
        var result = new SaveValidationResult();

        if (data == null)
        {
            result.AddError("root", "non-null SaveData", "null", "null");
            return result;
        }

        ValidateMeta(data.meta, result);

        if (data.payloadJson == null)
            result.AddError("payloadJson", "non-null string", "null", "missing");

        return result;
    }

    static void ValidateMeta(SaveMeta meta, SaveValidationResult result)
    {
        if (meta == null)
        {
            result.AddError("meta", "object", "null", "missing");
            return;
        }

        if (meta.schemaVersion <= 0)
            result.AddError("meta.schemaVersion", ">= 1", FormatValue(meta.schemaVersion), "out_of_range");

        if (string.IsNullOrEmpty(meta.appVersion))
            result.AddError("meta.appVersion", "non-empty string", FormatValue(meta.appVersion), "missing");

        if (meta.timestampUtc <= 0)
            result.AddError("meta.timestampUtc", "> 0", FormatValue(meta.timestampUtc), "out_of_range");

        if (string.IsNullOrEmpty(meta.checksum))
            result.AddError("meta.checksum", "non-empty string", FormatValue(meta.checksum), "missing");
    }

    static string FormatValue(object value)
    {
        if (value == null)
            return "null";

        if (value is string s)
            return s;

        if (value is IFormattable formattable)
            return formattable.ToString(null, CultureInfo.InvariantCulture);

        return value.ToString();
    }
}
