using BusinessLogic.DTOs.Requests;
using BusinessLogic.DTOs.Responses;

namespace BusinessLogic.Services;

public interface IAccountService
{
    Task<AccountProfileDto?> GetProfileAsync(int userId, CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateProfileAsync(
        UpdateProfileRequestDto request,
        CancellationToken cancellationToken = default);
}
