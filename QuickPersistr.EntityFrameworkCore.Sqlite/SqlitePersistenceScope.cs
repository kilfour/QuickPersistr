using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace QuickPersistr.EntityFrameworkCore.Sqlite;

public class SqlitePersistenceScope<TDbContext>
    : EfPersistenceScope<TDbContext>
where TDbContext : DbContext
{
    public SqlitePersistenceScope(
        Func<DbContextOptions<TDbContext>, TDbContext> contextFactory,
        bool enforceForeignKeys = true,
        Action<DbContextOptionsBuilder<TDbContext>>? configureOptions = null)
        : this(new SqliteDatabase<TDbContext>(
            contextFactory,
            enforceForeignKeys,
            configureOptions))
    { }

    private SqlitePersistenceScope(SqliteDatabase<TDbContext> database)
        : base(database.CreateContext, ensureCreated: true, database) { }

    private sealed class SqliteDatabase<TContext> : IDisposable
    where TContext : DbContext
    {
        private readonly SqliteConnection connection;
        private readonly Func<DbContextOptions<TContext>, TContext> contextFactory;
        private readonly DbContextOptions<TContext> options;

        public SqliteDatabase(
            Func<DbContextOptions<TContext>, TContext> contextFactory,
            bool enforceForeignKeys,
            Action<DbContextOptionsBuilder<TContext>>? configureOptions)
        {
            ArgumentNullException.ThrowIfNull(contextFactory);
            this.contextFactory = contextFactory;
            connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = ":memory:",
                ForeignKeys = enforceForeignKeys
            }.ToString());

            try
            {
                connection.Open();
                var optionsBuilder = new DbContextOptionsBuilder<TContext>()
                    .UseSqlite(connection);
                configureOptions?.Invoke(optionsBuilder);
                options = optionsBuilder.Options;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        public TContext CreateContext() => contextFactory(options);

        public void Dispose() => connection.Dispose();
    }
}
