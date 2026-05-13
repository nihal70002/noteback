using Note.Backend.Models;

namespace Note.Backend.Services;

public interface IProductService
{
    Task<IEnumerable<Product>> GetAllProductsAsync(string? search = null, string? category = null, string? sort = null);
    Task<Product?> GetProductByIdAsync(string id);
    Task<Product?> GetProductBySlugAsync(string slug);
    Task<Product> CreateProductAsync(Product product);
    Task<bool> UpdateProductAsync(string id, Product product);
    Task<bool> DeleteProductAsync(string id);
    Task<IEnumerable<Product>> GetPackChoicesAsync(string packProductId);
}
