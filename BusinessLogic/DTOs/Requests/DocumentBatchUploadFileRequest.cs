namespace BusinessLogic.DTOs.Requests;

public sealed record DocumentBatchUploadFileRequest(
    Stream FileStream,
    string FileName,
    string? ContentType,
    long FileSizeBytes);
