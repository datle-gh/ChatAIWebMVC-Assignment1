using BusinessLogic.DTOs.Responses;

namespace BusinessLogic.Services;

public interface IVectorSearchService
{
    Task<IReadOnlyList<RetrievedChunkDto>> SearchAsync(
        int subjectId,
        string embeddingModel,
        float[] questionEmbedding,
        int topK,
        CancellationToken cancellationToken = default);
}
