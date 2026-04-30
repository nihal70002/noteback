using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Note.Backend.Services;
using Note.Backend.Models;

namespace Note.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts([FromQuery] string? search, [FromQuery] string? category, [FromQuery] string? sort)
    {
        var products = await _productService.GetAllProductsAsync(search, category, sort);
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(string id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        var createdProduct = await _productService.CreateProductAsync(product);
        return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, createdProduct);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(string id, Product product)
    {
        var success = await _productService.UpdateProductAsync(id, product);
        if (!success) return BadRequest();
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(string id)
    {
        var success = await _productService.DeleteProductAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
}
