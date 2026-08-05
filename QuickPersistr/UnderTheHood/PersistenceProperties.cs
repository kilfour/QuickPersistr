using System.Linq.Expressions;
using QuickCheckr;
using QuickPersistr.UnderTheHood.Many;

namespace QuickPersistr.UnderTheHood;

public class PersistenceProperties<TReader, TEntity, TId>(
    IdentitySelector<TEntity, TId> identitySelector)
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
    private readonly List<AfterDeleteCheck<TReader, TEntity>> afterDeleteChecks = [];

    public PersistenceProperties<TReader, TEntity, TId> HasMany(
        Func<HasManyFrom<TEntity, TReader, TId>, Func<IPersistenceScope<TReader>, PoolElement<TEntity>, CheckrOf<Case>>> many)
    {
        oneToManies.Add(many(new HasManyFrom<TEntity, TReader, TId>(identitySelector)));
        return this;
    }

    public PersistenceProperties<TReader, TEntity, TId> AfterDelete(
        string description,
        Func<IPersistenceReader<TReader>, TEntity, bool> check)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(check);

        afterDeleteChecks.Add(new(description, check));
        return this;
    }

    public PersistenceSpecification<TReader, TEntity, TId> Persist()
        => new(identitySelector, propertyChecks, oneToManies, afterDeleteChecks);
}
