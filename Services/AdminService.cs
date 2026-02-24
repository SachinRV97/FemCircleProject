using FemCircleProject.Data;
using FemCircleProject.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace FemCircleProject.Services;

public sealed class AdminService : IAdminService
{
    private readonly FemCircleDbContext _dbContext;

    public AdminService(FemCircleDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        int totalUsers = await _dbContext.Users.CountAsync(cancellationToken);
        int activeListings = await _dbContext.Products.CountAsync(p => p.IsApproved, cancellationToken);
        int pendingModerationCount = await _dbContext.Products.CountAsync(p => !p.IsApproved, cancellationToken);

        List<AdminProductSummaryViewModel> recentListings = await _dbContext.Products
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedOnUtc)
            .Take(6)
            .Select(p => new AdminProductSummaryViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Category = p.Category,
                ListingType = p.ListingType,
                SellerUserName = p.Seller.UserName,
                IsApproved = p.IsApproved,
                CreatedOnUtc = p.CreatedOnUtc
            })
            .ToListAsync(cancellationToken);

        List<AdminUserSummaryViewModel> newlyRegisteredUsers = await _dbContext.Users
            .AsNoTracking()
            .OrderByDescending(u => u.RegisteredOnUtc)
            .Take(6)
            .Select(u => new AdminUserSummaryViewModel
            {
                Id = u.Id.ToString(),
                FullName = u.FullName,
                UserName = u.UserName,
                Email = u.Email,
                IsVerified = u.IsVerified,
                IsBlocked = u.IsBlocked,
                RegisteredOnUtc = u.RegisteredOnUtc
            })
            .ToListAsync(cancellationToken);

        return new AdminDashboardViewModel
        {
            TotalUsers = totalUsers,
            ActiveListings = activeListings,
            PendingModerationCount = pendingModerationCount,
            CompletedOrders = 0,
            RecentListings = recentListings,
            NewlyRegisteredUsers = newlyRegisteredUsers
        };
    }

    public async Task<IReadOnlyList<AdminUserSummaryViewModel>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .OrderByDescending(u => u.RegisteredOnUtc)
            .Select(u => new AdminUserSummaryViewModel
            {
                Id = u.Id.ToString(),
                FullName = u.FullName,
                UserName = u.UserName,
                Email = u.Email,
                IsVerified = u.IsVerified,
                IsBlocked = u.IsBlocked,
                RegisteredOnUtc = u.RegisteredOnUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminProductSummaryViewModel>> GetPendingProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(p => !p.IsApproved)
            .OrderByDescending(p => p.CreatedOnUtc)
            .Select(p => new AdminProductSummaryViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Category = p.Category,
                ListingType = p.ListingType,
                SellerUserName = p.Seller.UserName,
                IsApproved = p.IsApproved,
                CreatedOnUtc = p.CreatedOnUtc
            })
            .ToListAsync(cancellationToken);
    }
}
