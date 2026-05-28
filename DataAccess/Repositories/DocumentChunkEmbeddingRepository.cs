using BusinessObject.Entities;
using BusinessObject.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public sealed class DocumentChunkEmbeddingRepository : IDocumentChunkEmbeddingRepository
{
    private readonly ChatAIWebDbContext _context;

    public DocumentChunkEmbeddingRepository(ChatAIWebDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(
        IEnumerable<DocumentChunkEmbedding> embeddings,
        CancellationToken cancellationToken = default)
    {
        var items = embeddings.ToList();
        if (items.Count == 0)
        {
            return;
        }

        _context.DocumentChunkEmbeddings.AddRange(items);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentChunkEmbedding>> GetBySubjectAsync(
        int subjectId,
        string embeddingModel,
        CancellationToken cancellationToken = default)
    {
        return await _context.DocumentChunkEmbeddings
            .AsNoTracking()
            .Include(embedding => embedding.DocumentChunk)
            .ThenInclude(chunk => chunk.Document)
            .Where(embedding =>
                embedding.EmbeddingModel == embeddingModel
                && embedding.EmbeddingJson != null
                && embedding.DocumentChunk.Document.SubjectId == subjectId
                && embedding.DocumentChunk.Document.Status == DocumentStatus.Indexed)
            .ToListAsync(cancellationToken);
    }

    public async Task<HashSet<int>> GetExistingChunkIdsAsync(
        int subjectId,
        string embeddingModel,
        CancellationToken cancellationToken = default)
    {
        var ids = await _context.DocumentChunkEmbeddings
            .AsNoTracking()
            .Where(embedding =>
                embedding.EmbeddingModel == embeddingModel
                && embedding.DocumentChunk.Document.SubjectId == subjectId
                && embedding.DocumentChunk.Document.Status == DocumentStatus.Indexed)
            .Select(embedding => embedding.DocumentChunkId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }
}
