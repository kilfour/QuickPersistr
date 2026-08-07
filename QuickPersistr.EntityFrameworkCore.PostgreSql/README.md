# QuickPersistr.EntityFrameworkCore.PostgreSql

An isolated PostgreSQL database scope for [QuickPersistr](https://github.com/kilfour/QuickPersistr) and Entity Framework Core, powered by Npgsql.

```bash
dotnet add package QuickPersistr.EntityFrameworkCore.PostgreSql --version 0.0.1
```

> [!WARNING]
> Use a dedicated PostgreSQL test server. The supplied user must be allowed to create, terminate connections to, and drop databases. Never supply production credentials.

## Usage

```csharp
using QuickPersistr;
using QuickPersistr.EntityFrameworkCore.PostgreSql;

var serverConnectionString =
    Environment.GetEnvironmentVariable("QUICKPERSISTR_POSTGRES")
    ?? throw new InvalidOperationException(
        "Set QUICKPERSISTR_POSTGRES to a dedicated PostgreSQL test server.");

Persistr.Named("Books on PostgreSQL")
    .Scope(() => new PostgreSqlPersistenceScope<LibraryDbContext>(
        serverConnectionString,
        options => new LibraryDbContext(options)))
    .Entities(new BookPersistence())
    .Run();
```

The connection string identifies the server and maintenance database used to issue `CREATE DATABASE` and `DROP DATABASE`. If it omits the database, `postgres` is used.

## Database lifecycle

For every scope, the adapter:

1. creates a uniquely named database such as `quickpersistr_<guid>`;
2. builds the schema with `Database.EnsureCreated()`;
3. opens fresh contexts against that database for session and concurrency checks;
4. terminates remaining connections and drops only that generated database on disposal.

Generated database connections have pooling disabled so cleanup is deterministic. Different scopes use different names and can run in parallel. If a test process is killed before disposal, a generated database may remain; it can be identified by the `quickpersistr_` prefix. A custom prefix may contain at most 30 ASCII letters, digits, or underscores.

If containers, migrations, or another fixture already manage database lifetime, use `QuickPersistr.EntityFrameworkCore.EfPersistenceScope<TDbContext>` instead.

This package supports EF Core 8.x and Npgsql 8.x, and targets .NET 8. QuickPersistr is an early preview; APIs may change before version 1.0.
