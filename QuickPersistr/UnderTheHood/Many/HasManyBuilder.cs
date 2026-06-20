using System.Reflection;
using QuickCheckr;
using QuickFuzzr;

namespace QuickPersistr.UnderTheHood.Many;

public class HasManyFrom<TEntity, TReader, TId>(PropertyInfo primaryKeyPropertyInfo)
where TEntity : class, new()
{
    public HasManyAddOne<TEntity, TReader, TChild, TId> From<TChild>(Persistence<TReader, TChild> childPersistence)
    where TChild : class, new()
    => new(primaryKeyPropertyInfo, childPersistence.Define().GetCreator<TChild>());
}

public class HasManyAddOne<TEntity, TReader, TChild, TId>(
    PropertyInfo primaryKeyPropertyInfo,
    FuzzrOf<TChild> childFuzzr
)
where TChild : class, new()
where TEntity : class, new()
{
    public HasManyAdded<TEntity, TReader, TChild, TId> AddOne(Action<TEntity, TChild> apply)
        => new(primaryKeyPropertyInfo, childFuzzr, apply);
}

public class HasManyAdded<TEntity, TReader, TChild, TId>(
    PropertyInfo primaryKeyPropertyInfo,
    FuzzrOf<TChild> childFuzzr,
    Action<TEntity, TChild> apply
)
where TChild : class, new()
where TEntity : class, new()
{
    public HasManyReload<TEntity, TReader, TChild, TId> Added(Func<TEntity, TChild, bool> check)
        => new(primaryKeyPropertyInfo, childFuzzr, apply, check);
}

public class HasManyReload<TEntity, TReader, TChild, TId>(
    PropertyInfo primaryKeyPropertyInfo,
    FuzzrOf<TChild> childFuzzr,
    Action<TEntity, TChild> apply,
    Func<TEntity, TChild, bool> check
)
where TChild : class, new()
where TEntity : class, new()
{
    public HasManyClear<TEntity, TReader, TChild, TId> Reload(Func<IPersistenceReader<TReader>, TId, TEntity> reload)
        => new(primaryKeyPropertyInfo, childFuzzr, apply, check, reload);
}

public class HasManyClear<TEntity, TReader, TChild, TId>(
    PropertyInfo primaryKeyPropertyInfo,
    FuzzrOf<TChild> childFuzzr,
    Action<TEntity, TChild> apply,
    Func<TEntity, TChild, bool> check,
    Func<IPersistenceReader<TReader>, TId, TEntity> reload
)
where TChild : class, new()
where TEntity : class, new()
{
    public HasManyCleared<TEntity, TReader, TChild, TId> Clear(Action<TEntity> clear)
        => new(primaryKeyPropertyInfo, childFuzzr, apply, check, reload, clear);
}

public class HasManyCleared<TEntity, TReader, TChild, TId>(
    PropertyInfo primaryKeyPropertyInfo,
    FuzzrOf<TChild> childFuzzr,
    Action<TEntity, TChild> apply,
    Func<TEntity, TChild, bool> check,
    Func<IPersistenceReader<TReader>, TId, TEntity> reload,
    Action<TEntity> clear
)
where TChild : class, new()
where TEntity : class, new()
{
    public Func<IPersistenceScope<TReader>, PoolElement<TEntity>, CheckrOf<Case>> Cleared(Func<TEntity, bool> checkCleared)
    {
        return
            (scope, element) => GetHasManyCheckr(
                element, apply, check, reload, clear, checkCleared, childFuzzr, scope);
    }

    public CheckrOf<Case> GetHasManyCheckr(
        PoolElement<TEntity> info,
        Action<TEntity, TChild> apply,
        Func<TEntity, TChild, bool> check,
        Func<IPersistenceReader<TReader>, TId, TEntity> reload,
        Action<TEntity> clear,
        Func<TEntity, bool> checkCleared,
        FuzzrOf<TChild> childFuzzr,
        IPersistenceScope<TReader> scope)
    {
        var entityName = typeof(TEntity).Name;
        var childEntityName = typeof(TChild).Name;
        return
            from id in Checkr.Capture(() => (TId)primaryKeyPropertyInfo.GetValue(info.Value)!)
            from entity in Checkr.Capture(() => scope.GetById<TEntity>(id))
            from children in Checkr.Input("Children", childFuzzr.Many(1, 3))
            from updated in Checkr.Act("Update", () =>
            {
                foreach (var child in children)
                {
                    apply(entity, child);
                }
                scope.Commit();
            })
            from reloaded in Checkr.Capture(
                () => reload(scope.Reader, id))
            from canUpdate in Checkr.Expect($"{entityName} Can Add {childEntityName}", () => children.All(a => check(reloaded, a)))
            from clearMany in Checkr.Act("Clear Many", () => { clear(reloaded); scope.Commit(); })
            from reloadedCleared in Checkr.Capture(
                () => reload(scope.Reader, id))
            from cleared in Checkr.Expect($"{entityName} Can Clear {childEntityName}", () => checkCleared(reloadedCleared))
            from stored in info.Replace(reloaded)
            select Case.Closed;
    }
}