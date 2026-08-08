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
        from childDelete in CheckChildDelete(added, cleared.ContractEligible)
        from current in Checkr.Capture(
            () => definition.Reload(scope.Reader, added.SourceId))
        from stored in element.Id < 0
            ? Checkr.Capture(() => Case.Closed)
            : element.Replace(current)
        select Case.Closed;

    public CheckrOf<Case> CheckAdditive(PoolElement<TEntity> element) =>
        from added in AddChildren(element)
        from childDelete in CheckChildDelete(added, added.ContractEligible)
        from current in Checkr.Capture(
            () => definition.Reload(scope.Reader, added.SourceId))
        from stored in element.Id < 0
            ? Checkr.Capture(() => Case.Closed)
            : element.Replace(current)
        select Case.Closed;

    private CheckrOf<AddedRelationship> AddChildren(PoolElement<TEntity> element) =>
        from sourceId in Checkr.Capture(
            () => definition.Identity.Select(element.Value))
        from source in Checkr.Capture(
            () => definition.Identity.GetById<TEntity>(scope, sourceId))
        from children in Checkr.Input(
            Key(element.Id < 0, "Children"),
            definition.ChildFuzzr.Many(definition.Reassign is null ? 2 : 3))
        from add in Checkr.Act(Key(element.Id < 0, "Add Children"), () =>
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
        from childContract in children.Any() &&
            reloaded is not null &&
            children.All(child => definition.Contains(reloaded, child))
            ? definition.ChildSpecification.GetNestedCheckr(
                scope,
                children.ToList(),
                children.First(),
                child => definition.Add(
                    definition.Reload(scope.Reader, sourceId),
                    child),
                definition.RelationshipKey)
            : Checkr.Capture(() => Case.Closed)
        from current in Checkr.Capture(
            () => definition.Reload(scope.Reader, sourceId))
        select new AddedRelationship(
            sourceId,
            children.ToList(),
            current,
            element.Id < 0,
            children.Any() &&
                reloaded is not null &&
                children.All(child => definition.Contains(reloaded, child)));

    private CheckrOf<Case> CheckChildDelete(
        AddedRelationship added,
        bool contractEligible) =>
        contractEligible
            ? definition.ChildSpecification.GetNestedDeleteCheckr(
                scope,
                added.Children[0],
                child => definition.Add(
                    definition.Reload(scope.Reader, added.SourceId),
                    child),
                definition.RelationshipKey)
            : Checkr.Capture(() => Case.Closed);

    private CheckrOf<RemovedRelationship> RemoveChild(AddedRelationship added) =>
        from removedChild in Checkr.Capture(added.Children.First)
        from retainedChildren in Checkr.Capture(
            () => added.Children.Skip(1).ToList())
        from remove in Checkr.Act(Key(added.Nested, "Remove Child"), () =>
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
            reloaded,
            added.Nested,
            added.ContractEligible &&
                reloaded is not null &&
                !definition.Contains(reloaded, removedChild) &&
                retainedChildren.All(child => definition.Contains(reloaded, child)));

    private CheckrOf<SourceForClear> ReassignIfRequested(RemovedRelationship removed)
    {
        if (definition.Reassign is null)
        {
            return Checkr.Capture(() =>
                new SourceForClear(
                    removed.SourceId,
                    removed.Source,
                    removed.Nested,
                    removed.ContractEligible));
        }

        return Reassign(removed, definition.Reassign);
    }

    private CheckrOf<SourceForClear> Reassign(
        RemovedRelationship removed,
        Action<TEntity, TEntity, TChild> reassign) =>
        from destination in Checkr.Input(
            Key(removed.Nested, "Destination"),
            EntityCreator,
            [.. definition.EntityShrinkers])
        from createDestination in Checkr.Act(
            Key(removed.Nested, $"Create Destination {entityName}"),
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
        from move in Checkr.Act(Key(removed.Nested, "Reassign Child"), () =>
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
        select new SourceForClear(
            removed.SourceId,
            reloadedSource,
            removed.Nested,
            removed.ContractEligible &&
                reloadedSource is not null &&
                reloadedDestination is not null &&
                !definition.Contains(reloadedSource, reassignedChild) &&
                definition.Contains(reloadedDestination, reassignedChild) &&
                childrenRemainingAtSource.All(child =>
                    definition.Contains(reloadedSource, child)));

    private CheckrOf<ClearedRelationship> ClearChildren(SourceForClear source) =>
        from clear in Checkr.Act(Key(source.Nested, "Clear Children"), () =>
        {
            definition.Clear(source.Source);
            CommitAndStartNewSession();
        })
        from reloaded in Checkr.Capture(
            () => definition.Reload(scope.Reader, source.SourceId))
        from canClear in Checkr.Expect(
            $"{entityName} Can Clear {childEntityName}",
            () => definition.Empty(reloaded))
        select new ClearedRelationship(
            reloaded,
            source.ContractEligible &&
                reloaded is not null &&
                definition.Empty(reloaded));

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

    private string Key(bool nested, string key) =>
        nested ? $"{definition.RelationshipKey}: {key}" : key;

    private record AddedRelationship(
        TId SourceId,
        IReadOnlyList<TChild> Children,
        TEntity Source,
        bool Nested,
        bool ContractEligible);

    private record RemovedRelationship(
        TId SourceId,
        IReadOnlyList<TChild> RetainedChildren,
        TEntity Source,
        bool Nested,
        bool ContractEligible);

    private record SourceForClear(
        TId SourceId,
        TEntity Source,
        bool Nested,
        bool ContractEligible);

    private record ClearedRelationship(TEntity Source, bool ContractEligible);
}
