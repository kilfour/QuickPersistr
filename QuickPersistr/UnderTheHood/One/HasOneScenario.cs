using QuickCheckr;
using QuickFuzzr;

namespace QuickPersistr.UnderTheHood.One;

public class HasOneScenario<TEntity, TReader, TChild, TId>(
    HasOneDefinition<TEntity, TReader, TChild, TId> definition,
    IPersistenceScope<TReader> scope)
where TEntity : class
where TChild : class
{
    private readonly string entityName = typeof(TEntity).Name;
    private readonly string childEntityName = typeof(TChild).Name;

    public CheckrOf<Case> Check(PoolElement<TEntity> element) =>
        from set in SetChild(element)
        from replaced in ReplaceChild(set)
        from sourceForClear in ReassignIfRequested(replaced)
        from cleared in ClearChild(sourceForClear)
        from stored in element.Replace(cleared)
        select Case.Closed;

    public CheckrOf<Case> CheckAdditive(PoolElement<TEntity> element) =>
        from set in SetChild(element)
        from replaced in ReplaceChild(set)
        from stored in element.Replace(replaced.Source)
        select Case.Closed;

    private CheckrOf<SetRelationship> SetChild(PoolElement<TEntity> element) =>
        from sourceId in Checkr.Capture(
            () => definition.Identity.Select(element.Value))
        from source in Checkr.Capture(
            () => definition.Identity.GetById<TEntity>(scope, sourceId))
        from child in Checkr.Input("Child", definition.ChildFuzzr)
        from set in Checkr.Act("Set Child", () =>
        {
            definition.Set(source, child);
            CommitAndStartNewSession();
        })
        from reloaded in Checkr.Capture(
            () => definition.Reload(scope.Reader, sourceId))
        from canSet in ExpectPresence(
            $"{entityName} Can Set {childEntityName}",
            reloaded,
            child,
            expectedPresent: true)
        select new SetRelationship(sourceId, child, reloaded);

    private CheckrOf<SetRelationship> ReplaceChild(SetRelationship set) =>
        from replacement in Checkr.Input("Replacement Child", definition.ChildFuzzr)
        from replace in Checkr.Act("Replace Child", () =>
        {
            definition.Set(set.Source, replacement);
            CommitAndStartNewSession();
        })
        from reloaded in Checkr.Capture(
            () => definition.Reload(scope.Reader, set.SourceId))
        from canReplace in ExpectPresence(
            $"{entityName} Can Replace {childEntityName}",
            reloaded,
            replacement,
            expectedPresent: true)
        from releasesPrevious in ExpectPresence(
            $"{entityName} Releases Previous {childEntityName}",
            reloaded,
            set.Child,
            expectedPresent: false)
        select new SetRelationship(set.SourceId, replacement, reloaded);

    private CheckrOf<SourceForClear> ReassignIfRequested(SetRelationship set)
    {
        if (definition.Reassign is null)
        {
            return Checkr.Capture(() =>
                new SourceForClear(set.SourceId, set.Source));
        }

        return Reassign(set, definition.Reassign);
    }

    private CheckrOf<SourceForClear> Reassign(
        SetRelationship added,
        Action<TEntity, TEntity, TChild> reassign) =>
        from destination in Checkr.Input(
            "Destination",
            EntityCreator,
            [.. definition.EntityShrinkers])
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
            () => definition.Reload(scope.Reader, added.SourceId))
        from destinationForMove in Checkr.Capture(
            () => definition.Reload(scope.Reader, destinationId))
        from move in Checkr.Act("Reassign Child", () =>
        {
            reassign(sourceForMove, destinationForMove, added.Child);
            CommitAndStartNewSession();
        })
        from reloadedSource in Checkr.Capture(
            () => definition.Reload(scope.Reader, added.SourceId))
        from reloadedDestination in Checkr.Capture(
            () => definition.Reload(scope.Reader, destinationId))
        from sourceReleases in ExpectPresence(
            $"Source {entityName} Releases {childEntityName}",
            reloadedSource,
            added.Child,
            expectedPresent: false)
        from destinationReceives in ExpectPresence(
            $"Destination {entityName} Receives {childEntityName}",
            reloadedDestination,
            added.Child,
            expectedPresent: true)
        select new SourceForClear(
            destinationId,
            reloadedDestination);

    private CheckrOf<TEntity> ClearChild(SourceForClear source) =>
        from clear in Checkr.Act("Clear Child", () =>
        {
            definition.Clear!(source.Source);
            CommitAndStartNewSession();
        })
        from reloaded in Checkr.Capture(
            () => definition.Reload(scope.Reader, source.SourceId))
        from canClear in Checkr.Expect(
            $"{entityName} Can Clear {childEntityName}",
            () => definition.Empty!(reloaded))
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

    private record SetRelationship(TId SourceId, TChild Child, TEntity Source);

    private record SourceForClear(TId SourceId, TEntity Source);
}
