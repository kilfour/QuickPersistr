using System.Linq.Expressions;
using System.Reflection;
using QuickCheckr;
using QuickPersistr.UnderTheHood.Many;

namespace QuickPersistr.UnderTheHood;

public record PropertyCheck<TEntity>(string Name, Func<TEntity, object?> GetValue)
{
    public Func<TEntity, TEntity, bool> Check = (a, b) => Equals(GetValue(a), GetValue(b));
}

public class PersistenceProperties<TReader, TEntity, TId>(PropertyInfo primaryKeyPropertyInfo)
where TEntity : class, new()
{
    private readonly List<PropertyCheck<TEntity>> propertyChecks = [];
    public PersistenceProperties<TReader, TEntity, TId> Property<TProp>(Expression<Func<TEntity, TProp>> propertyExpression)
    {
        var propertyInfo = propertyExpression.AsPropertyInfo();
        propertyChecks.Add(new(propertyInfo.Name, propertyInfo.GetValue));
        return this;
    }

    private readonly List<Func<IPersistenceScope<TReader>, PoolElement<TEntity>, CheckrOf<Case>>> oneToManies = [];

    public PersistenceProperties<TReader, TEntity, TId> HasMany(
        Func<HasManyFrom<TEntity, TReader, TId>, Func<IPersistenceScope<TReader>, PoolElement<TEntity>, CheckrOf<Case>>> many)
    {
        oneToManies.Add(many(new HasManyFrom<TEntity, TReader, TId>(primaryKeyPropertyInfo)));
        return this;
    }

    public PersistenceSpecification<TReader, TEntity> Persist()
        => new(primaryKeyPropertyInfo, propertyChecks, oneToManies);
}
