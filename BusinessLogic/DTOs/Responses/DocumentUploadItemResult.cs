namespace BusinessLogic.DTOs.Responses;

public sealed record DocumentUploadItemResult(
    bool Succeeded,
    int? DocumentId,
    string FileName,
    string Message);
