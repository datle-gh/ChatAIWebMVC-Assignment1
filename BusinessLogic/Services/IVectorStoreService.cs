using BusinessLogic.DTOs.Responses;
using BusinessObject.Entities;

namespace BusinessLogic.Services;

public interface IVectorStoreService
{
    string Name { get; }

    Task UpsertAsync(
        string embeddingModel,
        string provider,
        IReadOnlyList<DocumentChunk> chunks,
        IReadOnlyList<float[]> embeddings,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RetrievedChunkDto>> SearchAsync(
        int subjectId,
        string embeddingModel,
        float[] questionEmbedding,
        int topK,
        CancellationToken cancellationToken = default);
}
