using FemCircleProject.Data;
using FemCircleProject.Data.Entities;
using FemCircleProject.ViewModels.Product;
using Microsoft.EntityFrameworkCore;

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
            .Where(p => p.IsApproved);

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
                PostedOnUtc = p.CreatedOnUtc
            })
            .FirstOrDefaultAsync(cancellationToken);
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
            IsApproved = true,
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

        if (product is null || !await CanManageAsync(product, requestedByUserName, cancellationToken))
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
}
