using System.Linq.Expressions;

namespace QuickPersistr.UnderTheHood;

public class PersistencePrimaryKey<TReader, TEntity>
where TEntity : class, new()
{
    public PersistenceProperties<TReader, TEntity, TId> PrimaryKey<TId>(Expression<Func<TEntity, TId>> primaryKeyExpression)
        => new(primaryKeyExpression.AsPropertyInfo());

}
