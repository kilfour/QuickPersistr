using QuickCheckr;
using QuickCheckr.UnderTheHood;
using QuickFuzzr;

namespace QuickPersistr.UnderTheHood;

public class PersistenceSpecification<TReader, TEntity, TId>(
    IdentitySelector<TEntity, TId> identitySelector,
    List<PropertyCheck<TEntity>> propertyChecks,
    List<Func<IPersistenceScope<TReader>, PoolElement<TEntity>, CheckrOf<Case>>> oneToOnes,
    List<Func<IPersistenceScope<TReader>, PoolElement<TEntity>, CheckrOf<Case>>> oneToManies,
    List<AfterDeleteCheck<TReader, TEntity>> afterDeleteChecks,
    List<RejectedOperation<TEntity>> rejectedCreates,
    List<RejectedOperation<TEntity>> rejectedUpdates,
    List<RejectedOperation<TEntity>> rejectedDeletes,
    List<DomainMutation<TEntity>> domainUpdates,
    List<OptimisticConcurrency<TEntity>> concurrencyScenarios,
    IReadOnlyList<Shrinker> entityShrinkers)
: IPersistenceSpecification<TReader>
where TEntity : class
{
    private readonly string entityName = typeof(TEntity).Name;
    private readonly string identityName = identitySelector.QualifiedName(typeof(TEntity).Name);

    public int CheckrCount =>
        5 +
        oneToOnes.Count +
        oneToManies.Count +
        rejectedCreates.Count +
        rejectedUpdates.Count +
        rejectedDeletes.Count +
        domainUpdates.Count +
        concurrencyScenarios.Count;

    public FuzzrOf<T> GetCreator<T>()
    where T : class
        => Creator.Select(a => (a as T)!);

    public CheckrOf<Case> GetNestedCheckr<T>(
        IPersistenceScope<TReader> scope,
        IReadOnlyList<T> created,
        T active,
        Action<T> add,
        string keyPrefix)
    where T : class =>
        NestedCheckr(
            created.Cast<TEntity>().ToList(),
            (active as TEntity)!,
            child => add((child as T)!),
            keyPrefix,
            scope);

    public CheckrOf<Case> GetNestedDeleteCheckr<T>(
        IPersistenceScope<TReader> scope,
        T active,
        Action<T> add,
        string keyPrefix)
    where T : class =>
        NestedDeleteCheckr(
            (active as TEntity)!,
            child => add((child as T)!),
            keyPrefix,
            scope);

    public IList<CheckrOf<Case>> ToCheckrs(IPersistenceScope<TReader> scope) =>
        [
            .. CruCheckrs(scope),
            .. DomainUpdateCheckrs(scope),
            .. OptimisticConcurrencyCheckrs(scope),
            .. OneToOneCheckrs(scope),
            .. OneToManyCheckrs(scope),
            .. RejectedCreateCheckrs(scope),
            .. RejectedUpdateCheckrs(scope),
            .. RejectedDeleteCheckrs(scope),
            .. DeleteCheckr(scope),
            CreateSeveralCheckr(scope)
        ];

    private IList<CheckrOf<Case>> CruCheckrs(IPersistenceScope scope) => [
        CreateCheckr(scope),
        Trackr.OneOfPool<TEntity>("Entity", info => ReadCheckr(info, scope)),
        Trackr.OneOfPool<TEntity>("Entity", info => UpdateCheckr(scope, info))];

    private IList<CheckrOf<Case>> OneToManyCheckrs(IPersistenceScope<TReader> scope) =>
        [.. oneToManies.Select(a =>
            Trackr.OneOfPool<TEntity>("Entity", info
                => a(scope,info)))];

    private IList<CheckrOf<Case>> OneToOneCheckrs(IPersistenceScope<TReader> scope) =>
        [.. oneToOnes.Select(a =>
            Trackr.OneOfPool<TEntity>("Entity", info
                => a(scope, info)))];

    private IList<CheckrOf<Case>> DeleteCheckr(IPersistenceScope<TReader> scope) => [
        Trackr.OneOfPool<TEntity>("Entity", info => DeleteCheckr(scope, info))];

    private IList<CheckrOf<Case>> DomainUpdateCheckrs(IPersistenceScope<TReader> scope)
    {
        var scenario = new DomainMutationScenario<TReader, TEntity, TId>(
            identitySelector,
            propertyChecks,
            scope);
        return [.. domainUpdates.Select(mutation =>
            Trackr.OneOfPool<TEntity>("Entity", element => scenario.Check(mutation, element)))];
    }

    private IList<CheckrOf<Case>> OptimisticConcurrencyCheckrs(
        IPersistenceScope<TReader> scope)
    {
        var runner = new OptimisticConcurrencyScenario<TReader, TEntity, TId>(
            identitySelector,
            propertyChecks,
            scope);
        return [.. concurrencyScenarios.Select(scenario =>
            Trackr.OneOfPool<TEntity>("Entity", element => runner.Check(scenario, element)))];
    }

    private IList<CheckrOf<Case>> RejectedCreateCheckrs(IPersistenceScope<TReader> scope)
    {
        var scenario = RejectedOperations(scope);
        return [.. rejectedCreates.Select(operation => scenario.Create(operation, Creator))];
    }

    private IList<CheckrOf<Case>> RejectedUpdateCheckrs(IPersistenceScope<TReader> scope)
    {
        var scenario = RejectedOperations(scope);
        return [.. rejectedUpdates.Select(operation =>
            Trackr.OneOfPool<TEntity>("Entity", element => scenario.Update(operation, element)))];
    }

    private IList<CheckrOf<Case>> RejectedDeleteCheckrs(IPersistenceScope<TReader> scope)
    {
        var scenario = RejectedOperations(scope);
        return [.. rejectedDeletes.Select(operation =>
            Trackr.OneOfPool<TEntity>("Entity", element => scenario.Delete(operation, element)))];
    }

    private RejectedOperationScenario<TReader, TEntity, TId> RejectedOperations(
        IPersistenceScope<TReader> scope) =>
        new(identitySelector, propertyChecks, entityShrinkers, scope);

    private readonly FuzzrOf<TEntity> Creator =
        from ignore in Configr.Ignore(identitySelector.Properties.Contains)
        from entity in Fuzzr.One<TEntity>()
        select entity;

    private FuzzrOf<TEntity> Modifier(TEntity course) =>
        from ignore in Configr.Ignore(identitySelector.Properties.Contains)
        from entity in Fuzzr.One(() => course)
        select entity;

    private FuzzrOf<TEntity> NestedModifier(TEntity entity) =>
        from ignore in Configr.Ignore(property =>
            property.DeclaringType?.IsAssignableFrom(typeof(TEntity)) == true &&
            propertyChecks.All(check => check.Name != property.Name))
        from updated in Fuzzr.One(() => entity)
        select updated;

    private CheckrOf<Case> CreateCheckr(IPersistenceScope scope) =>
        from entity in Checkr.Input("Entity", Creator, [.. entityShrinkers])
        from create in Checkr.Act($"Create {entityName}", () =>
        {
            scope.Add(entity);
            CommitAndStartNewSession(scope);
        })
        from canCreate in Checkr.Expect(
            $"Can Create {entityName}",
            () => identitySelector.IsNonDefault(identitySelector.Select(entity)),
            report => [
                $"Expected: Non-default {identityName}",
                $"Actual:   {report.IntroduceThis(identitySelector.Select(entity))}"])
        from stored in Trackr.ToPool("Entity", () => entity)
        select Case.Closed;

    private CheckrOf<Case> ReadCheckr(PoolElement<TEntity> info, IPersistenceScope scope) =>
        ReadCheckr(info.Value, scope, null);

    private CheckrOf<Case> ReadCheckr(
        TEntity expected,
        IPersistenceScope scope,
        string? keyPrefix) =>
        from entity in Checkr.Act(Key(keyPrefix, $"Read {entityName}"), () =>
            identitySelector.GetById<TEntity>(scope, identitySelector.Select(expected)))
        from canRead in ReadExpectations(expected, entity)
        select Case.Closed;

    private CheckrOf<Case> NestedCheckr(
        IReadOnlyList<TEntity> created,
        TEntity active,
        Action<TEntity> add,
        string keyPrefix,
        IPersistenceScope scope)
    {
        var info = new PoolElement<TEntity>(NestedPoolId(active), active);
        return
            from canCreate in NestedCreateExpectations(created)
            from ensured in Checkr.Capture(() =>
                EnsureNestedEntity(scope, active, add))
            from persistedContract in NestedPersistedCheckr(
                (IPersistenceScope<TReader>)scope,
                info,
                add,
                keyPrefix)
            select Case.Closed;
    }

    private bool EnsureNestedEntity(
        IPersistenceScope scope,
        TEntity active,
        Action<TEntity> add)
    {
        // A failing expectation after the nested delete causes QuickCheckr to replay
        // this same parent execution while shrinking. Reattach the dependent through
        // its parent so that replay observes the same starting state and required FKs.
        try
        {
            var identity = identitySelector.Select(active);
            if (identitySelector.GetById<TEntity>(scope, identity) is null)
            {
                add(active);
                scope.Add(active);
                CommitAndStartNewSession(scope);
            }
            return identitySelector.GetById<TEntity>(scope, identity) is not null;
        }
        catch
        {
            scope.StartNewSession();
            return false;
        }
    }

    private int NestedPoolId(TEntity entity) =>
        int.MinValue +
        (identitySelector.Comparer.GetHashCode(identitySelector.Select(entity)!) & int.MaxValue);

    private CheckrOf<Case> NestedDeleteCheckr(
        TEntity active,
        Action<TEntity> add,
        string keyPrefix,
        IPersistenceScope<TReader> scope)
    {
        var info = new PoolElement<TEntity>(NestedPoolId(active), active);
        return
            from ensured in Checkr.Act(
                Key(keyPrefix, $"Prepare {entityName} For Delete"),
                () => EnsureNestedEntityForDelete(scope, active, add))
            from canDelete in ensured
                ? DeleteCheckr(scope, info, keyPrefix)
                : Checkr.Capture(() => Case.Closed)
            select Case.Closed;
    }

    private bool EnsureNestedEntityForDelete(
        IPersistenceScope scope,
        TEntity active,
        Action<TEntity> add)
    {
        if (!EnsureNestedEntity(scope, active, add))
        {
            throw new InvalidOperationException(
                $"Could not restore dependent {entityName} before checking its delete contract.");
        }
        return true;
    }

    private CheckrOf<Case> NestedPersistedCheckr(
        IPersistenceScope<TReader> scope,
        PoolElement<TEntity> info,
        Action<TEntity> add,
        string keyPrefix)
    {
        var domainScenario = new DomainMutationScenario<TReader, TEntity, TId>(
            identitySelector,
            propertyChecks,
            scope);
        var concurrencyScenario = new OptimisticConcurrencyScenario<TReader, TEntity, TId>(
            identitySelector,
            propertyChecks,
            scope);
        var rejectedScenario = RejectedOperations(scope);
        return
            from canRead in ReadCheckr(info.Value, scope, keyPrefix)
            from canUpdate in NestedUpdateCheckr(
                info,
                scope,
                keyPrefix)
            from domainUpdates in Combine.Checkrs(
                domainUpdates.Select(mutation => domainScenario.Check(
                    mutation,
                    info,
                    keyPrefix)))
            from concurrency in Combine.Checkrs(
                concurrencyScenarios.Select(scenario => concurrencyScenario.Check(
                    scenario,
                    info,
                    keyPrefix)))
            from oneToOnes in Combine.Checkrs(
                oneToOnes.Select(relationship => relationship(scope, info)))
            from oneToManies in Combine.Checkrs(
                oneToManies.Select(relationship => relationship(scope, info)))
            from rejectedCreates in Combine.Checkrs(
                rejectedCreates.Select(operation =>
                    rejectedScenario.Create(
                        operation,
                        Creator,
                        add,
                        keyPrefix)))
            from rejectedUpdates in Combine.Checkrs(
                rejectedUpdates.Select(operation => rejectedScenario.Update(
                    operation,
                    info,
                    keyPrefix)))
            from rejectedDeletes in Combine.Checkrs(
                rejectedDeletes.Select(operation => rejectedScenario.Delete(
                    operation,
                    info,
                    keyPrefix)))
            select Case.Closed;
    }

    private CheckrOf<Case> NestedCreateExpectations(
        IReadOnlyList<TEntity> entities) =>
        from canCreate in Checkr.Expect(
            $"Can Create {entityName}",
            () => entities.All(entity =>
                identitySelector.IsNonDefault(identitySelector.Select(entity))),
            report => [
                $"Expected: Non-default {identityName} values",
                $"Actual:   {report.IntroduceThis(entities.Select(identitySelector.Select).ToList())}"])
        from canCreateSeveral in entities.Count > 1
            ? NestedCreateSeveralExpectations(entities)
            : Checkr.Capture(() => Case.Closed)
        select Case.Closed;

    private CheckrOf<Case> NestedCreateSeveralExpectations(
        IReadOnlyList<TEntity> entities) =>
        from nonDefault in Checkr.Expect(
            $"Can Create Several {entityName}",
            () => entities.All(entity =>
                identitySelector.IsNonDefault(identitySelector.Select(entity))),
            report => [
                $"Expected: {entities.Count} non-default {identityName} values",
                $"Actual:   {report.IntroduceThis(entities.Select(identitySelector.Select).ToList())}"])
        from unique in Checkr.Expect(
            $"Can Create Unique {identityName}",
            () => entities
                .Select(identitySelector.Select)
                .Distinct(identitySelector.Comparer)
                .Count() == entities.Count,
            report => [
                $"Expected: {entities.Count} distinct {identityName} values",
                $"Actual:   {report.IntroduceThis(entities.Select(identitySelector.Select).ToList())}"])
        select Case.Closed;

    private CheckrOf<Case> NestedUpdateCheckr(
        PoolElement<TEntity> info,
        IPersistenceScope scope,
        string keyPrefix)
    {
        if (propertyChecks.Count == 0)
            return Checkr.Capture(() => Case.Closed);

        return
            from entity in Checkr.Capture(() =>
                identitySelector.GetById<TEntity>(scope, identitySelector.Select(info.Value))
                ?? info.Value)
            from updatedEntity in Checkr.Input(
                $"{keyPrefix}: Updated Child",
                () => NestedModifier(entity),
                [.. entityShrinkers])
            from updated in Checkr.Act(
                $"{keyPrefix}: Update {entityName}",
                () => CommitAndStartNewSession(scope))
            from reloaded in Checkr.Capture(() =>
                identitySelector.GetById<TEntity>(scope, identitySelector.Select(info.Value)))
            from canUpdate in Combine.Checkrs(
                propertyChecks.Select(check =>
                    Checkr.Expect(
                        $"Can Update {entityName}.{check.Name}",
                        () => check.Check(updatedEntity, reloaded),
                        report => [
                            $"Expected: {report.IntroduceThis(check.GetValue(updatedEntity))}",
                            $"Actual:   {report.IntroduceThis(check.GetValue(reloaded))}"])))
            from stored in info.Id < 0
                ? Checkr.Capture(() => Case.Closed)
                : info.Replace(reloaded)
            select Case.Closed;
    }

    private CheckrOf<Case> ReadExpectations(TEntity expected, TEntity entity) =>
        from canReadPrimaryKey in Checkr.Expect(
            $"Can Read {identityName}",
            () => identitySelector.Comparer.Equals(
                identitySelector.Select(expected),
                identitySelector.Select(entity)),
            report => [
                $"Expected: {report.IntroduceThis(identitySelector.Select(expected))}",
                $"Actual:   {report.IntroduceThis(identitySelector.Select(entity))}"])
        from canRead in Combine.Checkrs(
            propertyChecks.Select(a =>
                Checkr.Expect($"Can Read {entityName}.{a.Name}", () => a.Check(expected, entity),
                report => [
                        $"Expected: {report.IntroduceThis(a.GetValue(expected))}",
                        $"Actual:   {report.IntroduceThis(a.GetValue(entity))}"])))
        select Case.Closed;

    private CheckrOf<Case> UpdateCheckr(IPersistenceScope scope, PoolElement<TEntity> info) =>
        from entity in Checkr.Capture(() =>
            identitySelector.GetById<TEntity>(scope, identitySelector.Select(info.Value)))
        from updatedEntity in Checkr.Input(
            "Updated Entity",
            Modifier(entity),
            [.. entityShrinkers])
        from updated in Checkr.Act($"Update {entityName}", () => CommitAndStartNewSession(scope))
        from reloaded in Checkr.Capture(
            () => identitySelector.GetById<TEntity>(scope, identitySelector.Select(info.Value)))
        from canRead in Combine.Checkrs(
            propertyChecks.Select(a =>
                Checkr.Expect($"Can Update {entityName}.{a.Name}",
                    () => a.Check(updatedEntity, reloaded),
                    report => [
                        $"Expected: {report.IntroduceThis(a.GetValue(updatedEntity))}",
                        $"Actual:   {report.IntroduceThis(a.GetValue(reloaded))}"])))
        from stored in info.Replace(reloaded)
        select Case.Closed;

    private CheckrOf<Case> DeleteCheckr(
        IPersistenceScope<TReader> scope,
        PoolElement<TEntity> info,
        string? keyPrefix = null) =>
        from delete in Checkr.Act(Key(keyPrefix, $"Delete {entityName}"),
            () =>
            {
                identitySelector.DeleteById<TEntity>(scope, identitySelector.Select(info.Value));
                CommitAndStartNewSession(scope);
            })
        from reloaded in Checkr.Capture(
            () => identitySelector.GetById<TEntity>(scope, identitySelector.Select(info.Value)))
        from canDelete in Checkr.Expect($"Can Delete {entityName}", () => reloaded is null)
        from afterDelete in Combine.Checkrs(
            afterDeleteChecks.Select(check =>
                Checkr.Expect(
                    $"Deleting {entityName} {check.Description}",
                    () => check.Check(scope.Reader, info.Value))))
        from stored in info.Id < 0
            ? Checkr.Capture(() => Case.Closed)
            : info.Remove()
        select Case.Closed;

    private static string Key(string? prefix, string key) =>
        prefix is null ? key : $"{prefix}: {key}";

    private CheckrOf<Case> CreateSeveralCheckr(IPersistenceScope scope) =>
        from entities in Checkr.Input(
            "Entities",
            Creator.Many(2),
            [.. entityShrinkers])
        from create in Checkr.Act($"Create Several {entityName}", () =>
        {
            foreach (var entity in entities)
            {
                scope.Add(entity);
            }
            CommitAndStartNewSession(scope);
        })
        from identities in Checkr.Capture(() => entities
            .Select(identitySelector.Select)
            .ToList())
        from nonDefault in Checkr.Expect(
            $"Can Create Several {entityName}",
            () => identities.Count == 2 && identities.All(identitySelector.IsNonDefault),
            report => [
                $"Expected: 2 non-default {identityName} values",
                $"Actual:   {report.IntroduceThis(identities)}"])
        from unique in Checkr.Expect(
            $"Can Create Unique {identityName}",
            () => identities.Distinct(identitySelector.Comparer).Count() == identities.Count,
            report => [
                $"Expected: {identities.Count} distinct {identityName} values",
                $"Actual:   {report.IntroduceThis(identities)}"])
        select Case.Closed;

    public CheckrOf<Case> GetHasManyCheckr<T, TChild>(
        PoolElement<T> info,
        Action<T, TChild> apply,
        Func<T, TChild, bool> check,
        FuzzrOf<TChild> childFuzzr,
        IPersistenceScope scope)
    where T : class =>
        from entity in Checkr.Capture(() =>
            identitySelector.GetById<T>(scope, identitySelector.Select((info.Value as TEntity)!)))
        from children in Checkr.Input("Children", childFuzzr.Many(1, 3))
        from updated in Checkr.Act("Add Many", () =>
        {
            foreach (var child in children)
            {
                apply(entity, child);
            }
            CommitAndStartNewSession(scope);
        })
        from reloaded in Checkr.Capture(
            () => identitySelector.GetById<T>(scope, identitySelector.Select((info.Value as TEntity)!)))
        from canUpdate in Trackr.PoolExpectEach<TChild>($"{entityName} Has Many",
            child => check(reloaded, child))
        from stored in info.Replace(reloaded)
        select Case.Closed;

    private static void CommitAndStartNewSession(IPersistenceScope scope)
    {
        scope.Commit();
        scope.StartNewSession();
    }

}
