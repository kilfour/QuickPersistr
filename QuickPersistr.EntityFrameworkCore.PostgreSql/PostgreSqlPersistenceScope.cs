using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace QuickPersistr.EntityFrameworkCore.PostgreSql;

public class PostgreSqlPersistenceScope<TDbContext>
    : EfPersistenceScope<TDbContext>
where TDbContext : DbContext
{
    public PostgreSqlPersistenceScope(
        string serverConnectionString,
        Func<DbContextOptions<TDbContext>, TDbContext> contextFactory,
        Action<DbContextOptionsBuilder<TDbContext>>? configureOptions = null,
        string databaseNamePrefix = "quickpersistr_")
        : this(new PostgreSqlDatabase<TDbContext>(
            serverConnectionString,
            contextFactory,
            configureOptions,
            databaseNamePrefix))
    { }

    private PostgreSqlPersistenceScope(PostgreSqlDatabase<TDbContext> database)
        : base(database.CreateContext, ensureCreated: true, database)
    {
        DatabaseName = database.DatabaseName;
    }

    public string DatabaseName { get; }

    private sealed class PostgreSqlDatabase<TContext> : IDisposable
    where TContext : DbContext
    {
        private readonly string adminConnectionString;
        private readonly Func<DbContextOptions<TContext>, TContext> contextFactory;
        private readonly DbContextOptions<TContext> options;
        private bool disposed;

        public PostgreSqlDatabase(
            string serverConnectionString,
            Func<DbContextOptions<TContext>, TContext> contextFactory,
            Action<DbContextOptionsBuilder<TContext>>? configureOptions,
            string databaseNamePrefix)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(serverConnectionString);
            ArgumentNullException.ThrowIfNull(contextFactory);
            ValidateDatabaseNamePrefix(databaseNamePrefix);

            this.contextFactory = contextFactory;
            var adminConnection = new NpgsqlConnectionStringBuilder(serverConnectionString);
            if (string.IsNullOrWhiteSpace(adminConnection.Database))
                adminConnection.Database = "postgres";
            adminConnectionString = adminConnection.ConnectionString;

            DatabaseName = databaseNamePrefix + Guid.NewGuid().ToString("N");
            CreateDatabase();

            try
            {
                var databaseConnection = new NpgsqlConnectionStringBuilder(serverConnectionString)
                {
                    Database = DatabaseName,
                    Pooling = false
                };
                var optionsBuilder = new DbContextOptionsBuilder<TContext>()
                    .UseNpgsql(databaseConnection.ConnectionString);
                configureOptions?.Invoke(optionsBuilder);
                options = optionsBuilder.Options;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public string DatabaseName { get; }

        public TContext CreateContext() => contextFactory(options);

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            using var connection = new NpgsqlConnection(adminConnectionString);
            connection.Open();

            using (var terminate = connection.CreateCommand())
            {
                terminate.CommandText = """
                    SELECT pg_terminate_backend(pid)
                    FROM pg_stat_activity
                    WHERE datname = @database_name
                      AND pid <> pg_backend_pid()
                    """;
                terminate.Parameters.AddWithValue("database_name", DatabaseName);
                terminate.ExecuteNonQuery();
            }

            using var drop = connection.CreateCommand();
            drop.CommandText = $"DROP DATABASE IF EXISTS \"{DatabaseName}\"";
            drop.ExecuteNonQuery();
        }

        private void CreateDatabase()
        {
            using var connection = new NpgsqlConnection(adminConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE \"{DatabaseName}\"";
            command.ExecuteNonQuery();
        }

        private static void ValidateDatabaseNamePrefix(string prefix)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
            if (prefix.Length > 30 || prefix.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character != '_'))
            {
                throw new ArgumentException(
                    "The database name prefix must contain at most 30 ASCII letters, digits, or underscores.",
                    nameof(prefix));
            }
        }
    }
}
