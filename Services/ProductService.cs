using FemCircleProject.Data;
using FemCircleProject.Data.Entities;
using FemCircleProject.ViewModels.Product;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FemCircleProject.Services;

public sealed class ProductService : IProductService
{
    private readonly FemCircleDbContext _dbContext;

    public ProductService(FemCircleDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProductIndexViewModel> BuildIndexAsync(ProductSearchViewModel search, CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = _dbContext.Products
            .AsNoTracking()
            .Where(p => p.IsApproved && !p.IsSold && p.BoughtByUserId == null);

        if (!string.IsNullOrWhiteSpace(search.Query))
        {
            string keyword = $"%{search.Query.Trim()}%";
            query = query.Where(p =>
                EF.Functions.Like(p.Title, keyword) ||
                EF.Functions.Like(p.Description, keyword));
        }

        if (!string.IsNullOrWhiteSpace(search.Category))
        {
            string category = $"%{search.Category.Trim()}%";
            query = query.Where(p => EF.Functions.Like(p.Category, category));
        }

        if (search.ListingType.HasValue)
        {
            ProductListingType listingType = search.ListingType.Value;
            query = query.Where(p => p.ListingType == listingType);
        }

        if (search.MinPrice.HasValue)
        {
            decimal minPrice = search.MinPrice.Value;
            query = query.Where(p => (p.Price ?? 0) >= minPrice);
        }

        if (search.MaxPrice.HasValue)
        {
            decimal maxPrice = search.MaxPrice.Value;
            query = query.Where(p => (p.Price ?? 0) <= maxPrice);
        }

        query = search.SortBy switch
        {
            "price_asc" => query.OrderBy(p => p.Price ?? 0).ThenByDescending(p => p.CreatedOnUtc),
            "price_desc" => query.OrderByDescending(p => p.Price ?? 0).ThenByDescending(p => p.CreatedOnUtc),
            _ => query.OrderByDescending(p => p.CreatedOnUtc)
        };

        int totalCount = await query.CountAsync(cancellationToken);

        List<ProductListItemViewModel> products = await query
            .Select(p => new ProductListItemViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Category = p.Category,
                ListingType = p.ListingType,
                Price = p.Price,
                ItemCondition = p.ItemCondition,
                SellerDisplayName = p.Seller.FullName,
                SellerUserName = p.Seller.UserName,
                City = p.City,
                ImageUrl = p.ImageUrl,
                PostedOnUtc = p.CreatedOnUtc
            })
            .ToListAsync(cancellationToken);

        return new ProductIndexViewModel
        {
            Search = search,
            Products = products,
            TotalCount = totalCount
        };
    }

    public Task<ProductDetailsViewModel?> GetDetailsAsync(int productId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new ProductDetailsViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                Category = p.Category,
                ListingType = p.ListingType,
                Price = p.Price,
                ItemCondition = p.ItemCondition,
                Quantity = p.Quantity,
                City = p.City,
                ImageUrl = p.ImageUrl,
                SellerDisplayName = p.Seller.FullName,
                SellerUserName = p.Seller.UserName,
                PostedOnUtc = p.CreatedOnUtc,
                IsSold = p.IsSold,
                SoldOnUtc = p.SoldOnUtc,
                BoughtByUserName = p.BoughtByUser != null ? p.BoughtByUser.UserName : null
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductActivityDashboardViewModel?> GetMyActivityAsync(string? currentUserName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currentUserName))
        {
            return null;
        }

        string normalizedUserName = currentUserName.Trim();

