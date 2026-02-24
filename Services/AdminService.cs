using FemCircleProject.Data;
using FemCircleProject.Data.Entities;
using FemCircleProject.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
        int activeListings = await _dbContext.Products.CountAsync(p => p.IsApproved && !p.IsSold && p.BoughtByUserId == null, cancellationToken);
        int pendingModerationCount = await _dbContext.Products.CountAsync(p => !p.IsApproved, cancellationToken);
        int completedOrders = await _dbContext.Products.CountAsync(p => p.IsSold, cancellationToken);

        List<AdminProductSummaryViewModel> recentListings = await _dbContext.Products
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedOnUtc)
            .Take(10)
            .Select(ToAdminProductSummary())
            .ToListAsync(cancellationToken);

        List<AdminUserSummaryViewModel> newlyRegisteredUsers = await _dbContext.Users
            .AsNoTracking()
            .OrderByDescending(u => u.RegisteredOnUtc)
            .Take(10)
            .Select(ToAdminUserSummary())
            .ToListAsync(cancellationToken);

        return new AdminDashboardViewModel
        {
            TotalUsers = totalUsers,
            ActiveListings = activeListings,
            PendingModerationCount = pendingModerationCount,
            CompletedOrders = completedOrders,
            RecentListings = recentListings,
            NewlyRegisteredUsers = newlyRegisteredUsers
        };
    }

    public async Task<IReadOnlyList<AdminUserSummaryViewModel>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .OrderByDescending(u => u.RegisteredOnUtc)
            .Select(ToAdminUserSummary())
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminProductSummaryViewModel>> GetPendingProductsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(p => !p.IsApproved)
            .OrderByDescending(p => p.CreatedOnUtc)
            .Select(ToAdminProductSummary())
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ApproveListingAsync(int productId, CancellationToken cancellationToken = default)
    {
        Product? product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
        {
            return false;
        }

        if (!product.IsApproved)
        {
            product.IsApproved = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<bool> RejectListingAsync(int productId, CancellationToken cancellationToken = default)
    {
        Product? product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product is null)
        {
            return false;
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ToggleUserVerificationAsync(int userId, CancellationToken cancellationToken = default)
    {
        AppUser? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.IsVerified = !user.IsVerified;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ToggleUserBlockedAsync(int userId, int actingAdminUserId, CancellationToken cancellationToken = default)
    {
        AppUser? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        // Prevent an admin from blocking herself.
        if (user.Id == actingAdminUserId)
        {
            return false;
        }

        user.IsBlocked = !user.IsBlocked;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static Expression<Func<AppUser, AdminUserSummaryViewModel>> ToAdminUserSummary()
    {
        return u => new AdminUserSummaryViewModel
        {
            Id = u.Id,
            FullName = u.FullName,
            UserName = u.UserName,
            Email = u.Email,
            IsVerified = u.IsVerified,
            IsBlocked = u.IsBlocked,
            RegisteredOnUtc = u.RegisteredOnUtc
        };
    }

    private static Expression<Func<Product, AdminProductSummaryViewModel>> ToAdminProductSummary()
    {
        return p => new AdminProductSummaryViewModel
        {
            Id = p.Id,
            Title = p.Title,
            Category = p.Category,
            ListingType = p.ListingType,
            SellerUserName = p.Seller.UserName,
            IsApproved = p.IsApproved,
            CreatedOnUtc = p.CreatedOnUtc
        };
    }
}
