using System.Text.Json;

namespace FloatingAIDesktopWidget;

internal static class JsonFile
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    public static bool TryRead<T>(string path, out T? value)
    {
        value = default;

        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            var json = File.ReadAllText(path);
            value = JsonSerializer.Deserialize<T>(json, ReadOptions);
            return value is not null;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryWrite<T>(string path, T value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, WriteOptions);
            File.WriteAllText(path, json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

