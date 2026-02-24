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
        await EnsureProductSchemaCompatibilityAsync(cancellationToken);
        await SeedUsersAsync(cancellationToken);
        await SeedProductsAsync(cancellationToken);
    }

    private async Task EnsureProductSchemaCompatibilityAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            IF COL_LENGTH('Products', 'IsSold') IS NULL
            BEGIN
                ALTER TABLE [Products]
                ADD [IsSold] bit NOT NULL CONSTRAINT [DF_Products_IsSold] DEFAULT(0);
            END;

            IF COL_LENGTH('Products', 'SoldOnUtc') IS NULL
            BEGIN
                ALTER TABLE [Products]
                ADD [SoldOnUtc] datetime2 NULL;
            END;

            IF COL_LENGTH('Products', 'BoughtByUserId') IS NULL
            BEGIN
                ALTER TABLE [Products]
                ADD [BoughtByUserId] int NULL;
            END;

            IF NOT EXISTS
            (
                SELECT 1
                FROM sys.foreign_keys fk
                INNER JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
                WHERE fk.parent_object_id = OBJECT_ID('Products')
                  AND COL_NAME(fkc.parent_object_id, fkc.parent_column_id) = 'BoughtByUserId'
            )
            BEGIN
                ALTER TABLE [Products] WITH CHECK
                ADD CONSTRAINT [FK_Products_Users_BoughtByUserId]
                FOREIGN KEY ([BoughtByUserId]) REFERENCES [Users]([Id]);
            END;

            IF NOT EXISTS
            (
                SELECT 1
                FROM sys.indexes
                WHERE [name] = 'IX_Products_BoughtByUserId'
                  AND [object_id] = OBJECT_ID('Products')
            )
            BEGIN
                CREATE INDEX [IX_Products_BoughtByUserId] ON [Products]([BoughtByUserId]);
            END;
            """;

        await _dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        List<UserSeed> userSeeds =
        [
            new("FemCircle Admin", "admin", "admin@femcircle.local", "Admin@123", null, "Mumbai", true, false, true, 90),
            new("Priya Sharma", "priya", "priya@femcircle.local", "Member@123", "9999999999", "Pune", true, false, false, 30),
            new("Ananya Rao", "ananya", "ananya@femcircle.local", "Member@123", "8888888888", "Hyderabad", true, false, false, 24),
            new("Kavya Nair", "kavya", "kavya@femcircle.local", "Member@123", "7777777777", "Bengaluru", true, false, false, 20),
            new("Meera Patel", "meera", "meera@femcircle.local", "Member@123", "6666666666", "Ahmedabad", true, false, false, 17),
            new("Nisha Verma", "nisha", "nisha@femcircle.local", "Member@123", "5555555555", "Delhi", true, false, false, 14),
            new("Pooja Kulkarni", "pooja", "pooja@femcircle.local", "Member@123", "4444444444", "Pune", true, false, false, 12),
            new("Riddhi Jain", "riddhi", "riddhi@femcircle.local", "Member@123", "3333333333", "Jaipur", true, false, false, 10),
            new("Sakshi Singh", "sakshi", "sakshi@femcircle.local", "Member@123", "2222222222", "Lucknow", false, false, false, 8),
            new("Tanvi Joshi", "tanvi", "tanvi@femcircle.local", "Member@123", "1111111111", "Mumbai", true, false, false, 6),
            new("Vaishali Das", "vaishali", "vaishali@femcircle.local", "Member@123", "9123456780", "Kolkata", true, false, false, 4),
            new("Yesha Shah", "yesha", "yesha@femcircle.local", "Member@123", "9234567890", "Indore", true, false, false, 2)
        ];

        List<string> existingUserNamesList = await _dbContext.Users
            .AsNoTracking()
            .Select(u => u.UserName)
            .ToListAsync(cancellationToken);

        List<string> existingEmailsList = await _dbContext.Users
            .AsNoTracking()
            .Select(u => u.Email)
            .ToListAsync(cancellationToken);

        HashSet<string> existingUserNames = new(existingUserNamesList, StringComparer.OrdinalIgnoreCase);
        HashSet<string> existingEmails = new(existingEmailsList, StringComparer.OrdinalIgnoreCase);

        List<AppUser> usersToAdd = [];

        foreach (UserSeed seed in userSeeds)
        {
            if (existingUserNames.Contains(seed.UserName) || existingEmails.Contains(seed.Email))
            {
                continue;
            }

            AppUser user = new()
            {
                FullName = seed.FullName,
                UserName = seed.UserName,
                Email = seed.Email,
                PhoneNumber = seed.PhoneNumber,
                City = seed.City,
                IsVerified = seed.IsVerified,
                IsBlocked = seed.IsBlocked,
                IsAdmin = seed.IsAdmin,
                RegisteredOnUtc = now.AddDays(-seed.RegisteredDaysAgo)
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, seed.Password);

            usersToAdd.Add(user);
            existingUserNames.Add(seed.UserName);
            existingEmails.Add(seed.Email);
        }

        if (usersToAdd.Count > 0)
        {
            _dbContext.Users.AddRange(usersToAdd);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedProductsAsync(CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;

        List<AppUser> users = await _dbContext.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            return;
        }

        Dictionary<string, AppUser> usersByUserName = users
            .ToDictionary(u => u.UserName, StringComparer.OrdinalIgnoreCase);

        List<string> existingTitlesList = await _dbContext.Products
            .AsNoTracking()
            .Select(p => p.Title)
            .ToListAsync(cancellationToken);

        HashSet<string> existingTitles = new(existingTitlesList, StringComparer.OrdinalIgnoreCase);

        List<ProductSeed> productSeeds =
        [
            new("Handcrafted Cotton Kurti", "Lightly used festive kurti in excellent condition.", "Clothing", ProductListingType.Sell, 650, "Like New", 1, "Pune", "https://images.unsplash.com/photo-1610030469678-8f49c9b0839d?auto=format&fit=crop&w=900&q=80", "priya", true, 12),
            new("Study Desk Organizer Set", "Wooden organizer set for study corner and home office.", "Home Decor", ProductListingType.Exchange, null, "Good", 1, "Hyderabad", null, "ananya", true, 11),
            new("Books for Donation Career Prep", "Competitive exam books available for donation.", "Books", ProductListingType.Donate, 0, "Good", 4, "Mumbai", null, "priya", false, 10),
            new("Canvas Tote Bag Floral", "Spacious reusable tote bag with floral print.", "Accessories", ProductListingType.Sell, 280, "Like New", 2, "Delhi", "https://images.unsplash.com/photo-1591561954557-26941169b49e?auto=format&fit=crop&w=900&q=80", "nisha", true, 9),
            new("Yoga Mat Purple", "Anti-slip yoga mat used for a month.", "Sports", ProductListingType.Sell, 900, "Good", 1, "Bengaluru", "https://images.unsplash.com/photo-1599447421382-03d3dc6ca98d?auto=format&fit=crop&w=900&q=80", "kavya", true, 8),
            new("Laptop Backpack", "Water resistant backpack for laptop and daily commute.", "Accessories", ProductListingType.Exchange, null, "Good", 1, "Pune", null, "pooja", true, 8),
            new("Silk Saree Maroon", "Wedding wear silk saree worn once.", "Clothing", ProductListingType.Sell, 2400, "Like New", 1, "Ahmedabad", "https://images.unsplash.com/photo-1583391733956-6c78276477e2?auto=format&fit=crop&w=900&q=80", "meera", true, 7),
            new("Skincare Combo Unused", "Face wash and moisturizer combo unopened.", "Beauty", ProductListingType.Sell, 520, "New", 1, "Jaipur", null, "riddhi", true, 7),
            new("Kitchen Storage Jars Set", "Glass jar set for pantry organization.", "Kitchen", ProductListingType.Sell, 740, "Like New", 1, "Kolkata", null, "vaishali", true, 6),
            new("Kids Story Books Bundle", "Illustrated story books for age 6 to 10.", "Books", ProductListingType.Donate, 0, "Good", 8, "Lucknow", null, "sakshi", false, 6),
            new("Bluetooth Speaker Compact", "Portable speaker with clear sound and bass.", "Electronics", ProductListingType.Sell, 1500, "Good", 1, "Indore", null, "yesha", true, 5),
            new("Wooden Coffee Table", "Compact coffee table for living room.", "Home Decor", ProductListingType.Exchange, null, "Good", 1, "Mumbai", null, "tanvi", true, 5),
            new("Running Shoes Women Size 6", "Comfortable running shoes lightly used.", "Footwear", ProductListingType.Sell, 1200, "Good", 1, "Bengaluru", null, "kavya", true, 4),
            new("Office Chair Ergonomic", "Mesh back office chair in working condition.", "Furniture", ProductListingType.Sell, 3100, "Good", 1, "Delhi", null, "nisha", true, 4),
            new("Instant Pot Multi Cooker", "Seven in one cooker with accessories.", "Kitchen", ProductListingType.Sell, 2800, "Like New", 1, "Hyderabad", null, "ananya", true, 3),
            new("Traditional Jewelry Set", "Imitation jewelry set for festive wear.", "Accessories", ProductListingType.Sell, 650, "Like New", 1, "Jaipur", null, "riddhi", true, 3),
            new("Plant Pots Ceramic Set", "Set of 5 painted ceramic pots.", "Home Decor", ProductListingType.Sell, 430, "New", 1, "Pune", null, "priya", true, 2),
            new("Coding Interview Notes", "Handwritten and printed notes for donation.", "Books", ProductListingType.Donate, 0, "Good", 1, "Kolkata", null, "vaishali", false, 2),
            new("Makeup Vanity Mirror", "Tabletop mirror with light frame.", "Beauty", ProductListingType.Exchange, null, "Good", 1, "Indore", null, "yesha", true, 1),
            new("Dinner Set Ceramic 24 Piece", "Complete dinner set, no chips or cracks.", "Kitchen", ProductListingType.Sell, 1900, "Like New", 1, "Mumbai", null, "tanvi", true, 1)
        ];

        List<Product> productsToAdd = [];

        foreach (ProductSeed seed in productSeeds)
        {
            if (existingTitles.Contains(seed.Title))
            {
                continue;
            }

            if (!usersByUserName.TryGetValue(seed.SellerUserName, out AppUser? seller))
            {
                continue;
            }

            Product product = new()
            {
                Title = seed.Title,
                Description = seed.Description,
                Category = seed.Category,
                ListingType = seed.ListingType,
                Price = seed.ListingType == ProductListingType.Donate ? 0 : seed.Price,
                ItemCondition = seed.ItemCondition,
                Quantity = seed.Quantity,
                City = seed.City,
                ImageUrl = seed.ImageUrl,
                SellerId = seller.Id,
                IsApproved = seed.IsApproved,
                IsSold = false,
                CreatedOnUtc = now.AddDays(-seed.CreatedDaysAgo)
            };

            productsToAdd.Add(product);
            existingTitles.Add(seed.Title);
        }

        if (productsToAdd.Count > 0)
        {
            _dbContext.Products.AddRange(productsToAdd);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed record UserSeed(
        string FullName,
        string UserName,
        string Email,
        string Password,
        string? PhoneNumber,
        string City,
        bool IsVerified,
        bool IsBlocked,
        bool IsAdmin,
        int RegisteredDaysAgo);

    private sealed record ProductSeed(
        string Title,
        string Description,
        string Category,
        ProductListingType ListingType,
        decimal? Price,
        string ItemCondition,
        int Quantity,
        string City,
        string? ImageUrl,
        string SellerUserName,
        bool IsApproved,
        int CreatedDaysAgo);
}
