using FemCircleProject.Services;
using FemCircleProject.ViewModels.Product;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FemCircleProject.Controllers;

[Authorize]
public sealed class ProductsController : Controller
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
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
}
