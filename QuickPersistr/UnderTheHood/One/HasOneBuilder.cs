using QuickCheckr;
using QuickFuzzr;

namespace QuickPersistr.UnderTheHood.One;

public class HasOneFrom<TEntity, TReader, TId>(
    IdentitySelector<TEntity, TId> identitySelector,
    IReadOnlyList<Shrinker> entityShrinkers)
where TEntity : class
{
    public HasOneSet<TEntity, TReader, TChild, TId> From<TChild>(
        Persistence<TReader, TChild> childPersistence)
    where TChild : class =>
        new(new(
            identitySelector,
            childPersistence.Define().GetCreator<TChild>(),
            entityShrinkers));
}

public class HasOneSet<TEntity, TReader, TChild, TId>(
    HasOneDefinition<TEntity, TReader, TChild, TId> definition)
where TEntity : class
where TChild : class
{
    public HasOneClear<TEntity, TReader, TChild, TId> Set(
        Action<TEntity, TChild> set) =>
        new(definition with { Set = set });
}

public class HasOneClear<TEntity, TReader, TChild, TId>(
    HasOneDefinition<TEntity, TReader, TChild, TId> definition)
where TEntity : class
where TChild : class
{
    public HasOneReload<TEntity, TReader, TChild, TId> Clear(
        Action<TEntity> clear) =>
        new(definition with { Clear = clear });

    public HasOneReassigned<TEntity, TReader, TChild, TId> Reassign(
        Action<TEntity, TEntity, TChild> reassign) =>
        new(definition with { Reassign = reassign });

    public HasOneAdditiveContains<TEntity, TReader, TChild, TId> Reload(
        Func<IPersistenceReader<TReader>, TId, TEntity> reload) =>
        new(definition with { Reload = reload });
}

public class HasOneReassigned<TEntity, TReader, TChild, TId>(
    HasOneDefinition<TEntity, TReader, TChild, TId> definition)
where TEntity : class
where TChild : class
{
    public HasOneReload<TEntity, TReader, TChild, TId> Clear(
        Action<TEntity> clear) =>
        new(definition with { Clear = clear });
}

public class HasOneReload<TEntity, TReader, TChild, TId>(
    HasOneDefinition<TEntity, TReader, TChild, TId> definition)
where TEntity : class
where TChild : class
{
    public HasOneContains<TEntity, TReader, TChild, TId> Reload(
        Func<IPersistenceReader<TReader>, TId, TEntity> reload) =>
        new(definition with { Reload = reload });
}

public class HasOneAdditiveContains<TEntity, TReader, TChild, TId>(
    HasOneDefinition<TEntity, TReader, TChild, TId> definition)
where TEntity : class
where TChild : class
{
    public Func<IPersistenceScope<TReader>, PoolElement<TEntity>, CheckrOf<Case>> Contains(
        Func<TEntity, TChild, bool> contains)
    {
        var completed = definition with { Contains = contains };
        return (scope, element) =>
            new HasOneScenario<TEntity, TReader, TChild, TId>(completed, scope)
                .CheckAdditive(element);
    }
}

public class HasOneContains<TEntity, TReader, TChild, TId>(
    HasOneDefinition<TEntity, TReader, TChild, TId> definition)
where TEntity : class
where TChild : class
{
    public HasOneEmpty<TEntity, TReader, TChild, TId> Contains(
        Func<TEntity, TChild, bool> contains) =>
        new(definition with { Contains = contains });
}

public class HasOneEmpty<TEntity, TReader, TChild, TId>(
    HasOneDefinition<TEntity, TReader, TChild, TId> definition)
where TEntity : class
where TChild : class
{
    public Func<IPersistenceScope<TReader>, PoolElement<TEntity>, CheckrOf<Case>> Empty(
        Func<TEntity, bool> empty)
    {
        var completed = definition with { Empty = empty };
        return (scope, element) =>
            new HasOneScenario<TEntity, TReader, TChild, TId>(completed, scope)
                .Check(element);
    }
}
