using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace QuickPersistr.Tests;

public class EfPersistenceScope<TDbContext> : IDisposable, IPersistenceScope
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

    public TEntity GetById<TEntity>(object? id)
    where TEntity : class, new()
        => context.Find<TEntity>(id)!;


    public TEntity Query<TEntity>(object? id) where TEntity : class, new()
    {
        throw new NotImplementedException();
    }

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

    public TContext GetContext<TContext>() where TContext : class
        => (context as TContext)!;

    public TEntity Query<TContext, TEntity>(Func<TContext, TEntity> query)
     where TContext : class
     where TEntity : class, new()
        => query(GetContext<TContext>());
}
