# QuickPersistr.EntityFrameworkCore.Sqlite

An isolated in-memory SQLite persistence scope for [QuickPersistr](https://github.com/kilfour/QuickPersistr) and Entity Framework Core.

```bash
dotnet add package QuickPersistr.EntityFrameworkCore.Sqlite --version 0.0.1
```

## Complete quick start

The following belongs in a test project with your preferred test framework:

```csharp
using Microsoft.EntityFrameworkCore;
using QuickPersistr;
using QuickPersistr.EntityFrameworkCore.Sqlite;
using Xunit;

public sealed class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class LibraryDbContext(
    DbContextOptions<LibraryDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
}

public sealed class BookPersistence : Persistence<LibraryDbContext, Book>
{
    public override IPersistenceSpecification<LibraryDbContext> Define() =>
        Entity
            .PrimaryKey(book => book.Id)
            .Property(book => book.Title)
            .Property(book => book.Description)
            .Persist();
}

public sealed class BookPersistenceTests
{
    [Fact]
    public void Mapping_matches_the_contract() =>
        Persistr.Named("Books")
            .Scope(() => new SqlitePersistenceScope<LibraryDbContext>(
                options => new LibraryDbContext(options)))
            .Entities(new BookPersistence())
            .Run();
}
```

Each scope owns an open SQLite connection and a private in-memory database. `StartNewSession()` and optimistic-concurrency checks create fresh EF contexts over that same database. The connection and database are discarded when the scope is disposed.

Foreign-key enforcement is enabled by default. Additional EF options can be configured without taking ownership of the connection:

```csharp
new SqlitePersistenceScope<LibraryDbContext>(
    options => new LibraryDbContext(options),
    enforceForeignKeys: true,
    configureOptions: options => options.EnableSensitiveDataLogging());
```

## When SQLite is not enough

SQLite is excellent for fast integration tests, but it does not reproduce every production provider's SQL, types, conversions, constraints, or concurrency semantics. If the production database is PostgreSQL, use `QuickPersistr.EntityFrameworkCore.PostgreSql` for contracts that depend on those details.

This package supports EF Core 8.x and targets .NET 8. QuickPersistr is an early preview. APIs may change before version 1.0.
