namespace BusinessLogic.DTOs.Requests;

public sealed record LoginRequestDto(
    string Email,
    string Password);
