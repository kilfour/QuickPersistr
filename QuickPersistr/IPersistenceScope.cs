namespace QuickPersistr;

public interface IPersistenceScope
{

    public TEntity GetById<TEntity>(object? id) where TEntity : class, new();
    public TEntity Add<TEntity>(TEntity entity);
    public void DeleteById<TEntity>(object? id)
    where TEntity : class;
    public void Commit();
}
