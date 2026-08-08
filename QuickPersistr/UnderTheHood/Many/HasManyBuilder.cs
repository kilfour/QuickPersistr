using QuickCheckr;

namespace QuickPersistr.UnderTheHood.Many;

public class HasManyFrom<TEntity, TReader, TId>(
    IdentitySelector<TEntity, TId> identitySelector,
    IReadOnlyList<Shrinker> entityShrinkers,
    string relationshipKey)
where TEntity : class
{
    public HasManyAdd<TEntity, TReader, TChild, TId> From<TChild>(
        Persistence<TReader, TChild> childPersistence)
    where TChild : class =>
        new(new(
            identitySelector,
            childPersistence.Define(),
            entityShrinkers,
            relationshipKey));
}

public class HasManyAdd<TEntity, TReader, TChild, TId>(
    HasManyDefinition<TEntity, TReader, TChild, TId> definition)
where TChild : class
where TEntity : class
{
    public HasManyRemove<TEntity, TReader, TChild, TId> Add(Action<TEntity, TChild> add) =>
        new(definition with { Add = add });
}

public class HasManyRemove<TEntity, TReader, TChild, TId>(
    HasManyDefinition<TEntity, TReader, TChild, TId> definition)
where TChild : class
where TEntity : class
{
    public HasManyClear<TEntity, TReader, TChild, TId> Remove(Action<TEntity, TChild> remove) =>
        new(definition with { Remove = remove });

    public HasManyAdditiveContains<TEntity, TReader, TChild, TId> Reload(
        Func<IPersistenceReader<TReader>, TId, TEntity> reload) =>
        new(definition with { Reload = reload });
}

public class HasManyAdditiveContains<TEntity, TReader, TChild, TId>(
    HasManyDefinition<TEntity, TReader, TChild, TId> definition)
where TChild : class
where TEntity : class
{
    public Func<IPersistenceScope<TReader>, PoolElement<TEntity>, CheckrOf<Case>> Contains(
        Func<TEntity, TChild, bool> contains)
    {
        var completed = definition with { Contains = contains };
        return (scope, element) =>
            new HasManyScenario<TEntity, TReader, TChild, TId>(completed, scope)
                .CheckAdditive(element);
    }
}

public class HasManyClear<TEntity, TReader, TChild, TId>(
    HasManyDefinition<TEntity, TReader, TChild, TId> definition)
where TChild : class
where TEntity : class
{
    public HasManyReload<TEntity, TReader, TChild, TId> Clear(Action<TEntity> clear) =>
        new(definition with { Clear = clear });

    public HasManyReassigned<TEntity, TReader, TChild, TId> Reassign(
        Action<TEntity, TEntity, TChild> reassign) =>
        new(definition with { Reassign = reassign });
}

public class HasManyReassigned<TEntity, TReader, TChild, TId>(
    HasManyDefinition<TEntity, TReader, TChild, TId> definition)
where TChild : class
where TEntity : class
{
    public HasManyReload<TEntity, TReader, TChild, TId> Clear(Action<TEntity> clear) =>
        new(definition with { Clear = clear });
}

public class HasManyReload<TEntity, TReader, TChild, TId>(
    HasManyDefinition<TEntity, TReader, TChild, TId> definition)
where TChild : class
where TEntity : class
{
    public HasManyContains<TEntity, TReader, TChild, TId> Reload(
        Func<IPersistenceReader<TReader>, TId, TEntity> reload) =>
        new(definition with { Reload = reload });
}

public class HasManyContains<TEntity, TReader, TChild, TId>(
    HasManyDefinition<TEntity, TReader, TChild, TId> definition)
where TChild : class
where TEntity : class
{
    public HasManyEmpty<TEntity, TReader, TChild, TId> Contains(
        Func<TEntity, TChild, bool> contains) =>
        new(definition with { Contains = contains });
}

public class HasManyEmpty<TEntity, TReader, TChild, TId>(
    HasManyDefinition<TEntity, TReader, TChild, TId> definition)
where TChild : class
where TEntity : class
{
    public Func<IPersistenceScope<TReader>, PoolElement<TEntity>, CheckrOf<Case>> Empty(
        Func<TEntity, bool> empty)
    {
        var completed = definition with { Empty = empty };
        return (scope, element) =>
            new HasManyScenario<TEntity, TReader, TChild, TId>(completed, scope)
                .Check(element);
    }
}
