namespace BusinessLogic.DTOs.Requests;

public sealed record UpdateProfileRequestDto(
    int UserId,
    string FullName,
    string Email,
    string? CurrentPassword,
    string? NewPassword);
