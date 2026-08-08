using QuickCheckr;
using QuickCheckr.UnderTheHood;

namespace QuickPersistr.UnderTheHood;

public sealed class OptimisticConcurrencyScenario<TReader, TEntity, TId>(
    IdentitySelector<TEntity, TId> identitySelector,
    IReadOnlyList<PropertyCheck<TEntity>> propertyChecks,
    IPersistenceScope<TReader> scope)
where TEntity : class
{
    private readonly string entityName = typeof(TEntity).Name;
    private readonly string identityName = identitySelector.QualifiedName(typeof(TEntity).Name);

    public CheckrOf<Case> Check(
        OptimisticConcurrency<TEntity> scenario,
        PoolElement<TEntity> element,
        string? keyPrefix = null) =>
        from identity in Checkr.Capture(() => identitySelector.Select(element.Value))
        from attempt in Checkr.Act(
            Key(keyPrefix, $"Update {entityName} Concurrently: {scenario.Description}"),
            () => Execute(scenario, identity))
        from verified in attempt is null
            ? Checkr.Capture(() => Case.Closed)
            : Verify(scenario, element, identity, attempt)
        select Case.Closed;

    private static string Key(string? prefix, string key) =>
        prefix is null ? key : $"{prefix}: {key}";

    private CheckrOf<Case> Verify(
        OptimisticConcurrency<TEntity> scenario,
        PoolElement<TEntity> element,
        TId identity,
        ConcurrentAttempt attempt) =>
        from conflict in scenario.ExpectConflict(
            $"Rejects Stale {entityName} Update: {scenario.Description}",
            attempt.Conflict)
        from reloaded in Checkr.Capture(() =>
            identitySelector.GetById<TEntity>(scope, identity))
        from preservedIdentity in Checkr.Expect(
            $"Concurrent {scenario.Description} Preserves {identityName}",
            () => identitySelector.Comparer.Equals(
                identity,
                identitySelector.Select(reloaded)),
            report => [
                $"Expected: {report.IntroduceThis(identity)}",
                $"Actual:   {report.IntroduceThis(identitySelector.Select(reloaded))}"])
        from persistedWinner in Combine.Checkrs(
            propertyChecks.Select((check, index) =>
                Checkr.Expect(
                    $"Concurrent {scenario.Description} Persists Winner {entityName}.{check.Name}",
                    () => check.Check(attempt.WinningProperties[index], reloaded),
                    report => [
                        $"Expected: {report.IntroduceThis(attempt.WinningProperties[index])}",
                        $"Actual:   {report.IntroduceThis(check.GetValue(reloaded))}"])))
        from stored in element.Id < 0
            ? Checkr.Capture(() => Case.Closed)
            : element.Replace(reloaded)
        select Case.Closed;

    private ConcurrentAttempt Execute(
        OptimisticConcurrency<TEntity> scenario,
        TId identity)
    {
        scope.StartNewSession();
        using var staleScope = scope.OpenConcurrentSession();
        var winner = identitySelector.GetById<TEntity>(scope, identity);
        var stale = identitySelector.GetById<TEntity>(staleScope, identity);

        scenario.WinningUpdate(winner);
        var winningProperties = propertyChecks
            .Select(check => check.GetValue(winner))
            .ToList();
        scope.Commit();
        scope.StartNewSession();

        DelayedResult conflict;
        try
        {
            scenario.ConflictingUpdate(stale);
            staleScope.Commit();
            conflict = new DelayedResult();
        }
        catch (Exception exception)
        {
            conflict = new DelayedResult(exception);
        }

        return new(winningProperties, conflict);
    }

    private sealed record ConcurrentAttempt(
        IReadOnlyList<object?> WinningProperties,
        DelayedResult Conflict);
}
