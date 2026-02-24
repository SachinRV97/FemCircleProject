using FemCircleProject.ViewModels.Product;

namespace FemCircleProject.Data.Entities;

public sealed class Product
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
    public bool IsApproved { get; set; }
    public bool IsSold { get; set; }
    public DateTime? SoldOnUtc { get; set; }
    public DateTime CreatedOnUtc { get; set; }

    public int SellerId { get; set; }
    public AppUser Seller { get; set; } = null!;
    public int? BoughtByUserId { get; set; }
    public AppUser? BoughtByUser { get; set; }
}
