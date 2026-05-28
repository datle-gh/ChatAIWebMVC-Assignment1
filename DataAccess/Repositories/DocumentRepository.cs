using BusinessObject.Entities;
using BusinessObject.Enums;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public sealed class DocumentRepository : IDocumentRepository
{
    private readonly ChatAIWebDbContext _context;

    public DocumentRepository(ChatAIWebDbContext context)
    {
        _context = context;
    }

    public async Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        _context.Documents.Add(document);
        await _context.SaveChangesAsync(cancellationToken);
        return document;
    }

    public Task<Document?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Documents
            .AsNoTracking()
            .Include(document => document.Subject)
            .Include(document => document.UploadedByNavigation)
            .Include(document => document.DocumentChunks)
            .FirstOrDefaultAsync(document => document.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetListAsync(
        string? searchTerm = null,
        int? subjectId = null,
        DocumentStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Documents
            .AsNoTracking()
            .Include(document => document.Subject)
            .Include(document => document.UploadedByNavigation)
            .Include(document => document.DocumentChunks)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearchTerm = searchTerm.Trim().ToLower();
            query = query.Where(document =>
                document.Title.ToLower().Contains(normalizedSearchTerm)
                || document.OriginalFileName.ToLower().Contains(normalizedSearchTerm));
        }

        if (subjectId.HasValue && subjectId.Value > 0)
        {
            query = query.Where(document => document.SubjectId == subjectId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(document => document.Status == status.Value);
        }

        return await query
            .OrderByDescending(document => document.UploadedAt)
            .ThenBy(document => document.Title)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateStatusAsync(
        int id,
        DocumentStatus status,
        string? errorMessage = null,
        DateTime? indexedAt = null,
        CancellationToken cancellationToken = default)
    {
        var document = await _context.Documents.FirstOrDefaultAsync(
            item => item.Id == id,
            cancellationToken);

        if (document is null)
        {
            return;
        }

        document.Status = status;
        document.ErrorMessage = errorMessage;
        document.IndexedAt = indexedAt;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
