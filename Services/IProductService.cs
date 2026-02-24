using FemCircleProject.ViewModels.Product;

namespace FemCircleProject.Services;

public interface IProductService
{
    Task<ProductIndexViewModel> BuildIndexAsync(ProductSearchViewModel search, CancellationToken cancellationToken = default);
    Task<ProductDetailsViewModel?> GetDetailsAsync(int productId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(ProductCreateViewModel model, string createdByUserName, CancellationToken cancellationToken = default);
    Task<ProductEditViewModel?> GetForEditAsync(int productId, string? requestedByUserName, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(ProductEditViewModel model, string? requestedByUserName, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int productId, string? requestedByUserName, CancellationToken cancellationToken = default);
}
