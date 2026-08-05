using QuickCheckr;
using QuickFuzzr;

namespace QuickPersistr.UnderTheHood.Many;

public class HasManyScenario<TEntity, TReader, TChild, TId>(
    HasManyDefinition<TEntity, TReader, TChild, TId> definition,
    IPersistenceScope<TReader> scope)
where TEntity : class
where TChild : class
{
    private readonly string entityName = typeof(TEntity).Name;
    private readonly string childEntityName = typeof(TChild).Name;

    public CheckrOf<Case> Check(PoolElement<TEntity> element) =>
        from added in AddChildren(element)
        from removed in RemoveChild(added)
        from sourceForClear in ReassignIfRequested(removed)
        from cleared in ClearChildren(sourceForClear)
        from stored in element.Replace(cleared)
        select Case.Closed;

    private CheckrOf<AddedRelationship> AddChildren(PoolElement<TEntity> element) =>
        from sourceId in Checkr.Capture(
            () => definition.Identity.Select(element.Value))
        from source in Checkr.Capture(
            () => definition.Identity.GetById<TEntity>(scope, sourceId))
        from children in Checkr.Input(
            "Children",
            definition.ChildFuzzr.Many(definition.Reassign is null ? 2 : 3))
        from add in Checkr.Act("Add Children", () =>
        {
            foreach (var child in children)
            {
                definition.Add(source, child);
            }
            CommitAndStartNewSession();
        })
        from reloaded in Checkr.Capture(
            () => definition.Reload(scope.Reader, sourceId))
        from canAdd in Checkr.Expect(
            $"{entityName} Can Add {childEntityName}",
            () => children.All(child => definition.Contains(reloaded, child)))
        select new AddedRelationship(sourceId, children.ToList(), reloaded);

    private CheckrOf<RemovedRelationship> RemoveChild(AddedRelationship added) =>
        from removedChild in Checkr.Capture(added.Children.First)
        from retainedChildren in Checkr.Capture(
            () => added.Children.Skip(1).ToList())
        from remove in Checkr.Act("Remove Child", () =>
        {
            definition.Remove(added.Source, removedChild);
            CommitAndStartNewSession();
        })
        from reloaded in Checkr.Capture(
            () => definition.Reload(scope.Reader, added.SourceId))
        from canRemove in ExpectPresence(
            $"{entityName} Can Remove {childEntityName}",
            reloaded,
            removedChild,
            expectedPresent: false)
        from retainsOthers in ExpectRetained(
            $"{entityName} Retains Other {childEntityName}",
            reloaded,
            retainedChildren)
        select new RemovedRelationship(
            added.SourceId,
            retainedChildren,
            reloaded);

    private CheckrOf<SourceForClear> ReassignIfRequested(RemovedRelationship removed)
    {
        if (definition.Reassign is null)
        {
            return Checkr.Capture(() =>
                new SourceForClear(removed.SourceId, removed.Source));
        }

        return Reassign(removed, definition.Reassign);
    }

    private CheckrOf<SourceForClear> Reassign(
        RemovedRelationship removed,
        Action<TEntity, TEntity, TChild> reassign) =>
        from destination in Checkr.Input("Destination", EntityCreator)
        from createDestination in Checkr.Act(
            $"Create Destination {entityName}",
            () =>
            {
                scope.Add(destination);
                CommitAndStartNewSession();
            })
        from destinationId in Checkr.Capture(
            () => definition.Identity.Select(destination))
        from sourceForMove in Checkr.Capture(
            () => definition.Reload(scope.Reader, removed.SourceId))
        from destinationForMove in Checkr.Capture(
            () => definition.Reload(scope.Reader, destinationId))
        from reassignedChild in Checkr.Capture(
            removed.RetainedChildren.First)
        from childrenRemainingAtSource in Checkr.Capture(
            () => removed.RetainedChildren.Skip(1).ToList())
        from move in Checkr.Act("Reassign Child", () =>
        {
            reassign(sourceForMove, destinationForMove, reassignedChild);
            CommitAndStartNewSession();
        })
        from reloadedSource in Checkr.Capture(
            () => definition.Reload(scope.Reader, removed.SourceId))
        from reloadedDestination in Checkr.Capture(
            () => definition.Reload(scope.Reader, destinationId))
        from sourceReleases in ExpectPresence(
            $"Source {entityName} Releases {childEntityName}",
            reloadedSource,
            reassignedChild,
            expectedPresent: false)
        from destinationReceives in ExpectPresence(
            $"Destination {entityName} Receives {childEntityName}",
            reloadedDestination,
            reassignedChild,
            expectedPresent: true)
        from sourceRetainsOthers in ExpectRetained(
            $"Source {entityName} Retains Other {childEntityName}",
            reloadedSource,
            childrenRemainingAtSource)
        select new SourceForClear(removed.SourceId, reloadedSource);

    private CheckrOf<TEntity> ClearChildren(SourceForClear source) =>
        from clear in Checkr.Act("Clear Children", () =>
        {
            definition.Clear(source.Source);
            CommitAndStartNewSession();
        })
        from reloaded in Checkr.Capture(
            () => definition.Reload(scope.Reader, source.SourceId))
        from canClear in Checkr.Expect(
            $"{entityName} Can Clear {childEntityName}",
            () => definition.Empty(reloaded))
        select reloaded;

    private CheckrOf<Case> ExpectPresence(
        string label,
        TEntity entity,
        TChild child,
        bool expectedPresent) =>
        Checkr.Expect(
            label,
            () => definition.Contains(entity, child) == expectedPresent,
            report => [
                $"Expected: {report.IntroduceThis(child)} {Presence(expectedPresent)}",
                $"Actual:   {report.IntroduceThis(child)} {Presence(!expectedPresent)}"]);

    private CheckrOf<Case> ExpectRetained(
        string label,
        TEntity entity,
        IReadOnlyList<TChild> children) =>
        Checkr.Expect(
            label,
            () => children.All(child => definition.Contains(entity, child)),
            report => [
                $"Expected: {report.IntroduceThis(children)}",
                $"Actual:   {report.IntroduceThis(children.Where(child => definition.Contains(entity, child)).ToList())}"]);

    private FuzzrOf<TEntity> EntityCreator =>
        from ignore in Configr.Ignore(definition.Identity.Properties.Contains)
        from entity in Fuzzr.One<TEntity>()
        select entity;

    private void CommitAndStartNewSession()
    {
        scope.Commit();
        scope.StartNewSession();
    }

    private static string Presence(bool present) => present ? "present" : "absent";

    private record AddedRelationship(
        TId SourceId,
        IReadOnlyList<TChild> Children,
        TEntity Source);

    private record RemovedRelationship(
        TId SourceId,
        IReadOnlyList<TChild> RetainedChildren,
        TEntity Source);

    private record SourceForClear(TId SourceId, TEntity Source);
}
