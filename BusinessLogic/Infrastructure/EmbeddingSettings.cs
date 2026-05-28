using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Infrastructure;

public sealed class EmbeddingSettings
{
    public string Provider { get; init; } = "Ollama";

    public string DefaultModel { get; init; } = "bge-m3";

    public string DefaultModelKey { get; init; } = "bge-m3";

    public OllamaEmbeddingSettings Ollama { get; init; } = new();

    public PhoBertEmbeddingSettings PhoBert { get; init; } = new();

    public IReadOnlyList<EmbeddingModelSettings> Models { get; init; } = [];

    public static EmbeddingSettings FromConfiguration(IConfiguration configuration)
    {
        var ollamaSettings = new OllamaEmbeddingSettings
        {
            BaseUrl = ReadString(configuration, "Embedding:Ollama:BaseUrl", "http://localhost:11434"),
            Model = ReadString(configuration, "Embedding:Ollama:Model", "bge-m3"),
            Truncate = ReadBool(configuration, "Embedding:Ollama:Truncate", true),
            MaxInputCharacters = ReadInt(configuration, "Embedding:Ollama:MaxInputCharacters", 3500),
            MaxRetryCount = ReadInt(configuration, "Embedding:Ollama:MaxRetryCount", 2),
            RetryDelayMilliseconds = ReadInt(configuration, "Embedding:Ollama:RetryDelayMilliseconds", 600)
        };

        var phoBertSettings = new PhoBertEmbeddingSettings
        {
            BaseUrl = ReadString(configuration, "Embedding:PhoBert:BaseUrl", "http://localhost:8001"),
            Model = ReadString(configuration, "Embedding:PhoBert:Model", "vinai/phobert-base"),
            Enabled = ReadBool(configuration, "Embedding:PhoBert:Enabled", false)
        };

        var defaultModelKey = ReadString(configuration, "Embedding:DefaultModelKey", string.Empty);
        if (string.IsNullOrWhiteSpace(defaultModelKey))
        {
            defaultModelKey = ReadString(configuration, "Embedding:DefaultModel", ollamaSettings.Model);
        }

        var configuredModels = ReadModels(configuration).ToList();
        if (configuredModels.Count == 0)
        {
            configuredModels.Add(new EmbeddingModelSettings
            {
                Key = "bge-m3",
                Provider = "Ollama",
                Model = ollamaSettings.Model,
                BaseUrl = ollamaSettings.BaseUrl,
                Dimension = 1024,
                Enabled = true,
                IncludeInBenchmark = true,
                MaxInputCharacters = ollamaSettings.MaxInputCharacters
            });

            configuredModels.Add(new EmbeddingModelSettings
            {
                Key = "phobert-base",
                Provider = "PhoBert",
                Model = phoBertSettings.Model,
                BaseUrl = phoBertSettings.BaseUrl,
                Dimension = 768,
                Enabled = phoBertSettings.Enabled,
                IncludeInBenchmark = true,
                MaxInputCharacters = 2000
            });
        }

        return new EmbeddingSettings
        {
            Provider = ReadString(configuration, "Embedding:Provider", "Ollama"),
            DefaultModel = ReadString(configuration, "Embedding:DefaultModel", "bge-m3"),
            DefaultModelKey = defaultModelKey,
            Ollama = ollamaSettings,
            PhoBert = phoBertSettings,
            Models = configuredModels
        };
    }

    private static IEnumerable<EmbeddingModelSettings> ReadModels(IConfiguration configuration)
    {
        foreach (var section in configuration.GetSection("Embedding:Models").GetChildren())
        {
            var key = ReadString(section, "Key", string.Empty);
            var provider = ReadString(section, "Provider", string.Empty);
            var model = ReadString(section, "Model", string.Empty);
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(model))
            {
                continue;
            }

            yield return new EmbeddingModelSettings
            {
                Key = key,
                Provider = provider,
                Model = model,
                BaseUrl = ReadString(section, "BaseUrl", string.Empty),
                Dimension = ReadInt(section, "Dimension", 0),
                Enabled = ReadBool(section, "Enabled", true),
                IncludeInBenchmark = ReadBool(section, "IncludeInBenchmark", true),
                MaxInputCharacters = ReadInt(section, "MaxInputCharacters", 3000)
            };
        }
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

public sealed class EmbeddingModelSettings
{
    public string Key { get; init; } = string.Empty;

    public string Provider { get; init; } = string.Empty;

    public string Model { get; init; } = string.Empty;

    public string BaseUrl { get; init; } = string.Empty;

    public int Dimension { get; init; }

    public bool Enabled { get; init; } = true;

    public bool IncludeInBenchmark { get; init; } = true;

    public int MaxInputCharacters { get; init; } = 3000;
}

public sealed class OllamaEmbeddingSettings
{
    public string BaseUrl { get; init; } = "http://localhost:11434";

    public string Model { get; init; } = "bge-m3";

    public bool Truncate { get; init; } = true;

    public int MaxInputCharacters { get; init; } = 3500;

    public int MaxRetryCount { get; init; } = 2;

    public int RetryDelayMilliseconds { get; init; } = 600;
}

public sealed class PhoBertEmbeddingSettings
{
    public string BaseUrl { get; init; } = "http://localhost:8001";

    public string Model { get; init; } = "vinai/phobert-base";

    public bool Enabled { get; init; }
}
