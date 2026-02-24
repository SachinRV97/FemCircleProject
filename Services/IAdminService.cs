using FemCircleProject.ViewModels.Admin;

namespace FemCircleProject.Services;

public interface IAdminService
{
    Task<AdminDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminUserSummaryViewModel>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdminProductSummaryViewModel>> GetPendingProductsAsync(CancellationToken cancellationToken = default);
    Task<bool> ApproveListingAsync(int productId, CancellationToken cancellationToken = default);
    Task<bool> RejectListingAsync(int productId, CancellationToken cancellationToken = default);
    Task<bool> ToggleUserVerificationAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> ToggleUserBlockedAsync(int userId, int actingAdminUserId, CancellationToken cancellationToken = default);
}
