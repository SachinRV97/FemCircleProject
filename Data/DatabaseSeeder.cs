using FemCircleProject.Data.Entities;
using FemCircleProject.ViewModels.Product;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FemCircleProject.Data;

public sealed class DatabaseSeeder
{
    private readonly FemCircleDbContext _dbContext;
    private readonly IPasswordHasher<AppUser> _passwordHasher;

    public DatabaseSeeder(FemCircleDbContext dbContext, IPasswordHasher<AppUser> passwordHasher)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.EnsureCreatedAsync(cancellationToken);

        if (!await _dbContext.Users.AnyAsync(cancellationToken))
        {
            await SeedUsersAsync(cancellationToken);
        }

        if (!await _dbContext.Products.AnyAsync(cancellationToken))
        {
            await SeedProductsAsync(cancellationToken);
        }
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        AppUser admin = new()
        {
            FullName = "FemCircle Admin",
            UserName = "admin",
            Email = "admin@femcircle.local",
            PhoneNumber = null,
            City = "Mumbai",
            IsVerified = true,
            IsBlocked = false,
            IsAdmin = true,
            RegisteredOnUtc = DateTime.UtcNow.AddMonths(-2)
        };
        admin.PasswordHash = _passwordHasher.HashPassword(admin, "Admin@123");

        AppUser userA = new()
        {
            FullName = "Priya Sharma",
            UserName = "priya",
            Email = "priya@femcircle.local",
            PhoneNumber = "9999999999",
            City = "Pune",
            IsVerified = true,
            IsBlocked = false,
            IsAdmin = false,
            RegisteredOnUtc = DateTime.UtcNow.AddDays(-20)
        };
        userA.PasswordHash = _passwordHasher.HashPassword(userA, "Member@123");

        AppUser userB = new()
        {
            FullName = "Ananya Rao",
            UserName = "ananya",
            Email = "ananya@femcircle.local",
            PhoneNumber = "8888888888",
            City = "Hyderabad",
            IsVerified = true,
            IsBlocked = false,
            IsAdmin = false,
            RegisteredOnUtc = DateTime.UtcNow.AddDays(-8)
        };
        userB.PasswordHash = _passwordHasher.HashPassword(userB, "Member@123");

        _dbContext.Users.AddRange(admin, userA, userB);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedProductsAsync(CancellationToken cancellationToken)
    {
        Dictionary<string, int> userIds = await _dbContext.Users
            .AsNoTracking()
            .ToDictionaryAsync(x => x.UserName, x => x.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        if (!userIds.TryGetValue("priya", out int priyaId) || !userIds.TryGetValue("ananya", out int ananyaId))
        {
            return;
        }

        List<Product> products = new()
        {
            new Product
            {
                Title = "Handcrafted Cotton Kurti",
                Description = "Lightly used festive kurti in excellent condition.",
                Category = "Clothing",
                ListingType = ProductListingType.Sell,
                Price = 650,
                ItemCondition = "Like New",
                Quantity = 1,
                City = "Pune",
                ImageUrl = "https://images.unsplash.com/photo-1610030469678-8f49c9b0839d?auto=format&fit=crop&w=900&q=80",
                SellerId = priyaId,
                IsApproved = true,
                CreatedOnUtc = DateTime.UtcNow.AddDays(-3)
            },
            new Product
            {
                Title = "Study Desk Organizer Set",
                Description = "Wooden organizer set, ideal for home office and study corner.",
                Category = "Home Decor",
                ListingType = ProductListingType.Exchange,
                Price = null,
                ItemCondition = "Good",
                Quantity = 1,
                City = "Hyderabad",
                ImageUrl = null,
                SellerId = ananyaId,
                IsApproved = true,
                CreatedOnUtc = DateTime.UtcNow.AddDays(-2)
            },
            new Product
            {
                Title = "Books for Donation - Career Prep",
                Description = "Set of exam prep books available for donation.",
                Category = "Books",
                ListingType = ProductListingType.Donate,
                Price = 0,
                ItemCondition = "Good",
                Quantity = 4,
                City = "Mumbai",
                ImageUrl = null,
                SellerId = priyaId,
                IsApproved = false,
                CreatedOnUtc = DateTime.UtcNow.AddDays(-1)
            }
        };

        _dbContext.Products.AddRange(products);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
