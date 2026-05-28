namespace BusinessLogic.DTOs.Responses;

public sealed record UploadProgressDto(
    string UploadId,
    int UserId,
    string FileName,
    int FileIndex,
    int TotalFiles,
    string Stage,
    int Percent,
    string Message,
    bool IsCompleted = false,
    bool IsFailed = false);
