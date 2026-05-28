using System.Text.Json;
using BusinessObject.Entities;
using DataAccess.Repositories;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services;

public sealed class EmbeddingBackfillService : IEmbeddingBackfillService
{
    private readonly IDocumentChunkRepository _chunkRepository;
    private readonly IDocumentChunkEmbeddingRepository _embeddingRepository;
    private readonly IEmbeddingModelRegistry _embeddingModelRegistry;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly ILogger<EmbeddingBackfillService> _logger;

    public EmbeddingBackfillService(
        IDocumentChunkRepository chunkRepository,
        IDocumentChunkEmbeddingRepository embeddingRepository,
        IEmbeddingModelRegistry embeddingModelRegistry,
        IVectorStoreService vectorStoreService,
        ILogger<EmbeddingBackfillService> logger)
    {
        _chunkRepository = chunkRepository;
        _embeddingRepository = embeddingRepository;
        _embeddingModelRegistry = embeddingModelRegistry;
        _vectorStoreService = vectorStoreService;
        _logger = logger;
    }

    public async Task<int> BackfillSubjectAsync(
        int subjectId,
        string embeddingModel,
        CancellationToken cancellationToken = default)
    {
        var embeddingService = _embeddingModelRegistry.GetRequired(embeddingModel);
        var existingChunkIds = await _embeddingRepository.GetExistingChunkIdsAsync(
            subjectId,
            embeddingService.ModelKey,
            cancellationToken);

        var chunks = (await _chunkRepository.GetIndexedChunksBySubjectForBackfillAsync(
                subjectId,
                cancellationToken))
            .Where(chunk => !existingChunkIds.Contains(chunk.Id))
            .ToList();

        if (chunks.Count == 0)
        {
            return 0;
        }

        var rows = new List<DocumentChunkEmbedding>();
        var embeddedChunks = new List<DocumentChunk>();
        var vectors = new List<float[]>();

        foreach (var chunk in chunks)
        {
            try
            {
                var vector = await embeddingService.GenerateEmbeddingAsync(chunk.Content, cancellationToken);
                rows.Add(new DocumentChunkEmbedding
                {
                    DocumentChunkId = chunk.Id,
                    EmbeddingModel = embeddingService.ModelKey,
                    EmbeddingProvider = embeddingService.ProviderName,
                    Dimension = vector.Length,
                    VectorId = $"chunk-{chunk.Id}-{embeddingService.ModelKey}",
                    VectorStore = _vectorStoreService.Name,
                    EmbeddingJson = JsonSerializer.Serialize(vector),
                    CreatedAt = DateTime.UtcNow
                });
                embeddedChunks.Add(chunk);
                vectors.Add(vector);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Unable to backfill embedding for chunk {ChunkId} using model {EmbeddingModel}",
                    chunk.Id,
                    embeddingService.ModelKey);
            }
        }

        await _embeddingRepository.AddRangeAsync(rows, cancellationToken);
        await _vectorStoreService.UpsertAsync(
            embeddingService.ModelKey,
            embeddingService.ProviderName,
            embeddedChunks,
            vectors,
            cancellationToken);

        return rows.Count;
    }
}
