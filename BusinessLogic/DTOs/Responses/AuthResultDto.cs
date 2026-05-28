namespace BusinessLogic.DTOs.Responses;

public sealed record AuthResultDto(
    bool Succeeded,
    AuthUserDto? User,
    string Message);
