namespace QuickPersistr;

public interface IPersistenceScope
{
    public TEntity GetById<TEntity>(object? id) where TEntity : class;
    public TEntity GetById<TEntity>(object?[] identity)
    where TEntity : class =>
        identity.Length == 1
            ? GetById<TEntity>(identity[0])
            : throw new NotSupportedException(
                $"{GetType().Name} does not support composite identities.");

    public TEntity Add<TEntity>(TEntity entity);
    public void DeleteById<TEntity>(object? id)
    where TEntity : class;

    public void DeleteById<TEntity>(object?[] identity)
    where TEntity : class
    {
        if (identity.Length == 1)
        {
            DeleteById<TEntity>(identity[0]);
            return;
        }

        throw new NotSupportedException(
            $"{GetType().Name} does not support composite identities.");
    }

    public void Commit();
    public void StartNewSession();
}

public interface IPersistenceScope<TReader> : IPersistenceScope
{
    IPersistenceReader<TReader> Reader { get; }
}
