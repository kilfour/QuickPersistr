using QuickCheckr;
using QuickCheckr.UnderTheHood;
using QuickFuzzr;

namespace QuickPersistr.UnderTheHood;

public sealed class RejectedOperationScenario<TReader, TEntity, TId>(
    IdentitySelector<TEntity, TId> identitySelector,
    IReadOnlyList<PropertyCheck<TEntity>> propertyChecks,
    IReadOnlyList<Shrinker> entityShrinkers,
    IPersistenceScope<TReader> scope)
where TEntity : class
{
    private readonly string entityName = typeof(TEntity).Name;
    private readonly string identityName = identitySelector.QualifiedName(typeof(TEntity).Name);

    public CheckrOf<Case> Create(
        RejectedOperation<TEntity> operation,
        FuzzrOf<TEntity> creator,
        Action<TEntity>? add = null,
        string? keyPrefix = null) =>
        from entity in Checkr.Input(
            Key(keyPrefix, "Rejected Entity", operation.Description),
            creator,
            [.. entityShrinkers])
        from rejected in Checkr.ActCarefully(
            Key(keyPrefix, $"Attempt Rejected Create {entityName}: {operation.Description}"),
            () => AttemptCreate(operation, entity, add))
        from rejectionExpected in operation.ExpectRejection(
            $"Rejects Creating {entityName}: {operation.Description}",
            rejected)
        from reloaded in Checkr.Capture(() =>
            identitySelector.GetById<TEntity>(scope, identitySelector.Select(entity)))
        from unchanged in Checkr.Expect(
            $"Rejected Create Leaves {entityName} Absent",
            () => reloaded is null)
        select Case.Closed;

    private static string Key(
        string? prefix,
        string key,
        string description) =>
        prefix is null ? key : $"{prefix}: {key}: {description}";

    private static string Key(string? prefix, string key) =>
        prefix is null ? key : $"{prefix}: {key}";

    public CheckrOf<Case> Update(
        RejectedOperation<TEntity> operation,
        PoolElement<TEntity> element,
        string? keyPrefix = null) =>
        from entity in Checkr.Capture(() =>
            identitySelector.GetById<TEntity>(scope, identitySelector.Select(element.Value)))
        from before in Checkr.Capture(() => Snapshot(entity))
        from rejected in Checkr.ActCarefully(
            Key(keyPrefix, $"Attempt Rejected Update {entityName}: {operation.Description}"),
            () => AttemptUpdate(operation, entity))
        from rejectionExpected in operation.ExpectRejection(
            $"Rejects Updating {entityName}: {operation.Description}",
            rejected)
        from reloaded in Checkr.Capture(() =>
            identitySelector.GetById<TEntity>(scope, before.Identity))
        from unchanged in Preserves("Update", before, reloaded)
        from stored in element.Id < 0
            ? Checkr.Capture(() => Case.Closed)
            : element.Replace(reloaded)
        select Case.Closed;

    public CheckrOf<Case> Delete(
        RejectedOperation<TEntity> operation,
        PoolElement<TEntity> element,
        string? keyPrefix = null) =>
        from entity in Checkr.Capture(() =>
            identitySelector.GetById<TEntity>(scope, identitySelector.Select(element.Value)))
        from before in Checkr.Capture(() => Snapshot(entity))
        from rejected in Checkr.ActCarefully(
            Key(keyPrefix, $"Attempt Rejected Delete {entityName}: {operation.Description}"),
            () => AttemptDelete(operation, entity, before.Identity))
        from rejectionExpected in operation.ExpectRejection(
            $"Rejects Deleting {entityName}: {operation.Description}",
            rejected)
        from reloaded in Checkr.Capture(() =>
            identitySelector.GetById<TEntity>(scope, before.Identity))
        from unchanged in Preserves("Delete", before, reloaded)
        from stored in element.Id < 0
            ? Checkr.Capture(() => Case.Closed)
            : element.Replace(reloaded)
        select Case.Closed;

    private CheckrOf<Case> Preserves(
        string operation,
        EntitySnapshot before,
        TEntity? actual) =>
        from identity in Checkr.Expect(
            $"Rejected {operation} Preserves {identityName}",
            () => actual is not null && identitySelector.Comparer.Equals(
                before.Identity,
                identitySelector.Select(actual)),
            report => [
                $"Expected: {report.IntroduceThis(before.Identity)}",
                $"Actual:   {report.IntroduceThis(
                    actual is null ? null : (object?)identitySelector.Select(actual))}"])
        from properties in Combine.Checkrs(
            propertyChecks.Select((check, index) =>
                Checkr.Expect(
                    $"Rejected {operation} Preserves {entityName}.{check.Name}",
                    () => actual is not null && check.Check(before.Properties[index], actual),
                    report => [
                        $"Expected: {report.IntroduceThis(before.Properties[index])}",
                        $"Actual:   {report.IntroduceThis(
                            actual is null ? null : check.GetValue(actual))}"])))
        select Case.Closed;

    private EntitySnapshot Snapshot(TEntity entity) =>
        new(
            identitySelector.Select(entity),
            propertyChecks.Select(check => check.GetValue(entity)).ToList());

    private void AttemptCreate(
        RejectedOperation<TEntity> operation,
        TEntity entity,
        Action<TEntity>? add) =>
        Attempt(() =>
        {
            operation.Attempt(entity);
            if (add is null)
                scope.Add(entity);
            else
                add(entity);
        });

    private void AttemptUpdate(RejectedOperation<TEntity> operation, TEntity entity) =>
        Attempt(() => operation.Attempt(entity));

    private void AttemptDelete(
        RejectedOperation<TEntity> operation,
        TEntity entity,
        TId identity) =>
        Attempt(() =>
        {
            operation.Attempt(entity);
            identitySelector.DeleteById<TEntity>(scope, identity);
        });

    private void Attempt(Action operation)
    {
        try
        {
            operation();
            scope.Commit();
        }
        finally
        {
            scope.StartNewSession();
        }
    }

    private sealed record EntitySnapshot(
        TId Identity,
        IReadOnlyList<object?> Properties);
}
