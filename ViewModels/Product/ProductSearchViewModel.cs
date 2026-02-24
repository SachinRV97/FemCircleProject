using System.ComponentModel.DataAnnotations;

namespace FemCircleProject.ViewModels.Product;

public sealed class ProductSearchViewModel : IValidatableObject
{
    [Display(Name = "Search")]
    public string? Query { get; set; }

    public string? Category { get; set; }

    [Display(Name = "Listing Type")]
    public ProductListingType? ListingType { get; set; }

    [Range(typeof(decimal), "0", "1000000")]
    [Display(Name = "Min Price")]
    public decimal? MinPrice { get; set; }

    [Range(typeof(decimal), "0", "1000000")]
    [Display(Name = "Max Price")]
    public decimal? MaxPrice { get; set; }

    public string SortBy { get; set; } = "latest";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinPrice.HasValue && MaxPrice.HasValue && MinPrice > MaxPrice)
        {
            yield return new ValidationResult("Min price cannot be greater than max price.", new[] { nameof(MinPrice), nameof(MaxPrice) });
        }
    }
}
