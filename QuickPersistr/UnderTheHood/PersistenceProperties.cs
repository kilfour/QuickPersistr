using System.Linq.Expressions;
using QuickCheckr;
using QuickPersistr.UnderTheHood.Many;
using QuickPersistr.UnderTheHood.One;

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

    private readonly List<Func<IPersistenceScope<TReader>, PoolElement<TEntity>, CheckrOf<Case>>> oneToOnes = [];
    private readonly List<Func<IPersistenceScope<TReader>, PoolElement<TEntity>, CheckrOf<Case>>> oneToManies = [];
    private readonly List<AfterDeleteCheck<TReader, TEntity>> afterDeleteChecks = [];
    private readonly List<RejectedOperation<TEntity>> rejectedCreates = [];
    private readonly List<RejectedOperation<TEntity>> rejectedUpdates = [];
    private readonly List<RejectedOperation<TEntity>> rejectedDeletes = [];
    private readonly List<DomainMutation<TEntity>> domainUpdates = [];
    private readonly List<OptimisticConcurrency<TEntity>> concurrencyScenarios = [];
    private readonly List<Shrinker> entityShrinkers = [];

    public PersistenceProperties<TReader, TEntity, TId> Shrinking(
        params Shrinker[] shrinkers)
    {
        ArgumentNullException.ThrowIfNull(shrinkers);
        if (shrinkers.Any(shrinker => shrinker is null))
        {
            throw new ArgumentException(
                "Entity shrinkers cannot contain null.",
                nameof(shrinkers));
        }

        entityShrinkers.AddRange(shrinkers);
        return this;
    }

    public PersistenceProperties<TReader, TEntity, TId> Update(
        Expression<Action<TEntity>> mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        return Update(MutationName(mutation, nameof(mutation)), mutation.Compile());
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

    public PersistenceProperties<TReader, TEntity, TId> OptimisticConcurrency<TException>(
        Expression<Action<TEntity>> winningUpdate,
        Expression<Action<TEntity>> conflictingUpdate)
    where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(winningUpdate);
        ArgumentNullException.ThrowIfNull(conflictingUpdate);

        var winningName = MutationName(winningUpdate, nameof(winningUpdate));
        var conflictingName = MutationName(conflictingUpdate, nameof(conflictingUpdate));
        var description = winningName == conflictingName
            ? winningName
            : $"{winningName} / {conflictingName}";

        return OptimisticConcurrency<TException>(
            description,
            winningUpdate.Compile(),
            conflictingUpdate.Compile());
    }

    public PersistenceProperties<TReader, TEntity, TId> OptimisticConcurrency<TException>(
        string description,
        Action<TEntity> winningUpdate,
        Action<TEntity> conflictingUpdate)
    where TException : Exception
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(winningUpdate);
        ArgumentNullException.ThrowIfNull(conflictingUpdate);

        concurrencyScenarios.Add(new(
            description,
            winningUpdate,
            conflictingUpdate,
            (label, result) => Checkr.ExpectThrewExactly<TException>(label, result)));
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
        oneToManies.Add(many(new HasManyFrom<TEntity, TReader, TId>(
            identitySelector,
            entityShrinkers,
            $"{typeof(TEntity).Name}.HasMany[{oneToManies.Count}]")));
        return this;
    }

    public PersistenceProperties<TReader, TEntity, TId> HasOne(
        Func<HasOneFrom<TEntity, TReader, TId>, Func<IPersistenceScope<TReader>, PoolElement<TEntity>, CheckrOf<Case>>> one)
    {
        oneToOnes.Add(one(new HasOneFrom<TEntity, TReader, TId>(
            identitySelector,
            entityShrinkers,
            $"{typeof(TEntity).Name}.HasOne[{oneToOnes.Count}]")));
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
            oneToOnes,
            oneToManies,
            afterDeleteChecks,
            rejectedCreates,
            rejectedUpdates,
            rejectedDeletes,
            domainUpdates,
            concurrencyScenarios,
            [.. entityShrinkers]);

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

    private static string MutationName(
        Expression<Action<TEntity>> mutation,
        string parameterName)
    {
        if (mutation.Body is MethodCallExpression methodCall)
            return methodCall.Method.Name;

        throw new ArgumentException(
            "An inferred mutation must be a domain method call.",
            parameterName);
    }
}
