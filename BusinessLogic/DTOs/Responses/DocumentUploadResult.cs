namespace BusinessLogic.DTOs.Responses;

public sealed record DocumentUploadResult(
    bool Succeeded,
    int? DocumentId,
    string Message);
