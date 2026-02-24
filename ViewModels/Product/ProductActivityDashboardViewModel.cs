namespace FemCircleProject.ViewModels.Product;

public sealed class ProductActivityDashboardViewModel
{
    public string UserDisplayName { get; set; } = string.Empty;
    public IReadOnlyList<ProductActivityItemViewModel> ActiveListings { get; set; } = Array.Empty<ProductActivityItemViewModel>();
    public IReadOnlyList<ProductActivityItemViewModel> PendingListings { get; set; } = Array.Empty<ProductActivityItemViewModel>();
    public IReadOnlyList<ProductActivityItemViewModel> SoldListings { get; set; } = Array.Empty<ProductActivityItemViewModel>();
    public IReadOnlyList<ProductActivityItemViewModel> BoughtItems { get; set; } = Array.Empty<ProductActivityItemViewModel>();
}
