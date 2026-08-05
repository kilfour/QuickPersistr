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
            (expected, actual) => equals((TProp)expected!, (TProp)actual!)));
        return this;
    }

    private readonly List<Func<IPersistenceScope<TReader>, PoolElement<TEntity>, CheckrOf<Case>>> oneToManies = [];
    private readonly List<AfterDeleteCheck<TReader, TEntity>> afterDeleteChecks = [];
    private readonly List<RejectedOperation<TEntity>> rejectedCreates = [];
    private readonly List<RejectedOperation<TEntity>> rejectedUpdates = [];
    private readonly List<RejectedOperation<TEntity>> rejectedDeletes = [];
    private readonly List<DomainMutation<TEntity>> domainUpdates = [];

    public PersistenceProperties<TReader, TEntity, TId> Update(
        Expression<Action<TEntity>> mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        if (mutation.Body is not MethodCallExpression methodCall)
        {
            throw new ArgumentException(
                "An inferred update must be a domain method call.",
                nameof(mutation));
        }

        return Update(methodCall.Method.Name, mutation.Compile());
    }

    public PersistenceProperties<TReader, TEntity, TId> Update(
        string description,
        Action<TEntity> mutation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(mutation);

        domainUpdates.Add(new(description, mutation));
        return this;
    }

    public PersistenceProperties<TReader, TEntity, TId> CreateRejected<TException>(
        string description,
        Action<TEntity> attempt)
    where TException : Exception =>
        AddRejected<TException>(rejectedCreates, description, attempt);

    public PersistenceProperties<TReader, TEntity, TId> CreateRejected<TException>(
        string description)
    where TException : Exception =>
        CreateRejected<TException>(description, _ => { });

    public PersistenceProperties<TReader, TEntity, TId> UpdateRejected<TException>(
        string description,
        Action<TEntity> attempt)
    where TException : Exception =>
        AddRejected<TException>(rejectedUpdates, description, attempt);

    public PersistenceProperties<TReader, TEntity, TId> UpdateRejected<TException>(
        string description)
    where TException : Exception =>
        UpdateRejected<TException>(description, _ => { });

    public PersistenceProperties<TReader, TEntity, TId> DeleteRejected<TException>(
        string description,
        Action<TEntity> attempt)
    where TException : Exception =>
        AddRejected<TException>(rejectedDeletes, description, attempt);

    public PersistenceProperties<TReader, TEntity, TId> DeleteRejected<TException>(
        string description)
    where TException : Exception =>
        DeleteRejected<TException>(description, _ => { });

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
        => new(
            identitySelector,
            propertyChecks,
            oneToManies,
            afterDeleteChecks,
            rejectedCreates,
            rejectedUpdates,
            rejectedDeletes,
            domainUpdates);

    private PersistenceProperties<TReader, TEntity, TId> AddRejected<TException>(
        List<RejectedOperation<TEntity>> operations,
        string description,
        Action<TEntity> attempt)
    where TException : Exception
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(attempt);

        operations.Add(new(
            description,
            attempt,
            (label, result) => Checkr.ExpectThrewExactly<TException>(label, result)));
        return this;
    }
}
