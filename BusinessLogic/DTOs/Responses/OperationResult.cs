namespace BusinessLogic.DTOs.Responses;

public sealed record OperationResult(
    bool Succeeded,
    string Message);
