using Microsoft.EntityFrameworkCore;
using QuickPersistr.EntityFrameworkCore.PostgreSql;
using QuickPersistr.EntityFrameworkCore.Sqlite;

using var scope = new SqlitePersistenceScope<SmokeDbContext>(
    options => new SmokeDbContext(options));

var added = scope.Add(new SmokeEntity { Name = "From the package" });
scope.Commit();
scope.StartNewSession();

var reloaded = scope.GetById<SmokeEntity>(added.Id);
if (reloaded.Name != added.Name)
    throw new InvalidOperationException("The packaged SQLite scope did not persist the entity.");

// PostgreSQL needs a server at runtime. Constructing this delegate verifies that
// its public API and all transitive compile assets are present in the package.
Func<string, PostgreSqlPersistenceScope<SmokeDbContext>> createPostgreSqlScope =
    connectionString => new PostgreSqlPersistenceScope<SmokeDbContext>(
    connectionString,
    options => new SmokeDbContext(options));
_ = createPostgreSqlScope;

Console.WriteLine("Packaged SQLite runtime and PostgreSQL compile smoke tests passed.");

public sealed class SmokeEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class SmokeDbContext(DbContextOptions<SmokeDbContext> options)
    : DbContext(options)
{
    public DbSet<SmokeEntity> Entities => Set<SmokeEntity>();
}
