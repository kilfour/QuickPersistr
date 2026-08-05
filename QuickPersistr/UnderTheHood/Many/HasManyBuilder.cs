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
        => new(primaryKeyPropertyInfo, childFuzzr, add, remove, null, clear);

    public HasManyReassigned<TEntity, TReader, TChild, TId> Reassign(
        Action<TEntity, TEntity, TChild> reassign)
        => new(primaryKeyPropertyInfo, childFuzzr, add, remove, reassign);
}

public class HasManyReassigned<TEntity, TReader, TChild, TId>(
    PropertyInfo primaryKeyPropertyInfo,
    FuzzrOf<TChild> childFuzzr,
    Action<TEntity, TChild> add,
    Action<TEntity, TChild> remove,
    Action<TEntity, TEntity, TChild> reassign
)
where TChild : class
where TEntity : class
{
    public HasManyReload<TEntity, TReader, TChild, TId> Clear(Action<TEntity> clear)
        => new(primaryKeyPropertyInfo, childFuzzr, add, remove, reassign, clear);
}

public class HasManyReload<TEntity, TReader, TChild, TId>(
    PropertyInfo primaryKeyPropertyInfo,
    FuzzrOf<TChild> childFuzzr,
    Action<TEntity, TChild> add,
    Action<TEntity, TChild> remove,
    Action<TEntity, TEntity, TChild>? reassign,
    Action<TEntity> clear
)
where TChild : class
where TEntity : class
{
    public HasManyContains<TEntity, TReader, TChild, TId> Reload(
        Func<IPersistenceReader<TReader>, TId, TEntity> reload)
        => new(primaryKeyPropertyInfo, childFuzzr, add, remove, reassign, clear, reload);
}

public class HasManyContains<TEntity, TReader, TChild, TId>(
    PropertyInfo primaryKeyPropertyInfo,
    FuzzrOf<TChild> childFuzzr,
    Action<TEntity, TChild> add,
    Action<TEntity, TChild> remove,
    Action<TEntity, TEntity, TChild>? reassign,
    Action<TEntity> clear,
    Func<IPersistenceReader<TReader>, TId, TEntity> reload
)
where TChild : class
where TEntity : class
{
    public HasManyEmpty<TEntity, TReader, TChild, TId> Contains(Func<TEntity, TChild, bool> contains)
        => new(primaryKeyPropertyInfo, childFuzzr, add, remove, reassign, clear, reload, contains);
}

public class HasManyEmpty<TEntity, TReader, TChild, TId>(
    PropertyInfo primaryKeyPropertyInfo,
    FuzzrOf<TChild> childFuzzr,
    Action<TEntity, TChild> add,
    Action<TEntity, TChild> remove,
    Action<TEntity, TEntity, TChild>? reassign,
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
                element, add, remove, reassign, clear, reload, contains, empty, childFuzzr, scope);
    }

    public CheckrOf<Case> GetHasManyCheckr(
        PoolElement<TEntity> info,
        Action<TEntity, TChild> add,
        Action<TEntity, TChild> remove,
        Action<TEntity, TEntity, TChild>? reassign,
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
            from children in Checkr.Input("Children", childFuzzr.Many(reassign is null ? 2 : 3))
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
            from sourceForClear in ReassignIfRequested(
                id,
                reloadedRemoved,
                retainedChildren,
                reassign,
                reload,
                contains,
                scope)
            from clearMany in Checkr.Act("Clear Children", () =>
            {
                clear(sourceForClear);
                CommitAndStartNewSession(scope);
            })
            from reloadedCleared in Checkr.Capture(
                () => reload(scope.Reader, id))
            from cleared in Checkr.Expect($"{entityName} Can Clear {childEntityName}", () => empty(reloadedCleared))
            from stored in info.Replace(reloadedCleared)
            select Case.Closed;
    }

    private CheckrOf<TEntity> ReassignIfRequested(
        TId sourceId,
        TEntity source,
        IReadOnlyList<TChild> retainedChildren,
        Action<TEntity, TEntity, TChild>? reassign,
        Func<IPersistenceReader<TReader>, TId, TEntity> reload,
        Func<TEntity, TChild, bool> contains,
        IPersistenceScope<TReader> scope)
    {
        if (reassign is null)
        {
            return Checkr.Capture(() => source);
        }

        var entityName = typeof(TEntity).Name;
        var childEntityName = typeof(TChild).Name;
        return
            from destination in Checkr.Input("Destination", EntityCreator)
            from createDestination in Checkr.Act($"Create Destination {entityName}", () =>
            {
                scope.Add(destination);
                CommitAndStartNewSession(scope);
            })
            from destinationId in Checkr.Capture(
                () => (TId)primaryKeyPropertyInfo.GetValue(destination)!)
            from sourceForMove in Checkr.Capture(
                () => reload(scope.Reader, sourceId))
            from destinationForMove in Checkr.Capture(
                () => reload(scope.Reader, destinationId))
            from reassignedChild in Checkr.Capture(retainedChildren.First)
            from childrenRemainingAtSource in Checkr.Capture(
                () => retainedChildren.Skip(1).ToList())
            from move in Checkr.Act("Reassign Child", () =>
            {
                reassign(sourceForMove, destinationForMove, reassignedChild);
                CommitAndStartNewSession(scope);
            })
            from reloadedSource in Checkr.Capture(
                () => reload(scope.Reader, sourceId))
            from reloadedDestination in Checkr.Capture(
                () => reload(scope.Reader, destinationId))
            from sourceReleases in Checkr.Expect(
                $"Source {entityName} Releases {childEntityName}",
                () => !contains(reloadedSource, reassignedChild),
                report => [
                    $"Expected: {report.IntroduceThis(reassignedChild)} absent",
                    $"Actual:   {report.IntroduceThis(reassignedChild)} present"])
            from destinationReceives in Checkr.Expect(
                $"Destination {entityName} Receives {childEntityName}",
                () => contains(reloadedDestination, reassignedChild),
                report => [
                    $"Expected: {report.IntroduceThis(reassignedChild)} present",
                    $"Actual:   {report.IntroduceThis(reassignedChild)} absent"])
            from sourceRetainsOthers in Checkr.Expect(
                $"Source {entityName} Retains Other {childEntityName}",
                () => childrenRemainingAtSource.All(child => contains(reloadedSource, child)),
                report => [
                    $"Expected: {report.IntroduceThis(childrenRemainingAtSource)}",
                    $"Actual:   {report.IntroduceThis(childrenRemainingAtSource.Where(child => contains(reloadedSource, child)).ToList())}"])
            select reloadedSource;
    }

    private FuzzrOf<TEntity> EntityCreator =>
        from ignore in Configr.Ignore(property => property == primaryKeyPropertyInfo)
        from entity in Fuzzr.One<TEntity>()
        select entity;

    private static void CommitAndStartNewSession(IPersistenceScope scope)
    {
        scope.Commit();
        scope.StartNewSession();
    }
}
