using System.Linq.Expressions;

namespace QuickPersistr.UnderTheHood;

public class PersistencePrimaryKey<TEntity>
where TEntity : class, new()
{
    public PersistenceProperties<TEntity, TId> PrimaryKey<TId>(Expression<Func<TEntity, TId>> primaryKeyExpression)
        => new(primaryKeyExpression);

}
