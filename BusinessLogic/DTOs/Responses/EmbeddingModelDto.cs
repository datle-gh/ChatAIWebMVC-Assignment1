namespace BusinessLogic.DTOs.Responses;

public sealed record EmbeddingModelDto(
    string Key,
    string Provider,
    string Model,
    int Dimension,
    bool Enabled,
    bool IncludeInBenchmark);
