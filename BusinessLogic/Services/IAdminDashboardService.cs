using BusinessLogic.DTOs.Responses;

namespace BusinessLogic.Services;

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
