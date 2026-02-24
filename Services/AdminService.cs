using FemCircleProject.ViewModels.Admin;

namespace FemCircleProject.Services;

public sealed class AdminService : IAdminService
{
    private readonly InMemoryAppStore _store;

    public AdminService(InMemoryAppStore store)
    {
        _store = store;
    }

    public Task<AdminDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<UserRecord> users = _store.GetUsersSnapshot();
        IReadOnlyList<ProductRecord> products = _store.GetProductsSnapshot();

        AdminDashboardViewModel model = new()
        {
            TotalUsers = users.Count,
            ActiveListings = products.Count(p => p.IsApproved),
            PendingModerationCount = products.Count(p => !p.IsApproved),
            CompletedOrders = 0,
            RecentListings = products
                .OrderByDescending(p => p.CreatedOnUtc)
                .Take(6)
                .Select(ToAdminProductSummary)
                .ToList(),
            NewlyRegisteredUsers = users
                .OrderByDescending(u => u.RegisteredOnUtc)
                .Take(6)
                .Select(ToAdminUserSummary)
                .ToList()
        };

        return Task.FromResult(model);
    }

    public Task<IReadOnlyList<AdminUserSummaryViewModel>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AdminUserSummaryViewModel> users = _store.GetUsersSnapshot()
            .OrderByDescending(u => u.RegisteredOnUtc)
            .Select(ToAdminUserSummary)
            .ToList();

        return Task.FromResult(users);
    }

    public Task<IReadOnlyList<AdminProductSummaryViewModel>> GetPendingProductsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AdminProductSummaryViewModel> pending = _store.GetProductsSnapshot()
            .Where(p => !p.IsApproved)
            .OrderByDescending(p => p.CreatedOnUtc)
            .Select(ToAdminProductSummary)
            .ToList();

        return Task.FromResult(pending);
    }

    private static AdminUserSummaryViewModel ToAdminUserSummary(UserRecord user)
    {
        return new AdminUserSummaryViewModel
        {
            Id = user.Id.ToString(),
            FullName = user.FullName,
            UserName = user.UserName,
            Email = user.Email,
            IsVerified = user.IsVerified,
            IsBlocked = user.IsBlocked,
            RegisteredOnUtc = user.RegisteredOnUtc
        };
    }

    private static AdminProductSummaryViewModel ToAdminProductSummary(ProductRecord product)
    {
        return new AdminProductSummaryViewModel
        {
            Id = product.Id,
            Title = product.Title,
            Category = product.Category,
            ListingType = product.ListingType,
            SellerUserName = product.SellerUserName,
            IsApproved = product.IsApproved,
            CreatedOnUtc = product.CreatedOnUtc
        };
    }
}
