using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Note.Backend.Data;
using Note.Backend.Services;
using Note.Backend.Models;

namespace Note.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly NoteDbContext _context;

    public ProductsController(IProductService productService, NoteDbContext context)
    {
        _productService = productService;
        _context = context;
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

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<Product>> GetProductBySlug(string slug)
    {
        var product = await _productService.GetProductBySlugAsync(slug);
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

    [HttpGet("{id}/pack-choices")]
    public async Task<IActionResult> GetPackChoices(string id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null) return NotFound();
        if (!product.IsPack) return BadRequest(new { Message = "This product is not a pack." });

        var choices = await _productService.GetPackChoicesAsync(id);
        return Ok(choices);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id}/pack-choices")]
    public async Task<IActionResult> SetPackChoices(string id, [FromBody] List<string> choiceProductIds)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null) return NotFound();

        var existing = await _context.PackChoices.Where(pc => pc.PackProductId == id).ToListAsync();
        _context.PackChoices.RemoveRange(existing);

        foreach (var choiceId in choiceProductIds)
        {
            _context.PackChoices.Add(new PackChoice { PackProductId = id, ChoiceProductId = choiceId });
        }

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Pack choices updated." });
    }
}
