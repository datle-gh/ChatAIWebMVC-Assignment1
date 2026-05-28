using BusinessObject.Entities;

namespace DataAccess.Repositories;

public interface IDocumentChunkEmbeddingRepository
{
    Task AddRangeAsync(IEnumerable<DocumentChunkEmbedding> embeddings, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentChunkEmbedding>> GetBySubjectAsync(
        int subjectId,
        string embeddingModel,
        CancellationToken cancellationToken = default);

    Task<HashSet<int>> GetExistingChunkIdsAsync(
        int subjectId,
        string embeddingModel,
        CancellationToken cancellationToken = default);
}
