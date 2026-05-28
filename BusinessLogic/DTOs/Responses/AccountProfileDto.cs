namespace BusinessLogic.DTOs.Responses;

public sealed record AccountProfileDto(
    int Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
