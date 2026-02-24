using FemCircleProject.ViewModels.Admin;

namespace FemCircleProject.Services;

public interface IAdminService
{
    Task<AdminDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminUserSummaryViewModel>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminProductSummaryViewModel>> GetPendingProductsAsync(CancellationToken cancellationToken = default);
}
