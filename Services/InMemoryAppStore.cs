using FemCircleProject.ViewModels.Account;
using FemCircleProject.ViewModels.Product;

namespace FemCircleProject.Services;

public sealed class InMemoryAppStore
{
    private readonly object _sync = new();
    private readonly List<UserRecord> _users = new();
    private readonly List<ProductRecord> _products = new();
    private int _nextUserId = 1;
    private int _nextProductId = 1;

    public InMemoryAppStore()
    {
        Seed();
    }

    internal bool TryCreateUser(RegisterViewModel model, out UserRecord? createdUser, out string? errorMessage)
    {
        lock (_sync)
        {
            if (_users.Any(u => string.Equals(u.UserName, model.UserName, StringComparison.OrdinalIgnoreCase)))
            {
                createdUser = null;
                errorMessage = "Username already exists.";
                return false;
            }

            if (_users.Any(u => string.Equals(u.Email, model.Email, StringComparison.OrdinalIgnoreCase)))
            {
                createdUser = null;
                errorMessage = "Email already exists.";
                return false;
            }

            UserRecord user = new()
            {
                Id = _nextUserId++,
                FullName = model.FullName.Trim(),
                UserName = model.UserName.Trim(),
                Email = model.Email.Trim(),
                Password = model.Password,
                PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
                City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim(),
                IsVerified = true,
                IsBlocked = false,
                IsAdmin = false,
                RegisteredOnUtc = DateTime.UtcNow
            };

            _users.Add(user);
            createdUser = CloneUser(user);
            errorMessage = null;
            return true;
        }
    }

    internal UserRecord? GetUserForSignIn(string usernameOrEmail, string password)
    {
        lock (_sync)
        {
            UserRecord? user = _users.FirstOrDefault(u =>
                (string.Equals(u.UserName, usernameOrEmail, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(u.Email, usernameOrEmail, StringComparison.OrdinalIgnoreCase)) &&
                string.Equals(u.Password, password, StringComparison.Ordinal));

            return user is null ? null : CloneUser(user);
        }
    }

    internal UserRecord? GetUserByUserName(string? userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return null;
        }

        lock (_sync)
        {
            UserRecord? user = _users.FirstOrDefault(u => string.Equals(u.UserName, userName, StringComparison.OrdinalIgnoreCase));
            return user is null ? null : CloneUser(user);
        }
    }

    internal IReadOnlyList<UserRecord> GetUsersSnapshot()
    {
        lock (_sync)
        {
            return _users.Select(CloneUser).ToList();
        }
    }

    internal IReadOnlyList<ProductRecord> GetProductsSnapshot()
    {
        lock (_sync)
        {
            return _products.Select(CloneProduct).ToList();
        }
    }

    internal ProductRecord? GetProductById(int productId)
    {
        lock (_sync)
        {
            ProductRecord? product = _products.FirstOrDefault(p => p.Id == productId);
            return product is null ? null : CloneProduct(product);
        }
    }

    internal int AddProduct(ProductRecord product)
    {
        lock (_sync)
        {
            ProductRecord copy = CloneProduct(product);
            copy.Id = _nextProductId++;
            _products.Add(copy);
            return copy.Id;
        }
    }

    internal bool UpdateProduct(ProductRecord updatedProduct)
    {
        lock (_sync)
        {
            int index = _products.FindIndex(p => p.Id == updatedProduct.Id);
            if (index < 0)
            {
                return false;
            }

            _products[index] = CloneProduct(updatedProduct);
            return true;
        }
    }

    internal bool DeleteProduct(int productId)
    {
        lock (_sync)
        {
            ProductRecord? existing = _products.FirstOrDefault(p => p.Id == productId);
            if (existing is null)
            {
                return false;
            }

            _products.Remove(existing);
            return true;
        }
    }

