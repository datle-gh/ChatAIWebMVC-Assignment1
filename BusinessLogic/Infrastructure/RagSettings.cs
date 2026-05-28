using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Infrastructure;

public sealed class RagSettings
{
    public int TopK { get; init; } = 5;

    public decimal SimilarityThreshold { get; init; } = 0.7m;

    public int MaxCitationSnippetLength { get; init; } = 250;

    public int MaxContextChunks { get; init; } = 5;

    public int MaxChunkTokens { get; init; } = 700;

    public int ChunkOverlapTokens { get; init; } = 100;

    public int MinChunkCharacters { get; init; } = 30;

    public static RagSettings FromConfiguration(IConfiguration configuration)
    {
        return new RagSettings
        {
            TopK = ReadInt(configuration, "RagSettings:TopK", 5),
            SimilarityThreshold = ReadDecimal(configuration, "RagSettings:SimilarityThreshold", 0.7m),
            MaxCitationSnippetLength = ReadInt(configuration, "RagSettings:MaxCitationSnippetLength", 250),
            MaxContextChunks = ReadInt(configuration, "RagSettings:MaxContextChunks", 5),
            MaxChunkTokens = ReadInt(configuration, "RagSettings:MaxChunkTokens", 700),
            ChunkOverlapTokens = ReadInt(configuration, "RagSettings:ChunkOverlapTokens", 100),
            MinChunkCharacters = ReadInt(configuration, "RagSettings:MinChunkCharacters", 30)
        };
    }

    private static int ReadInt(IConfiguration configuration, string key, int fallback)
    {
        return int.TryParse(configuration[key], out var value) && value > 0
            ? value
            : fallback;
    }

    private static decimal ReadDecimal(IConfiguration configuration, string key, decimal fallback)
    {
        return decimal.TryParse(configuration[key], out var value)
            ? value
            : fallback;
    }
}
