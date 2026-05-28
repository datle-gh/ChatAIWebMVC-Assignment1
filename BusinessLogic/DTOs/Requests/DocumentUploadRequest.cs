namespace BusinessLogic.DTOs.Requests;

public sealed record DocumentUploadRequest(
    Stream FileStream,
    string FileName,
    string? ContentType,
    long FileSizeBytes,
    int SubjectId,
    int? UploadedBy,
    string? UploaderRole,
    string? Title);
