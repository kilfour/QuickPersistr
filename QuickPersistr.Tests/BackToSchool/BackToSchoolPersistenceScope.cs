using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuickPersistr.Tests.BackToSchool.Model;

namespace QuickPersistr.Tests.BackToSchool;

public class BackToSchoolPersistenceScope : IDisposable, IPersistenceScope
{
    private readonly SqliteConnection connection;
    private readonly BackToSchoolDbContext context;

    public BackToSchoolPersistenceScope()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<BackToSchoolDbContext>()
            .UseSqlite(connection)
            .Options;
        context = new BackToSchoolDbContext(options);
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
}
