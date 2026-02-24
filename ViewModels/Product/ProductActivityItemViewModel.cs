namespace FemCircleProject.ViewModels.Product;

public sealed class ProductActivityItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public ProductListingType ListingType { get; set; }
    public decimal? Price { get; set; }
    public string ItemCondition { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string SellerDisplayName { get; set; } = string.Empty;
    public string SellerUserName { get; set; } = string.Empty;
    public string? BuyerUserName { get; set; }
    public bool IsApproved { get; set; }
    public bool IsSold { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? SoldOnUtc { get; set; }
}
