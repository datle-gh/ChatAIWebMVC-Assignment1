namespace BusinessLogic.DTOs.Responses;

public sealed record UserManagementDto(
    int Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
