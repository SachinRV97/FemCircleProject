namespace FemCircleProject.ViewModels.Product;

public sealed class ProductDetailsViewModel
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
    public string SellerDisplayName { get; set; } = string.Empty;
    public string SellerUserName { get; set; } = string.Empty;
    public bool CanManage { get; set; }
    public bool CanBook { get; set; }
    public bool IsSold { get; set; }
    public DateTime? SoldOnUtc { get; set; }
    public string? BoughtByUserName { get; set; }
    public bool IsBoughtByCurrentUser { get; set; }
    public DateTime PostedOnUtc { get; set; }
}
