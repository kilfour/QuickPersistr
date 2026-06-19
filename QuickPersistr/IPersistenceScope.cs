namespace QuickPersistr;

public interface IPersistenceScope
{
    public TContext GetContext<TContext>() where TContext : class;
    public TEntity Query<TContext, TEntity>(Func<TContext, TEntity> query)
        where TContext : class
        where TEntity : class, new();
    public TEntity GetById<TEntity>(object? id) where TEntity : class, new();
    public TEntity Add<TEntity>(TEntity entity);
    public void DeleteById<TEntity>(object? id)
    where TEntity : class;
    public void Commit();
}
