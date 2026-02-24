using FemCircleProject.ViewModels.Product;

namespace FemCircleProject.Services;

public sealed class ProductService : IProductService
{
    private readonly InMemoryAppStore _store;

    public ProductService(InMemoryAppStore store)
    {
        _store = store;
    }

    public Task<ProductIndexViewModel> BuildIndexAsync(ProductSearchViewModel search, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ProductRecord> products = _store.GetProductsSnapshot();
        Dictionary<string, string> sellerNames = _store.GetUsersSnapshot()
            .ToDictionary(u => u.UserName, u => u.FullName, StringComparer.OrdinalIgnoreCase);

        IEnumerable<ProductRecord> query = products.Where(p => p.IsApproved);

        if (!string.IsNullOrWhiteSpace(search.Query))
        {
            string keyword = search.Query.Trim();
            query = query.Where(p =>
                p.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search.Category))
        {
            string category = search.Category.Trim();
            query = query.Where(p => p.Category.Contains(category, StringComparison.OrdinalIgnoreCase));
        }

        if (search.ListingType.HasValue)
        {
            query = query.Where(p => p.ListingType == search.ListingType.Value);
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

        List<ProductListItemViewModel> items = query
            .Select(p => new ProductListItemViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Category = p.Category,
                ListingType = p.ListingType,
                Price = p.Price,
                ItemCondition = p.ItemCondition,
                SellerDisplayName = sellerNames.TryGetValue(p.SellerUserName, out string? fullName) ? fullName : p.SellerUserName,
                City = p.City,
                PostedOnUtc = p.CreatedOnUtc
            })
            .ToList();

        ProductIndexViewModel model = new()
        {
            Search = search,
            Products = items,
            TotalCount = items.Count
        };

        return Task.FromResult(model);
    }

    public Task<ProductDetailsViewModel?> GetDetailsAsync(int productId, CancellationToken cancellationToken = default)
    {
        ProductRecord? product = _store.GetProductById(productId);
        if (product is null)
        {
            return Task.FromResult<ProductDetailsViewModel?>(null);
        }

        UserRecord? seller = _store.GetUserByUserName(product.SellerUserName);
        ProductDetailsViewModel model = new()
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
            SellerDisplayName = seller?.FullName ?? product.SellerUserName,
            PostedOnUtc = product.CreatedOnUtc
        };

        return Task.FromResult<ProductDetailsViewModel?>(model);
    }

    public Task<int> CreateAsync(ProductCreateViewModel model, string createdByUserName, CancellationToken cancellationToken = default)
    {
        string owner = string.IsNullOrWhiteSpace(createdByUserName) ? "admin" : createdByUserName;
        ProductRecord record = new()
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
            SellerUserName = owner,
            CreatedOnUtc = DateTime.UtcNow,
            IsApproved = true
        };

        int productId = _store.AddProduct(record);
        return Task.FromResult(productId);
    }

    public Task<ProductEditViewModel?> GetForEditAsync(int productId, string? requestedByUserName, CancellationToken cancellationToken = default)
    {
        ProductRecord? product = _store.GetProductById(productId);
        if (product is null || !CanManage(product, requestedByUserName))
        {
            return Task.FromResult<ProductEditViewModel?>(null);
        }

        ProductEditViewModel model = new()
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

        return Task.FromResult<ProductEditViewModel?>(model);
    }

    public Task<bool> UpdateAsync(ProductEditViewModel model, string? requestedByUserName, CancellationToken cancellationToken = default)
    {
        ProductRecord? existing = _store.GetProductById(model.Id);
        if (existing is null || !CanManage(existing, requestedByUserName))
        {
            return Task.FromResult(false);
        }

        existing.Title = model.Title.Trim();
        existing.Description = model.Description.Trim();
        existing.Category = model.Category.Trim();
        existing.ListingType = model.ListingType;
        existing.Price = NormalizePrice(model.ListingType, model.Price);
        existing.ItemCondition = model.ItemCondition.Trim();
        existing.Quantity = model.Quantity;
        existing.City = model.City.Trim();
        existing.ImageUrl = string.IsNullOrWhiteSpace(model.ImageUrl) ? null : model.ImageUrl.Trim();

        bool updated = _store.UpdateProduct(existing);
        return Task.FromResult(updated);
    }

    public Task<bool> DeleteAsync(int productId, string? requestedByUserName, CancellationToken cancellationToken = default)
    {
        ProductRecord? existing = _store.GetProductById(productId);
        if (existing is null || !CanManage(existing, requestedByUserName))
        {
            return Task.FromResult(false);
        }

        bool deleted = _store.DeleteProduct(productId);
        return Task.FromResult(deleted);
    }

    private bool CanManage(ProductRecord product, string? requestedByUserName)
    {
        if (string.IsNullOrWhiteSpace(requestedByUserName))
        {
            return false;
        }

        if (string.Equals(product.SellerUserName, requestedByUserName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        UserRecord? user = _store.GetUserByUserName(requestedByUserName);
        return user?.IsAdmin == true;
    }

    private static decimal? NormalizePrice(ProductListingType listingType, decimal? rawPrice)
    {
        if (listingType == ProductListingType.Donate)
        {
            return 0;
        }

        return rawPrice;
    }
}
