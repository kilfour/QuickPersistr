using QuickPersistr.UnderTheHood;

namespace QuickPersistr;

public abstract class Persistence<TEntity>
where TEntity : class, new()
{
    public abstract IPersistenceSpecification Define();
    protected PersistencePrimaryKey<TEntity> Entity => new();

}
