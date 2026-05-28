namespace BusinessLogic.DTOs.Requests;

public sealed record DocumentBatchUploadRequest(
    string UploadId,
    IReadOnlyList<DocumentBatchUploadFileRequest> Files,
    int SubjectId,
    int? UploadedBy,
    string? UploaderRole,
    string? Title);
