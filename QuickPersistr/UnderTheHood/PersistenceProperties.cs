using System.Linq.Expressions;
using System.Reflection;
using QuickCheckr;
using QuickPersistr.UnderTheHood.Many;

namespace QuickPersistr.UnderTheHood;

public class PersistenceProperties<TReader, TEntity, TId>(PropertyInfo primaryKeyPropertyInfo)
where TEntity : class
{
    private readonly List<PropertyCheck<TEntity>> propertyChecks = [];
    public PersistenceProperties<TReader, TEntity, TId> Property<TProp>(Expression<Func<TEntity, TProp>> propertyExpression)
        => Property(propertyExpression, EqualityComparer<TProp>.Default.Equals);

    public PersistenceProperties<TReader, TEntity, TId> Property<TProp>(
        Expression<Func<TEntity, TProp>> propertyExpression,
        Func<TProp, TProp, bool> equals)
    {
        ArgumentNullException.ThrowIfNull(propertyExpression);
        ArgumentNullException.ThrowIfNull(equals);

        var propertyInfo = propertyExpression.AsPropertyInfo();
        var getValue = propertyExpression.Compile();
        propertyChecks.Add(new(
            propertyInfo.Name,
            entity => getValue(entity),
            (expected, actual) => equals(getValue(expected), getValue(actual))));
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