    private void Seed()
    {
        UserRecord admin = new()
        {
            Id = _nextUserId++,
            FullName = "FemCircle Admin",
            UserName = "admin",
            Email = "admin@femcircle.local",
            Password = "Admin@123",
            PhoneNumber = null,
            City = "Mumbai",
            IsVerified = true,
            IsBlocked = false,
            IsAdmin = true,
            RegisteredOnUtc = DateTime.UtcNow.AddMonths(-2)
        };

        UserRecord userA = new()
        {
            Id = _nextUserId++,
            FullName = "Priya Sharma",
            UserName = "priya",
            Email = "priya@femcircle.local",
            Password = "Member@123",
            PhoneNumber = "9999999999",
            City = "Pune",
            IsVerified = true,
            IsBlocked = false,
            IsAdmin = false,
            RegisteredOnUtc = DateTime.UtcNow.AddDays(-20)
        };

        UserRecord userB = new()
        {
            Id = _nextUserId++,
            FullName = "Ananya Rao",
            UserName = "ananya",
            Email = "ananya@femcircle.local",
            Password = "Member@123",
            PhoneNumber = "8888888888",
            City = "Hyderabad",
            IsVerified = true,
            IsBlocked = false,
            IsAdmin = false,
            RegisteredOnUtc = DateTime.UtcNow.AddDays(-8)
        };

        _users.Add(admin);
        _users.Add(userA);
        _users.Add(userB);

        _products.Add(new ProductRecord
        {
            Id = _nextProductId++,
            Title = "Handcrafted Cotton Kurti",
            Description = "Lightly used festive kurti in excellent condition.",
            Category = "Clothing",
            ListingType = ProductListingType.Sell,
            Price = 650,
            ItemCondition = "Like New",
            Quantity = 1,
            City = "Pune",
            ImageUrl = "https://images.unsplash.com/photo-1610030469678-8f49c9b0839d?auto=format&fit=crop&w=900&q=80",
            SellerUserName = userA.UserName,
            CreatedOnUtc = DateTime.UtcNow.AddDays(-3),
            IsApproved = true
        });

        _products.Add(new ProductRecord
        {
            Id = _nextProductId++,
            Title = "Study Desk Organizer Set",
            Description = "Wooden organizer set, ideal for home office and study corner.",
            Category = "Home Decor",
            ListingType = ProductListingType.Exchange,
            Price = null,
            ItemCondition = "Good",
            Quantity = 1,
            City = "Hyderabad",
            ImageUrl = null,
            SellerUserName = userB.UserName,
            CreatedOnUtc = DateTime.UtcNow.AddDays(-2),
            IsApproved = true
        });

        _products.Add(new ProductRecord
        {
            Id = _nextProductId++,
            Title = "Books for Donation - Career Prep",
            Description = "Set of exam prep books available for donation.",
            Category = "Books",
            ListingType = ProductListingType.Donate,
            Price = 0,
            ItemCondition = "Good",
            Quantity = 4,
            City = "Mumbai",
            ImageUrl = null,
            SellerUserName = userA.UserName,
            CreatedOnUtc = DateTime.UtcNow.AddDays(-1),
            IsApproved = false
        });
    }

    private static UserRecord CloneUser(UserRecord user)
    {
        return new UserRecord
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName,
            Email = user.Email,
            Password = user.Password,
            PhoneNumber = user.PhoneNumber,
            City = user.City,
            IsVerified = user.IsVerified,
            IsBlocked = user.IsBlocked,
            IsAdmin = user.IsAdmin,
            RegisteredOnUtc = user.RegisteredOnUtc
        };
    }

    private static ProductRecord CloneProduct(ProductRecord product)
    {
        return new ProductRecord
        {
            Id = product.Id,
            Title = product.Title,
            Description = product.Description,
            Category = product.Category,
            ListingType = product.ListingType,
            Price = product.Price,
            ItemCondition = product.ItemCondition,
            Quantity = product.Quantity,
            City = product.City,
            ImageUrl = product.ImageUrl,
            SellerUserName = product.SellerUserName,
            CreatedOnUtc = product.CreatedOnUtc,
            IsApproved = product.IsApproved
        };
    }
}

internal sealed class UserRecord
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? City { get; set; }
    public bool IsVerified { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsAdmin { get; set; }
    public DateTime RegisteredOnUtc { get; set; }
}

internal sealed class ProductRecord
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public ProductListingType ListingType { get; set; }
    public decimal? Price { get; set; }
    public string ItemCondition { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string City { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string SellerUserName { get; set; } = string.Empty;
    public DateTime CreatedOnUtc { get; set; }
    public bool IsApproved { get; set; }
}
