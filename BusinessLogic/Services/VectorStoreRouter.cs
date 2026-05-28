using BusinessLogic.DTOs.Responses;
using BusinessLogic.Infrastructure;
using BusinessObject.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services;

public sealed class VectorStoreRouter : IVectorStoreService, IVectorSearchService
{
    private readonly SqlVectorStoreService _sqlStore;
    private readonly QdrantVectorStoreService _qdrantStore;
    private readonly VectorStoreSettings _settings;
    private readonly ILogger<VectorStoreRouter> _logger;

    public VectorStoreRouter(
        SqlVectorStoreService sqlStore,
        QdrantVectorStoreService qdrantStore,
        IConfiguration configuration,
        ILogger<VectorStoreRouter> logger)
    {
        _sqlStore = sqlStore;
        _qdrantStore = qdrantStore;
        _settings = VectorStoreSettings.FromConfiguration(configuration);
        _logger = logger;
    }

    public string Name => IsQdrantEnabled ? _qdrantStore.Name : _sqlStore.Name;

    public async Task UpsertAsync(
        string embeddingModel,
        string provider,
        IReadOnlyList<DocumentChunk> chunks,
        IReadOnlyList<float[]> embeddings,
        CancellationToken cancellationToken = default)
    {
        if (!IsQdrantEnabled && !_settings.DualWrite)
        {
            return;
        }

        try
        {
            await _qdrantStore.UpsertAsync(
                embeddingModel,
                provider,
                chunks,
                embeddings,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Qdrant upsert failed for embedding model {EmbeddingModel}. SQL fallback remains available.",
                embeddingModel);
        }
    }

    public async Task<IReadOnlyList<RetrievedChunkDto>> SearchAsync(
        int subjectId,
        string embeddingModel,
        float[] questionEmbedding,
        int topK,
        CancellationToken cancellationToken = default)
    {
        if (!IsQdrantEnabled)
        {
            return await _sqlStore.SearchAsync(subjectId, embeddingModel, questionEmbedding, topK, cancellationToken);
        }

        try
        {
            return await _qdrantStore.SearchAsync(subjectId, embeddingModel, questionEmbedding, topK, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Qdrant search failed for subject {SubjectId}, model {EmbeddingModel}. Falling back to SQL.",
                subjectId,
                embeddingModel);

            return await _sqlStore.SearchAsync(subjectId, embeddingModel, questionEmbedding, topK, cancellationToken);
        }
    }

    private bool IsQdrantEnabled =>
        string.Equals(_settings.Provider, "Qdrant", StringComparison.OrdinalIgnoreCase);
}
