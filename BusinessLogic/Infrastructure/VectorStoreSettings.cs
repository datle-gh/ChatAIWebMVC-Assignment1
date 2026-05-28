using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Infrastructure;

public sealed class VectorStoreSettings
{
    public string Provider { get; init; } = "Sql";

    public bool DualWrite { get; init; }

    public QdrantSettings Qdrant { get; init; } = new();

    public static VectorStoreSettings FromConfiguration(IConfiguration configuration)
    {
        return new VectorStoreSettings
        {
            Provider = ReadString(configuration, "VectorStore:Provider", "Sql"),
            DualWrite = ReadBool(configuration, "VectorStore:DualWrite", false),
            Qdrant = new QdrantSettings
            {
                Host = ReadString(configuration, "Qdrant:Host", "localhost"),
                Port = ReadInt(configuration, "Qdrant:Port", 6333),
                UseHttps = ReadBool(configuration, "Qdrant:UseHttps", false),
                ApiKey = ReadString(configuration, "Qdrant:ApiKey", string.Empty),
                CollectionPrefix = ReadString(configuration, "Qdrant:CollectionPrefix", "chataiweb")
            }
        };
    }

    private static string ReadString(IConfiguration configuration, string key, string fallback)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static bool ReadBool(IConfiguration configuration, string key, bool fallback)
    {
        return bool.TryParse(configuration[key], out var value) ? value : fallback;
    }

    private static int ReadInt(IConfiguration configuration, string key, int fallback)
    {
        return int.TryParse(configuration[key], out var value) && value > 0
            ? value
            : fallback;
    }
}

public sealed class QdrantSettings
{
    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 6333;

    public bool UseHttps { get; init; }

    public string ApiKey { get; init; } = string.Empty;

    public string CollectionPrefix { get; init; } = "chataiweb";

    public string BaseUrl => $"{(UseHttps ? "https" : "http")}://{Host}:{Port}";
}
