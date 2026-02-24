using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FemCircleProject.ViewModels.Product;

public sealed class ProductEditViewModel : IValidatableObject
{
    [Required]
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(3000)]
    [DataType(DataType.MultilineText)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string Category { get; set; } = string.Empty;

    [Display(Name = "Listing Type")]
    public ProductListingType ListingType { get; set; } = ProductListingType.Sell;

    [Range(typeof(decimal), "0", "1000000")]
    public decimal? Price { get; set; }

    [Required]
    [StringLength(40)]
    [Display(Name = "Condition")]
    public string ItemCondition { get; set; } = string.Empty;

    [Range(1, 500)]
    public int Quantity { get; set; } = 1;

    [Required]
    [StringLength(80)]
    public string City { get; set; } = string.Empty;

    [StringLength(600)]
    [Display(Name = "Image URL")]
    public string? ImageUrl { get; set; }

    [Display(Name = "Upload Image")]
    public IFormFile? ImageFile { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ListingType == ProductListingType.Sell && (!Price.HasValue || Price <= 0))
        {
            yield return new ValidationResult("Price is required for sell listings.", new[] { nameof(Price) });
        }

        if (ListingType == ProductListingType.Donate && Price.HasValue && Price.Value > 0)
        {
            yield return new ValidationResult("Donate listings must have no price or zero price.", new[] { nameof(Price) });
        }

        if (!string.IsNullOrWhiteSpace(ImageUrl))
        {
            string imageUrl = ImageUrl.Trim();
            bool isAppRelative = imageUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase);
            bool isHttpUrl = Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? parsedUri) &&
                             (parsedUri.Scheme == Uri.UriSchemeHttp || parsedUri.Scheme == Uri.UriSchemeHttps);

            if (!isAppRelative && !isHttpUrl)
            {
                yield return new ValidationResult("Image URL must be a valid http/https URL.", new[] { nameof(ImageUrl) });
            }
        }
    }
}
