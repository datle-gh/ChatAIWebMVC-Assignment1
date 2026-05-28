namespace BusinessLogic.DTOs.Requests;

public sealed record CreateUserRequestDto(
    string FullName,
    string Email,
    string Role,
    string Password,
    bool IsActive);
