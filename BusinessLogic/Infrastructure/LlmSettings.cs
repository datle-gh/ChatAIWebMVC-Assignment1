using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Infrastructure;

public sealed class LlmSettings
{
    public string Provider { get; init; } = "Gemini";

    public string Model { get; init; } = "gemini-2.5-flash";

    public GeminiSettings Gemini { get; init; } = new();

    public static LlmSettings FromConfiguration(IConfiguration configuration)
    {
        return new LlmSettings
        {
            Provider = ReadString(configuration, "Llm:Provider", "Gemini"),
            Model = ReadString(configuration, "Llm:Model", "gemini-2.5-flash"),
            Gemini = new GeminiSettings
            {
                BaseUrl = ReadString(configuration, "Llm:Gemini:BaseUrl", "https://generativelanguage.googleapis.com"),
                ApiKey = ReadString(configuration, "Llm:Gemini:ApiKey", string.Empty),
                Temperature = ReadDouble(configuration, "Llm:Gemini:Temperature", 0.2),
                MaxOutputTokens = ReadInt(configuration, "Llm:Gemini:MaxOutputTokens", 1024)
            }
        };
    }

    private static string ReadString(IConfiguration configuration, string key, string fallback)
    {
        var value = configuration[key];
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static int ReadInt(IConfiguration configuration, string key, int fallback)
    {
        return int.TryParse(configuration[key], out var value) && value > 0
            ? value
            : fallback;
    }

    private static double ReadDouble(IConfiguration configuration, string key, double fallback)
    {
        return double.TryParse(configuration[key], out var value) ? value : fallback;
    }
}

public sealed class GeminiSettings
{
    public string BaseUrl { get; init; } = "https://generativelanguage.googleapis.com";

    public string ApiKey { get; init; } = string.Empty;

    public double Temperature { get; init; } = 0.2;

    public int MaxOutputTokens { get; init; } = 1024;
}
