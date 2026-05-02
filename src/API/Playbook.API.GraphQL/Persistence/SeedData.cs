namespace Playbook.API.GraphQL.Persistence;

public static class SeedData
{
    private static readonly string[] FirstNames =
    [
        "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda",
        "William", "Barbara", "David", "Elizabeth", "Richard", "Susan", "Joseph", "Jessica",
        "Thomas", "Sarah", "Charles", "Karen", "Christopher", "Nancy"
    ];

    private static readonly string[] LastNames =
    [
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
        "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson",
        "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez"
    ];

    private static readonly string[] Nationalities =
    [
        "American", "British", "French", "German", "Japanese",
        "Brazilian", "Canadian", "Australian", "Indian", "Spanish"
    ];

    private static readonly string[] TitleAdjectives =
    [
        "The Great", "A Brief", "The Dark", "Lost", "The Last", "Infinite", "Silent",
        "The Hidden", "Brave New", "The Long", "Forgotten", "The Seventh", "Beyond the",
        "Into the", "Echoes of", "Shadows of", "Rise of the", "Fall of the", "The Secret",
        "Whispers of"
    ];

    private static readonly string[] TitleNouns =
    [
        "Journey", "History", "World", "House", "Garden", "Fire", "Wind", "Ocean",
        "Mountain", "Dream", "Empire", "Kingdom", "Storm", "Night", "Dawn", "Horizon",
        "Abyss", "Frontier", "Labyrinth", "Chronicle"
    ];

    public static (IReadOnlyList<Author> Authors, IReadOnlyList<Book> Books) Generate()
    {
        // Fixed seed for deterministic output on every startup
        var rng = new Random(42);

        var authors = Enumerable.Range(0, 100)
            .Select(_ =>
            {
                var bytes = new byte[16];
                rng.NextBytes(bytes);
                return new Author(
                    new Guid(bytes),
                    FirstNames[rng.Next(FirstNames.Length)],
                    LastNames[rng.Next(LastNames.Length)],
                    Nationalities[rng.Next(Nationalities.Length)],
                    new DateOnly(rng.Next(1920, 1985), rng.Next(1, 13), rng.Next(1, 28)),
                    DateTime.UtcNow);
            })
            .ToList();

        var genres = Enum.GetValues<Genre>();

        var books = Enumerable.Range(0, 1000)
            .Select(_ =>
            {
                var author = authors[rng.Next(authors.Count)];
                var genre = genres[rng.Next(genres.Length)];
                var adj = TitleAdjectives[rng.Next(TitleAdjectives.Length)];
                var noun = TitleNouns[rng.Next(TitleNouns.Length)];
                var rating = Math.Round(1.0 + rng.NextDouble() * 4.0, 1);
                var bytes = new byte[16];
                rng.NextBytes(bytes);

                return new Book(
                    new Guid(bytes),
                    author.Id,
                    $"{adj} {noun}",
                    genre,
                    rng.Next(1900, 2025),
                    rating,
                    rng.Next(80, 901),
                    GenerateIsbn(rng),
                    DateTime.UtcNow);
            })
            .ToList();

        return (authors, books);
    }

    private static string GenerateIsbn(Random rng)
        => $"978-{rng.Next(0, 10)}-{rng.Next(100000, 999999)}-{rng.Next(10, 99)}-{rng.Next(0, 10)}";
}
