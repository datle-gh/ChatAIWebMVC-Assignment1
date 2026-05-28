namespace BusinessLogic.Services;

public interface IEmbeddingService
{
    string ModelKey { get; }

    string ModelName { get; }

    string ProviderName { get; }

    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}
