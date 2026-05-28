namespace BusinessLogic.DTOs.Requests;

public sealed record ResetPasswordRequestDto(
    int UserId,
    string NewPassword);
