using Microsoft.EntityFrameworkCore;

namespace QuickPersistr.EntityFrameworkCore;

public class EfPersistenceScope<TDbContext> : IConcurrentPersistenceScope<TDbContext>
where TDbContext : DbContext
{
    private readonly Func<TDbContext> contextFactory;
    private readonly IDisposable? ownedResource;
    private TDbContext context = null!;
    private bool disposed;

    public EfPersistenceScope(
        Func<TDbContext> contextFactory,
        bool ensureCreated = true)
        : this(contextFactory, ensureCreated, null) { }

    protected EfPersistenceScope(
        Func<TDbContext> contextFactory,
        bool ensureCreated,
        IDisposable? ownedResource)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        this.contextFactory = contextFactory;
        this.ownedResource = ownedResource;

        try
        {
            context = CreateContext();
            if (ensureCreated)
                context.Database.EnsureCreated();
        }
        catch
        {
            context?.Dispose();
            ownedResource?.Dispose();
            throw;
        }
    }

    public IPersistenceReader<TDbContext> Reader
    {
        get
        {
            ThrowIfDisposed();
            return new EfPersistenceReader<TDbContext>(context);
        }
    }

    public TEntity GetById<TEntity>(object? id)
    where TEntity : class
    {
        ThrowIfDisposed();
        return context.Find<TEntity>(id)!;
    }

    public TEntity GetById<TEntity>(object?[] identity)
    where TEntity : class
    {
        ThrowIfDisposed();
        return context.Find<TEntity>(identity)!;
    }

    public TEntity Add<TEntity>(TEntity entity)
    {
        ThrowIfDisposed();
        context.Add(entity!);
        return entity;
    }

    public void DeleteById<TEntity>(object? id)
    where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(id);
        ThrowIfDisposed();
        Delete(context.Find<TEntity>(id));
    }

    public void DeleteById<TEntity>(object?[] identity)
    where TEntity : class
    {
        ThrowIfDisposed();
        Delete(context.Find<TEntity>(identity));
    }

    public void Commit()
    {
        ThrowIfDisposed();
        context.SaveChanges();
    }

    public void StartNewSession()
    {
        ThrowIfDisposed();
        context.Dispose();
        context = CreateContext();
    }

    public IConcurrentPersistenceScope<TDbContext> OpenConcurrentSession()
    {
        ThrowIfDisposed();
        return new EfPersistenceScope<TDbContext>(contextFactory, ensureCreated: false);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        try
        {
            context.Dispose();
        }
        finally
        {
            ownedResource?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    private TDbContext CreateContext() => contextFactory();

    private void Delete<TEntity>(TEntity? entity)
    where TEntity : class
    {
        if (entity is not null)
            context.Set<TEntity>().Remove(entity);
    }

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(disposed, this);
}
