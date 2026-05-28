using BusinessLogic.DTOs.Requests;
using BusinessLogic.DTOs.Responses;

namespace BusinessLogic.Services;

public interface IUserManagementService
{
    Task<IReadOnlyList<UserManagementDto>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<UserManagementDto?> GetUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<UserOperationResult> CreateUserAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateUserAsync(UpdateUserRequestDto request, CancellationToken cancellationToken = default);

    Task<OperationResult> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);
}
