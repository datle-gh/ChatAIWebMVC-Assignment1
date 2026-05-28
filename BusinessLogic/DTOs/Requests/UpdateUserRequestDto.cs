namespace BusinessLogic.DTOs.Requests;

public sealed record UpdateUserRequestDto(
    int UserId,
    int CurrentAdminUserId,
    string FullName,
    string Email,
    string Role,
    bool IsActive);
