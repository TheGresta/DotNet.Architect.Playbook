namespace Playbook.API.GraphQL.Extensions;

public static class GraphQlServiceExtensions
{
    public static IServiceCollection AddGraphQlServer(this IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            .AddTypeExtension<BookType>()
            .AddTypeExtension<AuthorType>()
            .AddFiltering()
            .AddSorting()
            // HC 16: depth limit moved to parser options — rejects nested queries before execution
            .ModifyParserOptions(o => o.MaxAllowedRecursionDepth = 5);

        return services;
    }

    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        // Singleton lifetime: seed data persists across requests so mutations accumulate
        var (authors, books) = SeedData.Generate();
        services.AddSingleton<IAuthorRepository>(new InMemoryAuthorRepository(authors));
        services.AddSingleton<IBookRepository>(new InMemoryBookRepository(books));
        return services;
    }
}
