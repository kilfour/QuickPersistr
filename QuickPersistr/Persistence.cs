using QuickCheckr;
using QuickPersistr.UnderTheHood;

namespace QuickPersistr;

public interface IPersistence
{
    public IPersistenceSpecification Define();
}
public abstract class Persistence<TEntity> : IPersistence
where TEntity : class, new()
{
    public abstract IPersistenceSpecification Define();
    protected PersistencePrimaryKey<TEntity> Entity => new();

}
