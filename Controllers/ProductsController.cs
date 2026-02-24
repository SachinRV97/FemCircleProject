using FemCircleProject.Services;
using FemCircleProject.ViewModels.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FemCircleProject.Controllers;

[Authorize]
public sealed class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProductsController(IProductService productService, IWebHostEnvironment webHostEnvironment)
    {
        _productService = productService;
        _webHostEnvironment = webHostEnvironment;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ProductSearchViewModel search, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(new ProductIndexViewModel { Search = search });
        }

        ProductIndexViewModel model = await _productService.BuildIndexAsync(search, cancellationToken);
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        ProductDetailsViewModel? model = await _productService.GetDetailsAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ProductCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        (bool success, string? imagePath, string? errorMessage) = await SaveProductImageAsync(model.ImageFile, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(nameof(model.ImageFile), errorMessage ?? "Image upload failed.");
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            model.ImageUrl = imagePath;
        }

        string currentUser = User.Identity?.Name ?? string.Empty;
        int newId = await _productService.CreateAsync(model, currentUser, cancellationToken);

        return RedirectToAction(nameof(Details), new { id = newId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ProductEditViewModel? model = await _productService.GetForEditAsync(id, User.Identity?.Name, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductEditViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        (bool success, string? imagePath, string? errorMessage) = await SaveProductImageAsync(model.ImageFile, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(nameof(model.ImageFile), errorMessage ?? "Image upload failed.");
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(imagePath))
        {
            model.ImageUrl = imagePath;
        }

        bool updated = await _productService.UpdateAsync(model, User.Identity?.Name, cancellationToken);
        if (!updated)
        {
            return Forbid();
        }

        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        bool deleted = await _productService.DeleteAsync(id, User.Identity?.Name, cancellationToken);
        if (!deleted)
        {
            return Forbid();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<(bool Success, string? Path, string? Error)> SaveProductImageAsync(IFormFile? imageFile, CancellationToken cancellationToken)
    {
        if (imageFile is null || imageFile.Length == 0)
        {
            return (true, null, null);
        }

        const long maxFileSize = 5 * 1024 * 1024;
        if (imageFile.Length > maxFileSize)
        {
            return (false, null, "Image size must be 5 MB or less.");
        }

        string extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
        HashSet<string> allowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

        if (!allowedExtensions.Contains(extension))
        {
            return (false, null, "Only JPG, JPEG, PNG, and WEBP files are allowed.");
        }

        if (string.IsNullOrWhiteSpace(_webHostEnvironment.WebRootPath))
        {
            return (false, null, "Server storage path is not available.");
        }

        string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadFolder);

        string fileName = $"{Guid.NewGuid():N}{extension}";
        string filePath = Path.Combine(uploadFolder, fileName);

        await using FileStream stream = new(filePath, FileMode.Create);
        await imageFile.CopyToAsync(stream, cancellationToken);

        return (true, $"/uploads/products/{fileName}", null);
    }
}
