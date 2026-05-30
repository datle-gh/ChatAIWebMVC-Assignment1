using BusinessLogic.DTOs.Requests;
using BusinessLogic.DTOs.Responses;

namespace BusinessLogic.Services;

public interface IDocumentService
{
    Task<DocumentUploadResult> UploadAndIndexAsync(
        DocumentUploadRequest request,
        CancellationToken cancellationToken = default);

    Task<DocumentBatchUploadResult> UploadBatchAndIndexAsync(
        DocumentBatchUploadRequest request,
        CancellationToken cancellationToken = default);

    Task<DocumentListResultDto> GetDocumentsAsync(
        DocumentListRequestDto request,
        CancellationToken cancellationToken = default);

    Task<DocumentDetailDto?> GetDocumentDetailAsync(
        int documentId,
        CancellationToken cancellationToken = default);

    Task<DocumentUploadResult> VerifyAndIndexAsync(
        int documentId,
        int verifiedBy,
        string? verifierRole,
        CancellationToken cancellationToken = default);

    Task<DocumentUploadResult> RejectAsync(
        int documentId,
        int rejectedBy,
        string? rejecterRole,
        string? reason,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubjectOptionDto>> GetSubjectOptionsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubjectOptionDto>> GetUploadSubjectOptionsAsync(
        int userId,
        string? userRole,
        CancellationToken cancellationToken = default);
}
