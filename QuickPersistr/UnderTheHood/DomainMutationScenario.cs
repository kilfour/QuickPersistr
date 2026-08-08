using QuickCheckr;
using QuickCheckr.UnderTheHood;

namespace QuickPersistr.UnderTheHood;

public sealed class DomainMutationScenario<TReader, TEntity, TId>(
    IdentitySelector<TEntity, TId> identitySelector,
    IReadOnlyList<PropertyCheck<TEntity>> propertyChecks,
    IPersistenceScope<TReader> scope)
where TEntity : class
{
    private readonly string entityName = typeof(TEntity).Name;
    private readonly string identityName = identitySelector.QualifiedName(typeof(TEntity).Name);

    public CheckrOf<Case> Check(
        DomainMutation<TEntity> mutation,
        PoolElement<TEntity> element,
        string? keyPrefix = null) =>
        from identity in Checkr.Capture(() => identitySelector.Select(element.Value))
        from entity in Checkr.Capture(() =>
            identitySelector.GetById<TEntity>(scope, identity))
        from expected in Checkr.Act(
            Key(keyPrefix, $"Update {entityName}: {mutation.Description}"),
            () => ApplyAndPersist(mutation, entity))
        from reloaded in Checkr.Capture(() =>
            identitySelector.GetById<TEntity>(scope, identity))
        from preservedIdentity in Checkr.Expect(
            $"{mutation.Description} Preserves {identityName}",
            () => identitySelector.Comparer.Equals(
                identity,
                identitySelector.Select(reloaded)),
            report => [
                $"Expected: {report.IntroduceThis(identity)}",
                $"Actual:   {report.IntroduceThis(identitySelector.Select(reloaded))}"])
        from persistedProperties in Combine.Checkrs(
            propertyChecks.Select((check, index) =>
                Checkr.Expect(
                    $"{mutation.Description} Persists {entityName}.{check.Name}",
                    () => check.Check(expected[index], reloaded),
                    report => [
                        $"Expected: {report.IntroduceThis(expected[index])}",
                        $"Actual:   {report.IntroduceThis(check.GetValue(reloaded))}"])))
        from stored in element.Id < 0
            ? Checkr.Capture(() => Case.Closed)
            : element.Replace(reloaded)
        select Case.Closed;

    private static string Key(string? prefix, string key) =>
        prefix is null ? key : $"{prefix}: {key}";

    private IReadOnlyList<object?> ApplyAndPersist(
        DomainMutation<TEntity> mutation,
        TEntity entity)
    {
        mutation.Apply(entity);
        var expected = propertyChecks
            .Select(check => check.GetValue(entity))
            .ToList();
        scope.Commit();
        scope.StartNewSession();
        return expected;
    }
}
