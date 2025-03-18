using System.Text.Json;
using System.Text.Json.Nodes;

namespace ReelGrab.Utils;

public static class Filesystem
{
    public static async Task WriteStreamToFileAsync(Stream inputStream, string filePath)
    {
        string? directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        await using FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await inputStream.CopyToAsync(fileStream);
    }

    public static void CreateFile(string filePath)
    {
        string? dir = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (!File.Exists(filePath))
        {
            File.Create(filePath).Dispose();
        }
    }

    public static async Task<bool> EmptyAsync(string filePath)
    {
        return (await File.ReadAllBytesAsync(filePath)).Length == 0;
    }

    public static async Task<bool> IsValidJsonObjectAsync(string filePath)
    {
        try
        {
            var json = JsonSerializer.Deserialize<JsonObject>(await File.ReadAllTextAsync(filePath));
            return json != null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static async Task<JsonObject> ReadJsonObjectFile(string filePath)
    {
        string text = await File.ReadAllTextAsync(filePath);
        try
        {
            return JsonSerializer.Deserialize<JsonObject>(text) ?? throw new InvalidOperationException($"{filePath} does not contain valid JSON");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"{filePath} does not contain valid JSON", ex);
        }
    }

    public static async Task<string?> GetConfigKeyString(string filePath, string key)
    {
        if (!File.Exists(filePath) || await EmptyAsync(filePath))
        {
            return null;
        }
        if (!await IsValidJsonObjectAsync(filePath))
        {
            throw new Exception($"{filePath} does not contain valid JSON, either fix the file or delete it");
        }
        JsonObject json = await ReadJsonObjectFile(filePath);
        return json[key]?.GetValue<string>();
    }

    public static async Task SetConfigKeyString(string filePath, string key, string? value)
    {
        JsonObject json;
        if (!File.Exists(filePath))
        {
            CreateFile(filePath);
            json = new();
        }
        else if (await EmptyAsync(filePath))
        {
            json = new();
        }
        else if (!await IsValidJsonObjectAsync(filePath))
        {
            throw new Exception($"{filePath} does not contain valid JSON, either fix the file or delete it");
        }
        else
        {
            json = JsonSerializer.Deserialize<JsonObject>(await File.ReadAllTextAsync(filePath)) ?? throw new Exception($"{filePath} does not contain valid JSON, either fix the file or delete it");
        }
        json[key] = value;
        await File.WriteAllTextAsync(filePath, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    public static async Task<List<string>> GetConfigKeyListString(string filePath, string key)
    {
        if (!File.Exists(filePath) || await EmptyAsync(filePath))
        {
            return [];
        }
        if (!await IsValidJsonObjectAsync(filePath))
        {
            throw new Exception($"{filePath} does not contain valid JSON, either fix the file or delete it");
        }
        JsonObject json = await ReadJsonObjectFile(filePath);
        if (json.TryGetPropertyValue(key, out JsonNode? node) && node is JsonArray jsonArray)
        {

            return jsonArray.Deserialize<List<string>>()!;
        }
        return [];
    }

    public static async Task SetConfigKeyListString(string filePath, string key, List<string> value)
    {
        JsonObject json;
        if (!File.Exists(filePath))
        {
            CreateFile(filePath);
            json = new();
        }
        else if (await EmptyAsync(filePath))
        {
            json = new();
        }
        else if (!await IsValidJsonObjectAsync(filePath))
        {
            throw new Exception($"{filePath} does not contain valid JSON, either fix the file or delete it");
        }
        else
        {
            json = JsonSerializer.Deserialize<JsonObject>(await File.ReadAllTextAsync(filePath)) ?? throw new Exception($"{filePath} does not contain valid JSON, either fix the file or delete it");
        }
        json[key] = JsonNode.Parse(JsonSerializer.Serialize(value));
        await File.WriteAllTextAsync(filePath, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}