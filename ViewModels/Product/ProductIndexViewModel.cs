namespace FemCircleProject.ViewModels.Product;

public sealed class ProductIndexViewModel
{
    public ProductSearchViewModel Search { get; set; } = new();
    public IReadOnlyList<ProductListItemViewModel> Products { get; set; } = Array.Empty<ProductListItemViewModel>();
    public int TotalCount { get; set; }
}
