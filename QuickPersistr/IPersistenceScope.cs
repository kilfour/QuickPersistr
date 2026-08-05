namespace QuickPersistr;

public interface IPersistenceScope
{
    public TEntity GetById<TEntity>(object? id) where TEntity : class;
    public TEntity Add<TEntity>(TEntity entity);
    public void DeleteById<TEntity>(object? id)
    where TEntity : class;
    public void Commit();
    public void StartNewSession();
}

public interface IPersistenceScope<TReader> : IPersistenceScope
{
    IPersistenceReader<TReader> Reader { get; }
}
