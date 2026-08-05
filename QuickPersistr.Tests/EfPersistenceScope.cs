using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace QuickPersistr.Tests;

public class EfPersistenceScope<TDbContext> : IDisposable, IPersistenceScope<TDbContext>
where TDbContext : DbContext
{
    private readonly SqliteConnection connection;
    private readonly DbContextOptions<TDbContext> options;
    private readonly Func<DbContextOptions<TDbContext>, TDbContext> contextFactory;
    private TDbContext context;

    public EfPersistenceScope(
        Func<DbContextOptions<TDbContext>, TDbContext> contextFactory,
        bool enforceForeignKeys = true)
    {
        this.contextFactory = contextFactory;
        connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = ":memory:",
            ForeignKeys = enforceForeignKeys
        }.ToString());
        connection.Open();
        options = new DbContextOptionsBuilder<TDbContext>()
            .UseSqlite(connection)
            .Options;
        context = CreateContext();
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        context.Dispose();
        connection.Dispose();
    }

    public IPersistenceReader<TDbContext> Reader => new EfReader<TDbContext>(context);

    public TEntity GetById<TEntity>(object? id)
    where TEntity : class
        => context.Find<TEntity>(id)!;

    public TEntity GetById<TEntity>(object?[] identity)
    where TEntity : class
        => context.Find<TEntity>(identity)!;

    public TEntity Add<TEntity>(TEntity entity)
    {
        context.Add(entity!);
        return entity;
    }

    public void DeleteById<TEntity>(object? id)
    where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(id);
        var entity = context.Set<TEntity>().Find(id);
        if (entity is null)
            return;
        context.Set<TEntity>().Remove(entity);
    }

    public void DeleteById<TEntity>(object?[] identity)
    where TEntity : class
    {
        var entity = context.Find<TEntity>(identity);
        if (entity is null)
            return;
        context.Set<TEntity>().Remove(entity);
    }

    public void Commit()
        => context.SaveChanges();

    public void StartNewSession()
    {
        context.Dispose();
        context = CreateContext();
    }

    public EfReader<TDbContext> GetReader() => new(context);

    private TDbContext CreateContext() => contextFactory(options);
}
