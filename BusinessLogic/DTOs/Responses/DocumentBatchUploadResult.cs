namespace BusinessLogic.DTOs.Responses;

public sealed record DocumentBatchUploadResult(
    bool Succeeded,
    string Message,
    IReadOnlyList<DocumentUploadItemResult> Items);
