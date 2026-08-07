# QuickPersistr.EntityFrameworkCore

The provider-neutral Entity Framework Core adapter for [QuickPersistr](https://github.com/kilfour/QuickPersistr).

Use this package when your test environment already creates and owns the database. For a ready-made isolated database, use the SQLite or PostgreSQL provider package instead.

```bash
dotnet add package QuickPersistr.EntityFrameworkCore --version 0.0.1
```

## Usage

Configure your EF provider normally and pass a context factory:

```csharp
using Microsoft.EntityFrameworkCore;
using QuickPersistr;
using QuickPersistr.EntityFrameworkCore;

var options = new DbContextOptionsBuilder<LibraryDbContext>()
    .UseYourProvider(testDatabaseConnectionString)
    .Options;

Persistr.Named("Books")
    .Scope(() => new EfPersistenceScope<LibraryDbContext>(
        () => new LibraryDbContext(options)))
    .Entities(new BookPersistence())
    .Run();
```

`UseYourProvider` represents the configuration extension supplied by the provider you choose.

## Database lifecycle

The scope:

- creates and disposes every `DbContext`;
- calls `Database.EnsureCreated()` by default;
- creates fresh contexts for `StartNewSession()`;
- creates independent contexts for optimistic-concurrency scenarios.

The context factory must always connect to the same isolated test database for the lifetime of the scope. The scope does not own external connections, containers, servers, or database cleanup. Pass `ensureCreated: false` when schema creation is handled by migrations or test infrastructure.

This package supports EF Core 8.x and targets .NET 8. QuickPersistr is an early preview. APIs may change before version 1.0.
