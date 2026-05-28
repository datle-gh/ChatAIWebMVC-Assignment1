using BusinessLogic.DTOs.Responses;

namespace BusinessLogic.Services;

public sealed class SqlVectorSearchService : IVectorSearchService
{
    private readonly SqlVectorStoreService _sqlVectorStoreService;

    public SqlVectorSearchService(SqlVectorStoreService sqlVectorStoreService)
    {
        _sqlVectorStoreService = sqlVectorStoreService;
    }

    public async Task<IReadOnlyList<RetrievedChunkDto>> SearchAsync(
        int subjectId,
        string embeddingModel,
        float[] questionEmbedding,
        int topK,
        CancellationToken cancellationToken = default)
    {
        return await _sqlVectorStoreService.SearchAsync(
            subjectId,
            embeddingModel,
            questionEmbedding,
            topK,
            cancellationToken);
    }
}
