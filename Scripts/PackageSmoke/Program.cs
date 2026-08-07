using Microsoft.EntityFrameworkCore;
using Npgsql;
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

Console.WriteLine("Packaged SQLite runtime smoke test passed.");

var postgresConnectionString =
    Environment.GetEnvironmentVariable("QUICKPERSISTR_POSTGRES");
if (string.IsNullOrWhiteSpace(postgresConnectionString))
{
    // PostgreSQL needs a server at runtime. Constructing this delegate verifies
    // its public API and transitive compile assets when no server was supplied.
    Func<string, PostgreSqlPersistenceScope<SmokeDbContext>> createScope =
        connectionString => new PostgreSqlPersistenceScope<SmokeDbContext>(
            connectionString,
            options => new SmokeDbContext(options));
    _ = createScope;
    Console.WriteLine("Packaged PostgreSQL compile smoke test passed (runtime skipped).");
}
else
{
    ExercisePostgreSql(postgresConnectionString);
    Console.WriteLine("Packaged PostgreSQL runtime smoke test passed.");
}

static void ExercisePostgreSql(string serverConnectionString)
{
    string generatedDatabaseName;
    using (var scope = new PostgreSqlPersistenceScope<SmokeDbContext>(
        serverConnectionString,
        options => new SmokeDbContext(options)))
    {
        generatedDatabaseName = scope.DatabaseName;
        var added = scope.Add(new SmokeEntity { Name = "From the PostgreSQL package" });
        scope.Commit();

        var firstContext = scope.Reader.Query(context => context);
        scope.StartNewSession();
        var secondContext = scope.Reader.Query(context => context);
        if (ReferenceEquals(firstContext, secondContext))
            throw new InvalidOperationException("PostgreSQL StartNewSession reused its DbContext.");

        var reloaded = scope.GetById<SmokeEntity>(added.Id);
        if (reloaded.Name != added.Name)
            throw new InvalidOperationException("The packaged PostgreSQL scope did not persist the entity.");

        using var concurrent = scope.OpenConcurrentSession();
        var concurrentContext = concurrent.Reader.Query(context => context);
        if (ReferenceEquals(secondContext, concurrentContext))
            throw new InvalidOperationException("The PostgreSQL concurrent session reused its DbContext.");
        _ = concurrent.GetById<SmokeEntity>(added.Id);
    }

    var adminConnectionString = new NpgsqlConnectionStringBuilder(serverConnectionString);
    if (string.IsNullOrWhiteSpace(adminConnectionString.Database))
        adminConnectionString.Database = "postgres";

    using var connection = new NpgsqlConnection(adminConnectionString.ConnectionString);
    connection.Open();
    using var command = connection.CreateCommand();
    command.CommandText =
        "SELECT EXISTS (SELECT FROM pg_database WHERE datname = @database_name)";
    command.Parameters.AddWithValue("database_name", generatedDatabaseName);
    if (command.ExecuteScalar() is true)
        throw new InvalidOperationException("The PostgreSQL scope did not drop its generated database.");
}

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
