using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class SaveJson
{
    static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        Formatting = Formatting.None,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Include
    };

    public static string Serialize(SaveData data)
    {
        if (data == null)
            return string.Empty;

        var serializer = JsonSerializer.Create(Settings);
        var token = JToken.FromObject(data, serializer);
        var canonical = Canonicalize(token);
        return canonical.ToString(Formatting.None);
    }

    public static string SerializeForChecksum(SaveData data)
    {
        if (data == null)
            return string.Empty;

        var clone = CloneWithoutChecksum(data);
        return Serialize(clone);
    }

    public static string ComputeChecksum(string payload)
    {
        if (payload == null)
            payload = string.Empty;

        using var sha = SHA256.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        byte[] hash = sha.ComputeHash(bytes);
        return ToHex(hash);
    }

    static SaveData CloneWithoutChecksum(SaveData data)
    {
        var json = JsonConvert.SerializeObject(data, Settings);
        var clone = JsonConvert.DeserializeObject<SaveData>(json, Settings) ?? new SaveData();
        if (clone.meta != null)
            clone.meta.checksum = string.Empty;
        return clone;
    }

    static JToken Canonicalize(JToken token)
    {
        switch (token.Type)
        {
            case JTokenType.Object:
            {
                var obj = (JObject)token;
                var props = obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal);
                var newObj = new JObject();
                foreach (var prop in props)
                    newObj.Add(prop.Name, Canonicalize(prop.Value));
                return newObj;
            }
            case JTokenType.Array:
            {
                var arr = (JArray)token;
                var newArr = new JArray();
                foreach (var item in arr)
                    newArr.Add(Canonicalize(item));
                return newArr;
            }
            default:
                return token.DeepClone();
        }
    }

    static string ToHex(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return string.Empty;

        var sb = new StringBuilder(bytes.Length * 2);
        for (int i = 0; i < bytes.Length; i++)
            sb.Append(bytes[i].ToString("x2"));
        return sb.ToString();
    }
}
