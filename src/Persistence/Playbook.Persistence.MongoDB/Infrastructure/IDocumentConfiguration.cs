using MongoDB.Driver;
using Playbook.Persistence.MongoDB.Domain;

namespace Playbook.Persistence.MongoDB.Infrastructure;

/// <summary>
/// Defines the configuration contract for a specific MongoDB document type, including index definitions and initial seed data.
/// </summary>
/// <typeparam name="TDocument">The type of the document to configure, which must inherit from <see cref="BaseDocument"/>.</typeparam>
/// <remarks>
/// Implementations of this interface are typically consumed during the database initialization or migration phase 
/// to ensure the collection schema and indexes are correctly optimized.
/// </remarks>
internal interface IDocumentConfiguration<TDocument> where TDocument : BaseDocument
{
    /// <summary>
    /// Configures the indexes for the document collection using the provided <see cref="IndexKeysDefinitionBuilder{TDocument}"/>.
    /// </summary>
    /// <param name="builder">The builder used to define index keys (e.g., Ascending, Descending, or GeoSpatial).</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="CreateIndexModel{TDocument}"/> representing the set of indexes to be created.</returns>
    /// <remarks>
    /// <para>
    /// Proper indexing is critical for query performance. You can define single-field, compound, or TTL (Time To Live) indexes here.
    /// </para>
    /// <code>
    /// // Example implementation:
    /// yield return new CreateIndexModel&lt;TDocument&gt;(builder.Ascending(x => x.CreatedAt));
    /// </code>
    /// </remarks>
    IEnumerable<CreateIndexModel<TDocument>> ConfigureIndexes(IndexKeysDefinitionBuilder<TDocument> builder);

    /// <summary>
    /// Provides a collection of initial data to be inserted into the MongoDB collection upon initialization.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{T}"/> containing the initial documents of type <typeparamref name="TDocument"/>.</returns>
    /// <remarks>
    /// This method is intended for "cold start" scenarios, such as inserting administrative users, 
    /// lookup tables, or default configuration settings.
    /// </remarks>
    IEnumerable<TDocument> SeedData();
}
