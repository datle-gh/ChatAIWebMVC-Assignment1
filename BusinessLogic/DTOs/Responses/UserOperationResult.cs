namespace BusinessLogic.DTOs.Responses;

public sealed record UserOperationResult(
    bool Succeeded,
    string Message,
    int? UserId = null);
