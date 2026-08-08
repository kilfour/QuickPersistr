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
        from relationship in Checkr.Capture(() => new SetRelationship(
            replaced.SourceId,
            replaced.Active,
            replaced.Source,
            replaced.Nested,
            replaced.ContractEligible))
        from sourceForClear in ReassignIfRequested(relationship)
        from cleared in ClearChild(sourceForClear)
        from deletedChild in CheckChildDelete(replaced, cleared.ContractEligible)
        from current in Checkr.Capture(
            () => definition.Reload(scope.Reader, replaced.SourceId))
        from stored in element.Id < 0
            ? Checkr.Capture(() => Case.Closed)
            : element.Replace(current)
        select Case.Closed;

    public CheckrOf<Case> CheckAdditive(PoolElement<TEntity> element) =>
        from set in SetChild(element)
        from replaced in ReplaceChild(set)
        from deletedChild in CheckChildDelete(replaced, replaced.ContractEligible)
        from current in Checkr.Capture(
            () => definition.Reload(scope.Reader, replaced.SourceId))
        from stored in element.Id < 0
            ? Checkr.Capture(() => Case.Closed)
            : element.Replace(current)
        select Case.Closed;

    private CheckrOf<SetRelationship> SetChild(PoolElement<TEntity> element) =>
        from sourceId in Checkr.Capture(
            () => definition.Identity.Select(element.Value))
        from source in Checkr.Capture(
            () => definition.Identity.GetById<TEntity>(scope, sourceId))
        from child in Checkr.Input(Key(element.Id < 0, "Child"), definition.ChildFuzzr)
        from set in Checkr.Act(Key(element.Id < 0, "Set Child"), () =>
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
        select new SetRelationship(
            sourceId,
            child,
            reloaded,
            element.Id < 0,
            reloaded is not null && definition.Contains(reloaded, child));

    private CheckrOf<ReplacedRelationship> ReplaceChild(SetRelationship set) =>
        from replacement in Checkr.Input(
            Key(set.Nested, "Replacement Child"),
            definition.ChildFuzzr)
        from replace in Checkr.Act(Key(set.Nested, "Replace Child"), () =>
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
        from childContract in set.ContractEligible &&
            reloaded is not null &&
            definition.Contains(reloaded, replacement) &&
            !definition.Contains(reloaded, set.Child)
            ? definition.ChildSpecification.GetNestedCheckr(
                scope,
                new[] { set.Child, replacement },
                replacement,
                child => definition.Set(
                    definition.Reload(scope.Reader, set.SourceId),
                    child),
                definition.RelationshipKey)
            : Checkr.Capture(() => Case.Closed)
        from current in Checkr.Capture(
            () => definition.Reload(scope.Reader, set.SourceId))
        select new ReplacedRelationship(
            set.SourceId,
            set.Child,
            replacement,
            current,
            set.Nested,
            set.ContractEligible &&
                reloaded is not null &&
                definition.Contains(reloaded, replacement) &&
                !definition.Contains(reloaded, set.Child));

    private CheckrOf<ChildContractResult> CheckChildDelete(
        ReplacedRelationship replaced,
        bool contractEligible) =>
        from childContract in contractEligible
            ? definition.ChildSpecification.GetNestedDeleteCheckr(
                scope,
                replaced.Released,
                child => definition.Set(
                    definition.Reload(scope.Reader, replaced.SourceId),
                    child),
                definition.RelationshipKey)
            : Checkr.Capture(() => Case.Closed)
        from current in Checkr.Capture(
            () => definition.Reload(scope.Reader, replaced.SourceId))
        select new ChildContractResult(replaced.SourceId, current, replaced.Nested);

    private CheckrOf<SourceForClear> ReassignIfRequested(SetRelationship set)
    {
        if (definition.Reassign is null)
        {
            return Checkr.Capture(() =>
                new SourceForClear(
                    set.SourceId,
                    set.Source,
                    set.Nested,
                    set.ContractEligible));
        }

        return Reassign(set, definition.Reassign);
    }

    private CheckrOf<SourceForClear> Reassign(
        SetRelationship added,
        Action<TEntity, TEntity, TChild> reassign) =>
        from destination in Checkr.Input(
            Key(added.Nested, "Destination"),
            EntityCreator,
            [.. definition.EntityShrinkers])
        from createDestination in Checkr.Act(
            Key(added.Nested, $"Create Destination {entityName}"),
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
        from move in Checkr.Act(Key(added.Nested, "Reassign Child"), () =>
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
            reloadedDestination,
            added.Nested,
            added.ContractEligible &&
                reloadedSource is not null &&
                reloadedDestination is not null &&
                !definition.Contains(reloadedSource, added.Child) &&
                definition.Contains(reloadedDestination, added.Child));

    private CheckrOf<ClearedRelationship> ClearChild(SourceForClear source) =>
        from clear in Checkr.Act(Key(source.Nested, "Clear Child"), () =>
        {
            definition.Clear!(source.Source);
            CommitAndStartNewSession();
        })
        from reloaded in Checkr.Capture(
            () => definition.Reload(scope.Reader, source.SourceId))
        from canClear in Checkr.Expect(
            $"{entityName} Can Clear {childEntityName}",
            () => definition.Empty!(reloaded))
        select new ClearedRelationship(
            reloaded,
            source.ContractEligible &&
                reloaded is not null &&
                definition.Empty!(reloaded));

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

    private string Key(bool nested, string key) =>
        nested ? $"{definition.RelationshipKey}: {key}" : key;

    private record SetRelationship(
        TId SourceId,
        TChild Child,
        TEntity Source,
        bool Nested,
        bool ContractEligible);

    private record ReplacedRelationship(
        TId SourceId,
        TChild Released,
        TChild Active,
        TEntity Source,
        bool Nested,
        bool ContractEligible);

    private record ChildContractResult(TId SourceId, TEntity Source, bool Nested);

    private record SourceForClear(
        TId SourceId,
        TEntity Source,
        bool Nested,
        bool ContractEligible);

    private record ClearedRelationship(TEntity Source, bool ContractEligible);
}
