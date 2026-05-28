using BusinessObject.Entities;
using BusinessObject.Enums;

namespace DataAccess.Repositories;

public interface IDocumentRepository
{
    Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default);

    Task<Document?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Document>> GetListAsync(
        string? searchTerm = null,
        int? subjectId = null,
        DocumentStatus? status = null,
        CancellationToken cancellationToken = default);

    Task UpdateStatusAsync(
        int id,
        DocumentStatus status,
        string? errorMessage = null,
        DateTime? indexedAt = null,
        CancellationToken cancellationToken = default);
}
