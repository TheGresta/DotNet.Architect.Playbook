using Playbook.Persistence.Redis.Models;

namespace Playbook.Persistence.Redis;

/// <summary>
/// Defines the abstraction for managing product data persistence.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Asynchronously retrieves a product by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the <see cref="ProductDto"/> if found; otherwise, <see langword="null"/>.</returns>
    Task<ProductDto?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>
    /// Asynchronously retrieves all products available in the repository.
    /// </summary>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing a list of <see cref="ProductDto"/>.</returns>
    Task<List<ProductDto>> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Asynchronously creates a new product entry.
    /// </summary>
    /// <param name="product">The product data to create.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the created <see cref="ProductDto"/> with its assigned identifier.</returns>
    Task<ProductDto> CreateAsync(ProductDto product, CancellationToken ct);

    /// <summary>
    /// Asynchronously updates an existing product entry.
    /// </summary>
    /// <param name="product">The product data containing the identifier and updated values.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing the updated <see cref="ProductDto"/>, or <see langword="null"/> if the product does not exist.</returns>
    Task<ProductDto?> UpdateAsync(ProductDto product, CancellationToken ct);

    /// <summary>
    /// Asynchronously removes a product from the repository.
    /// </summary>
    /// <param name="id">The unique identifier of the product to remove.</param>
    /// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task{TResult}"/> containing <see langword="true"/> if the product was successfully removed; otherwise, <see langword="false"/>.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken ct);
}