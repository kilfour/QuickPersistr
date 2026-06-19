using System.Linq.Expressions;
using System.Reflection;
using QuickCheckr;
using QuickFuzzr;

namespace QuickPersistr.UnderTheHood;

public class PersistenceProperties<TEntity, TId>(PropertyInfo primaryKeyPropertyInfo)
where TEntity : class, new()
{
    private readonly List<Func<TEntity, TEntity, bool>> propertyChecks = [];
    public PersistenceProperties<TEntity, TId> Property<TProp>(Expression<Func<TEntity, TProp>> propertyExpression)
    {
        var propertyInfo = propertyExpression.AsPropertyInfo();
        propertyChecks.Add((a, b) => Equals(propertyInfo.GetValue(a), propertyInfo.GetValue(b)));
        return this;
    }

    private readonly List<Action<TEntity>> oneToManies = [];
    public PersistenceProperties<TEntity, TId> HasMany<TChild>(
        Persistence<TChild> childSpecification,
        Action<TEntity, TChild> apply)
    where TChild : class, new()
    {
        //void action(TEntity a) => apply(a, childSpecification.Define().GetCreator<TChild>().Generate());
        //oneToManies.Add(action);
        return this;
    }

    public PersistenceSpecification<TEntity> Persist()
        => new(primaryKeyPropertyInfo, propertyChecks);
}
