using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace QuickPersistr.Tests;

public class EfPersistenceScope<TDbContext> : IDisposable, IPersistenceScope<TDbContext>
where TDbContext : DbContext
{
    private readonly SqliteConnection connection;
    private readonly TDbContext context;

    public EfPersistenceScope(Func<DbContextOptions<TDbContext>, TDbContext> contextFactory)
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<TDbContext>()
            .UseSqlite(connection)
            .Options;
        context = contextFactory(options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        context.Dispose();
        connection.Dispose();
    }

    public IPersistenceReader<TDbContext> Reader => new EfReader<TDbContext>(context);

    public TEntity GetById<TEntity>(object? id)
    where TEntity : class, new()
        => context.Find<TEntity>(id)!;

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

    public void Commit()
    {
        context.SaveChanges();
        context.ChangeTracker.Clear();
    }

    public EfReader<TDbContext> GetReader() => new(context);
}
