namespace BusinessLogic.DTOs.Responses;

public sealed record AuthUserDto(
    int Id,
    string FullName,
    string Email,
    string Role);
