using BusinessLogic.DTOs.Responses;

namespace BusinessLogic.Services;

public interface IEmbeddingModelRegistry
{
    IEmbeddingService GetDefault();

    IEmbeddingService GetRequired(string modelKey);

    IReadOnlyList<EmbeddingModelDto> GetAvailableModels(bool benchmarkOnly = false);
}
