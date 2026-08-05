using System.Reflection;
using QuickCheckr;
using QuickFuzzr;

namespace QuickPersistr.UnderTheHood.Many;

public class HasManyFrom<TEntity, TReader, TId>(PropertyInfo primaryKeyPropertyInfo)
where TEntity : class
{
    public HasManyAdd<TEntity, TReader, TChild, TId> From<TChild>(Persistence<TReader, TChild> childPersistence)
    where TChild : class
    => new(primaryKeyPropertyInfo, childPersistence.Define().GetCreator<TChild>());
}

public class HasManyAdd<TEntity, TReader, TChild, TId>(
    PropertyInfo primaryKeyPropertyInfo,
    FuzzrOf<TChild> childFuzzr
)
where TChild : class
where TEntity : class
{
    public HasManyRemove<TEntity, TReader, TChild, TId> Add(Action<TEntity, TChild> add)
        => new(primaryKeyPropertyInfo, childFuzzr, add);
}

public class HasManyRemove<TEntity, TReader, TChild, TId>(
    PropertyInfo primaryKeyPropertyInfo,
    FuzzrOf<TChild> childFuzzr,
    Action<TEntity, TChild> add
)
where TChild : class
where TEntity : class
{
    public HasManyClear<TEntity, TReader, TChild, TId> Remove(Action<TEntity, TChild> remove)
        => new(primaryKeyPropertyInfo, childFuzzr, add, remove);
}

public class HasManyClear<TEntity, TReader, TChild, TId>(
    PropertyInfo primaryKeyPropertyInfo,
    FuzzrOf<TChild> childFuzzr,
    Action<TEntity, TChild> add,
    Action<TEntity, TChild> remove
)
where TChild : class
where TEntity : class
{
    public HasManyReload<TEntity, TReader, TChild, TId> Clear(Action<TEntity> clear)
        => new(primaryKeyPropertyInfo, childFuzzr, add, remove, clear);
}

public class HasManyReload<TEntity, TReader, TChild, TId>(
    PropertyInfo primaryKeyPropertyInfo,
    FuzzrOf<TChild> childFuzzr,
    Action<TEntity, TChild> add,
    Action<TEntity, TChild> remove,
    Action<TEntity> clear
)
where TChild : class
where TEntity : class
{
    public HasManyContains<TEntity, TReader, TChild, TId> Reload(
        Func<IPersistenceReader<TReader>, TId, TEntity> reload)
        => new(primaryKeyPropertyInfo, childFuzzr, add, remove, clear, reload);
}

public class HasManyContains<TEntity, TReader, TChild, TId>(
    PropertyInfo primaryKeyPropertyInfo,
    FuzzrOf<TChild> childFuzzr,
    Action<TEntity, TChild> add,
    Action<TEntity, TChild> remove,
    Action<TEntity> clear,
    Func<IPersistenceReader<TReader>, TId, TEntity> reload
)
where TChild : class
where TEntity : class
{
    public HasManyEmpty<TEntity, TReader, TChild, TId> Contains(Func<TEntity, TChild, bool> contains)
        => new(primaryKeyPropertyInfo, childFuzzr, add, remove, clear, reload, contains);
}

public class HasManyEmpty<TEntity, TReader, TChild, TId>(
    PropertyInfo primaryKeyPropertyInfo,
    FuzzrOf<TChild> childFuzzr,
    Action<TEntity, TChild> add,
    Action<TEntity, TChild> remove,
    Action<TEntity> clear,
    Func<IPersistenceReader<TReader>, TId, TEntity> reload,
    Func<TEntity, TChild, bool> contains
)
where TChild : class
where TEntity : class
{
    public Func<IPersistenceScope<TReader>, PoolElement<TEntity>, CheckrOf<Case>> Empty(
        Func<TEntity, bool> empty)
    {
        return
            (scope, element) => GetHasManyCheckr(
                element, add, remove, clear, reload, contains, empty, childFuzzr, scope);
    }

    public CheckrOf<Case> GetHasManyCheckr(
        PoolElement<TEntity> info,
        Action<TEntity, TChild> add,
        Action<TEntity, TChild> remove,
        Action<TEntity> clear,
        Func<IPersistenceReader<TReader>, TId, TEntity> reload,
        Func<TEntity, TChild, bool> contains,
        Func<TEntity, bool> empty,
        FuzzrOf<TChild> childFuzzr,
        IPersistenceScope<TReader> scope)
    {
        var entityName = typeof(TEntity).Name;
        var childEntityName = typeof(TChild).Name;
        return
            from id in Checkr.Capture(() => (TId)primaryKeyPropertyInfo.GetValue(info.Value)!)
            from entity in Checkr.Capture(() => scope.GetById<TEntity>(id))
            from children in Checkr.Input("Children", childFuzzr.Many(2))
            from updated in Checkr.Act("Add Children", () =>
            {
                foreach (var child in children)
                {
                    add(entity, child);
                }
                CommitAndStartNewSession(scope);
            })
            from reloaded in Checkr.Capture(
                () => reload(scope.Reader, id))
            from canUpdate in Checkr.Expect($"{entityName} Can Add {childEntityName}", () => children.All(a => contains(reloaded, a)))
            from removedChild in Checkr.Capture(children.First)
            from retainedChildren in Checkr.Capture(() => children.Skip(1).ToList())
            from removeChild in Checkr.Act("Remove Child", () =>
            {
                remove(reloaded, removedChild);
                CommitAndStartNewSession(scope);
            })
            from reloadedRemoved in Checkr.Capture(
                () => reload(scope.Reader, id))
            from canRemove in Checkr.Expect(
                $"{entityName} Can Remove {childEntityName}",
                () => !contains(reloadedRemoved, removedChild),
                report => [
                    $"Expected: {report.IntroduceThis(removedChild)} absent",
                    $"Actual:   {report.IntroduceThis(removedChild)} present"])
            from retainsOthers in Checkr.Expect(
                $"{entityName} Retains Other {childEntityName}",
                () => retainedChildren.All(child => contains(reloadedRemoved, child)),
                report => [
                    $"Expected: {report.IntroduceThis(retainedChildren)}",
                    $"Actual:   {report.IntroduceThis(retainedChildren.Where(child => contains(reloadedRemoved, child)).ToList())}"])
            from clearMany in Checkr.Act("Clear Children", () =>
            {
                clear(reloadedRemoved);
                CommitAndStartNewSession(scope);
            })
            from reloadedCleared in Checkr.Capture(
                () => reload(scope.Reader, id))
            from cleared in Checkr.Expect($"{entityName} Can Clear {childEntityName}", () => empty(reloadedCleared))
            from stored in info.Replace(reloadedCleared)
            select Case.Closed;
    }

    private static void CommitAndStartNewSession(IPersistenceScope scope)
    {
        scope.Commit();
        scope.StartNewSession();
    }
}
