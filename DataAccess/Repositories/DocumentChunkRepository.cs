using BusinessObject.Entities;
using BusinessObject.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public sealed class DocumentChunkRepository : IDocumentChunkRepository
{
    private readonly ChatAIWebDbContext _context;

    public DocumentChunkRepository(ChatAIWebDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        _context.DocumentChunks.AddRange(chunks);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentChunk>> GetIndexedChunksBySubjectAsync(
        int subjectId,
        string? embeddingModel = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DocumentChunks
            .AsNoTracking()
            .Include(chunk => chunk.Document)
            .Where(chunk =>
                chunk.Document.SubjectId == subjectId
                && chunk.Document.Status == DocumentStatus.Indexed
                && chunk.EmbeddingJson != null);

        if (!string.IsNullOrWhiteSpace(embeddingModel))
        {
            query = query.Where(chunk => chunk.EmbeddingModel == embeddingModel);
        }

        return await query
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentChunk>> GetIndexedChunksBySubjectForBackfillAsync(
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        return await _context.DocumentChunks
            .AsNoTracking()
            .Include(chunk => chunk.Document)
            .Where(chunk =>
                chunk.Document.SubjectId == subjectId
                && chunk.Document.Status == DocumentStatus.Indexed)
            .OrderBy(chunk => chunk.DocumentId)
            .ThenBy(chunk => chunk.ChunkIndex)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentChunk>> GetIndexedChunksByIdsAsync(
        IEnumerable<int> chunkIds,
        CancellationToken cancellationToken = default)
    {
        var ids = chunkIds.ToHashSet();
        if (ids.Count == 0)
        {
            return [];
        }

        return await _context.DocumentChunks
            .AsNoTracking()
            .Include(chunk => chunk.Document)
            .Where(chunk =>
                ids.Contains(chunk.Id)
                && chunk.Document.Status == DocumentStatus.Indexed)
            .ToListAsync(cancellationToken);
    }
}