        AppUser? user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == normalizedUserName, cancellationToken);

        if (user is null)
        {
            return null;
        }

        List<ProductActivityItemViewModel> ownedItems = await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.SellerId == user.Id)
            .OrderByDescending(p => p.CreatedOnUtc)
            .Select(ToActivityItem())
            .ToListAsync(cancellationToken);

        List<ProductActivityItemViewModel> boughtItems = await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.BoughtByUserId == user.Id)
            .OrderByDescending(p => p.SoldOnUtc ?? p.CreatedOnUtc)
            .Select(ToActivityItem())
            .ToListAsync(cancellationToken);

        return new ProductActivityDashboardViewModel
        {
            UserDisplayName = user.FullName,
            ActiveListings = ownedItems
                .Where(x => x.IsApproved && !x.IsSold && string.IsNullOrWhiteSpace(x.BuyerUserName))
                .OrderByDescending(x => x.CreatedOnUtc)
                .ToList(),
            PendingListings = ownedItems
                .Where(x => !x.IsApproved)
                .OrderByDescending(x => x.CreatedOnUtc)
                .ToList(),
            PendingBuyerRequests = ownedItems
                .Where(x => x.IsApproved && !x.IsSold && !string.IsNullOrWhiteSpace(x.BuyerUserName))
                .OrderByDescending(x => x.CreatedOnUtc)
                .ToList(),
            SoldListings = ownedItems
                .Where(x => x.IsSold)
                .OrderByDescending(x => x.SoldOnUtc ?? x.CreatedOnUtc)
                .ToList(),
            BoughtItems = boughtItems
        };
    }

    public async Task<int> CreateAsync(ProductCreateViewModel model, string createdByUserName, CancellationToken cancellationToken = default)
    {
        string ownerUserName = string.IsNullOrWhiteSpace(createdByUserName) ? "admin" : createdByUserName.Trim();

        AppUser? seller = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.UserName == ownerUserName, cancellationToken);

        seller ??= await _dbContext.Users
            .OrderByDescending(u => u.IsAdmin)
            .ThenBy(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (seller is null)
        {
            throw new InvalidOperationException("No users are available to own a product listing.");
        }

        Product product = new()
        {
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            Category = model.Category.Trim(),
            ListingType = model.ListingType,
            Price = NormalizePrice(model.ListingType, model.Price),
            ItemCondition = model.ItemCondition.Trim(),
            Quantity = model.Quantity,
            City = model.City.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim(),
            SellerId = seller.Id,
            IsApproved = seller.IsAdmin,
            CreatedOnUtc = DateTime.UtcNow
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return product.Id;
    }

    public async Task<ProductEditViewModel?> GetForEditAsync(int productId, string? requestedByUserName, CancellationToken cancellationToken = default)
    {
        Product? product = await _dbContext.Products
            .Include(p => p.Seller)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null || !await CanManageAsync(product, requestedByUserName, cancellationToken))
        {
            return null;
        }

        return new ProductEditViewModel
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
            ImageUrl = product.ImageUrl
        };
    }

    public async Task<bool> UpdateAsync(ProductEditViewModel model, string? requestedByUserName, CancellationToken cancellationToken = default)
    {
        Product? product = await _dbContext.Products
            .Include(p => p.Seller)
            .FirstOrDefaultAsync(p => p.Id == model.Id, cancellationToken);

        if (product is null || product.IsSold || !await CanManageAsync(product, requestedByUserName, cancellationToken))
        {
            return false;
        }

        product.Title = model.Title.Trim();
        product.Description = model.Description.Trim();
        product.Category = model.Category.Trim();
        product.ListingType = model.ListingType;
        product.Price = NormalizePrice(model.ListingType, model.Price);
        product.ItemCondition = model.ItemCondition.Trim();
        product.Quantity = model.Quantity;
        product.City = model.City.Trim();
        product.ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int productId, string? requestedByUserName, CancellationToken cancellationToken = default)
    {
        Product? product = await _dbContext.Products
            .Include(p => p.Seller)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null || !await CanManageAsync(product, requestedByUserName, cancellationToken))
        {
            return false;
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> BookAsync(int productId, string? buyerUserName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(buyerUserName))
        {
            return false;
        }

        string normalizedBuyerUserName = buyerUserName.Trim();

        AppUser? buyer = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.UserName == normalizedBuyerUserName, cancellationToken);

        if (buyer is null || buyer.IsBlocked)
        {
            return false;
        }

        Product? product = await _dbContext.Products
            .Include(p => p.Seller)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null || !product.IsApproved || product.IsSold)
        {
            return false;
        }

        if (product.BoughtByUserId.HasValue)
        {
            return false;
        }

        if (product.SellerId == buyer.Id)
        {
            return false;
        }

        product.IsSold = false;
        product.SoldOnUtc = null;
        product.BoughtByUserId = buyer.Id;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ApproveBookingAsync(int productId, string? requestedByUserName, CancellationToken cancellationToken = default)
    {
        Product? product = await _dbContext.Products
            .Include(p => p.Seller)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null ||
            !product.BoughtByUserId.HasValue ||
            product.IsSold ||
            !await CanManageAsync(product, requestedByUserName, cancellationToken))
        {
            return false;
        }

        product.IsSold = true;
        product.SoldOnUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RejectBookingAsync(int productId, string? requestedByUserName, CancellationToken cancellationToken = default)
    {
        Product? product = await _dbContext.Products
            .Include(p => p.Seller)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null ||
            !product.BoughtByUserId.HasValue ||
            product.IsSold ||
            !await CanManageAsync(product, requestedByUserName, cancellationToken))
        {
            return false;
        }

        product.BoughtByUserId = null;
        product.SoldOnUtc = null;
        product.IsSold = false;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UndoBookingAsync(int productId, string? requestedByUserName, CancellationToken cancellationToken = default)
    {
        Product? product = await _dbContext.Products
            .Include(p => p.Seller)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null || !product.IsSold || !await CanManageAsync(product, requestedByUserName, cancellationToken))
        {
            return false;
        }

        product.IsSold = false;
        product.SoldOnUtc = null;
        product.BoughtByUserId = null;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> CanManageAsync(Product product, string? requestedByUserName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requestedByUserName))
        {
            return false;
        }

        string normalizedUserName = requestedByUserName.Trim();

        if (string.Equals(product.Seller.UserName, normalizedUserName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.UserName == normalizedUserName && u.IsAdmin, cancellationToken);
    }

    private static decimal? NormalizePrice(ProductListingType listingType, decimal? rawPrice)
    {
        return listingType == ProductListingType.Donate ? 0 : rawPrice;
    }

    private static Expression<Func<Product, ProductActivityItemViewModel>> ToActivityItem()
    {
        return p => new ProductActivityItemViewModel
        {
            Id = p.Id,
            Title = p.Title,
            Category = p.Category,
            ListingType = p.ListingType,
            Price = p.Price,
            ItemCondition = p.ItemCondition,
            City = p.City,
            SellerDisplayName = p.Seller.FullName,
            SellerUserName = p.Seller.UserName,
            BuyerUserName = p.BoughtByUser != null ? p.BoughtByUser.UserName : null,
            IsApproved = p.IsApproved,
            IsSold = p.IsSold,
            CreatedOnUtc = p.CreatedOnUtc,
            SoldOnUtc = p.SoldOnUtc
        };
    }
}
