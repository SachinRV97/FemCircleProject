using FemCircleProject.ViewModels.Product;

namespace FemCircleProject.ViewModels.Admin;

public sealed class AdminProductSummaryViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public ProductListingType ListingType { get; set; }
    public string SellerUserName { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}
