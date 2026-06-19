using QuickFuzzr;
using QuickPersistr.UnderTheHood;

namespace QuickPersistr;

public interface IPersistence<TReader>
{
    public IPersistenceSpecification<TReader> Define();
}

public abstract class Persistence<TReader, TEntity> : IPersistence<TReader>
where TEntity : class, new()
{
    public abstract IPersistenceSpecification<TReader> Define();
    protected PersistencePrimaryKey<TReader, TEntity> Entity => new();
}
