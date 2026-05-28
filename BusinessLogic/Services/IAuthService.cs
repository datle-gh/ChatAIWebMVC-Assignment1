using BusinessLogic.DTOs.Requests;
using BusinessLogic.DTOs.Responses;

namespace BusinessLogic.Services;

public interface IAuthService
{
    Task<AuthResultDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
}
