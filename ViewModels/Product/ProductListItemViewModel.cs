namespace FemCircleProject.ViewModels.Product;

public sealed class ProductListItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public ProductListingType ListingType { get; set; }
    public decimal? Price { get; set; }
    public string ItemCondition { get; set; } = string.Empty;
    public string SellerDisplayName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public DateTime PostedOnUtc { get; set; }
}
