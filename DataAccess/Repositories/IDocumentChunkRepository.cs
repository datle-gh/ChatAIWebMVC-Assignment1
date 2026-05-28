using BusinessObject.Entities;

namespace DataAccess.Repositories;

public interface IDocumentChunkRepository
{
    Task AddRangeAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentChunk>> GetIndexedChunksBySubjectAsync(
        int subjectId,
        string? embeddingModel = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentChunk>> GetIndexedChunksBySubjectForBackfillAsync(
        int subjectId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentChunk>> GetIndexedChunksByIdsAsync(
        IEnumerable<int> chunkIds,
        CancellationToken cancellationToken = default);
}
