using Microsoft.EntityFrameworkCore;
using Note.Backend.Data;
using Note.Backend.Models;
using System.Text.RegularExpressions;

namespace Note.Backend.Services;

public class ProductService : IProductService
{
    private readonly NoteDbContext _context;

    public ProductService(NoteDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Product>> GetAllProductsAsync(string? search = null, string? category = null, string? sort = null)
    {
        var query = _context.Products.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(normalizedSearch) ||
                p.Category.ToLower().Contains(normalizedSearch) ||
                (p.Description != null && p.Description.ToLower().Contains(normalizedSearch)));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            query = query.Where(p => p.Category == category);
        }

        query = sort switch
        {
            "price-asc" => query.OrderBy(p => p.Price),
            "price-desc" => query.OrderByDescending(p => p.Price),
            "name" => query.OrderBy(p => p.Name),
            "rating" => query.OrderByDescending(p => p.AverageRating).ThenByDescending(p => p.ReviewCount),
            "newest" => query.OrderByDescending(p => p.IsNew).ThenBy(p => p.Name),
            _ => query.OrderBy(p => p.Name)
        };

        return await query.ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(string id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<Product?> GetProductBySlugAsync(string slug)
    {
        var normalizedSlug = BuildSeoSlug(slug);
        var products = await _context.Products.AsNoTracking().ToListAsync();

        return products.FirstOrDefault(product => BuildSeoSlug(product.Name) == normalizedSlug);
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        product.Id = Guid.NewGuid().ToString();
        product.AverageRating = 0;
        product.ReviewCount = 0;
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task<bool> UpdateProductAsync(string id, Product product)
    {
        if (id != product.Id) return false;

        _context.Entry(product).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await ProductExists(id)) return false;
            throw;
        }
    }

    public async Task<bool> DeleteProductAsync(string id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<bool> ProductExists(string id)
    {
        return await _context.Products.AnyAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<Product>> GetPackChoicesAsync(string packProductId)
    {
        return await _context.PackChoices
            .Where(pc => pc.PackProductId == packProductId)
            .Include(pc => pc.ChoiceProduct)
            .Select(pc => pc.ChoiceProduct)
            .Where(p => p != null)
            .Cast<Product>()
            .ToListAsync();
    }

    private static string BuildSeoSlug(string? value)
    {
        var slug = Regex.Replace((value ?? string.Empty).ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = "papercues";
        }

        return slug.Contains("notebook") || slug.Contains("journal") || slug.Contains("diary")
            ? slug
            : $"{slug}-aesthetic-notebook";
    }
}
