// Prevent Turkish-I bug: HC uses ToUpper() for enum names; without invariant culture
// the letter 'i' becomes 'İ' (U+0130) on Turkish OS, producing SCÏENCE instead of SCIENCE.
System.Globalization.CultureInfo.DefaultThreadCurrentCulture =
    System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture =
    System.Globalization.CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRepositories();    // Singleton in-memory stores, 100 authors + 1000 books
builder.Services.AddGraphQlServer();   // Hot Chocolate server with all GraphQL features

var app = builder.Build();

app.UseHttpsRedirection();

// Serves the GraphQL endpoint at /graphql
// Banana Cake Pop (Nitro) IDE is available at /graphql in a browser (GET request)
app.MapGraphQL();

app.Run();
