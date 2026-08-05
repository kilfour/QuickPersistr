using System.Reflection;
using QuickCheckr;
using QuickCheckr.UnderTheHood;
using QuickFuzzr;

namespace QuickPersistr.UnderTheHood;

public class PersistenceSpecification<TReader, TEntity>(
    PropertyInfo primaryKeyPropertyInfo,
    List<PropertyCheck<TEntity>> propertyChecks,
    List<Func<IPersistenceScope<TReader>, PoolElement<TEntity>, CheckrOf<Case>>> oneToManies,
    List<AfterDeleteCheck<TReader, TEntity>> afterDeleteChecks)
: IPersistenceSpecification<TReader>
where TEntity : class
{
    private readonly string entityName = typeof(TEntity).Name;

    public int CheckrCount => 5 + oneToManies.Count;

    public FuzzrOf<T> GetCreator<T>()
    where T : class
        => Creator.Select(a => (a as T)!);

    public IList<CheckrOf<Case>> ToCheckrs(IPersistenceScope<TReader> scope) =>
        [.. CruCheckrs(scope), .. OneToManyCheckrs(scope), .. DeleteCheckr(scope), CreateSeveralCheckr(scope)];

    private IList<CheckrOf<Case>> CruCheckrs(IPersistenceScope scope) => [
        CreateCheckr(scope),
        Trackr.OneOfPool<TEntity>("Entity", info => ReadCheckr(info, scope)),
        Trackr.OneOfPool<TEntity>("Entity", info => UpdateCheckr(scope, info))];

    private IList<CheckrOf<Case>> OneToManyCheckrs(IPersistenceScope<TReader> scope) =>
        [.. oneToManies.Select(a =>
            Trackr.OneOfPool<TEntity>("Entity", info
                => a(scope,info)))];

    private IList<CheckrOf<Case>> DeleteCheckr(IPersistenceScope<TReader> scope) => [
        Trackr.OneOfPool<TEntity>("Entity", info => DeleteCheckr(scope, info))];

    private readonly FuzzrOf<TEntity> Creator =
        from ignore in Configr.Ignore(a => a == primaryKeyPropertyInfo)
        from entity in Fuzzr.One<TEntity>()
        select entity;

    private FuzzrOf<TEntity> Modifier(TEntity course) =>
        from ignore in Configr.Ignore(a => a == primaryKeyPropertyInfo)
        from entity in Fuzzr.One(() => course)
        select entity;

    private CheckrOf<Case> CreateCheckr(IPersistenceScope scope) =>
        from entity in Checkr.Input("Entity", Creator)
        from create in Checkr.Act($"Create {entityName}", () =>
        {
            scope.Add(entity);
            CommitAndStartNewSession(scope);
        })
        from canCreate in Checkr.Expect(
            $"Can Create {entityName}",
            () => IsNonDefaultPrimaryKey(primaryKeyPropertyInfo.GetValue(entity)),
            report => [
                $"Expected: Non-default {entityName}.{primaryKeyPropertyInfo.Name}",
                $"Actual:   {report.IntroduceThis(primaryKeyPropertyInfo.GetValue(entity))}"])
        from stored in Trackr.ToPool("Entity", () => entity)
        select Case.Closed;

    private CheckrOf<Case> ReadCheckr(PoolElement<TEntity> info, IPersistenceScope scope) =>
        from entity in Checkr.Act($"Read {entityName}", () =>
            scope.GetById<TEntity>(primaryKeyPropertyInfo.GetValue(info.Value)))
        from canReadPrimaryKey in Checkr.Expect(
            $"Can Read {entityName}.{primaryKeyPropertyInfo.Name}",
            () => Equals(
                primaryKeyPropertyInfo.GetValue(info.Value),
                primaryKeyPropertyInfo.GetValue(entity)),
            report => [
                $"Expected: {report.IntroduceThis(primaryKeyPropertyInfo.GetValue(info.Value))}",
                $"Actual:   {report.IntroduceThis(primaryKeyPropertyInfo.GetValue(entity))}"])
        from canRead in Combine.Checkrs(
            propertyChecks.Select(a =>
                Checkr.Expect($"Can Read {entityName}.{a.Name}", () => a.Check(info.Value, entity),
                report => [
                        $"Expected: {report.IntroduceThis(a.GetValue(info.Value))}",
                        $"Actual:   {report.IntroduceThis(a.GetValue(entity))}"])))
        select Case.Closed;

    private CheckrOf<Case> UpdateCheckr(IPersistenceScope scope, PoolElement<TEntity> info) =>
        from entity in Checkr.Capture(() => scope.GetById<TEntity>(primaryKeyPropertyInfo.GetValue(info.Value)))
        from updatedEntity in Checkr.Input("Updated Entity", Modifier(entity))
        from updated in Checkr.Act($"Update {entityName}", () => CommitAndStartNewSession(scope))
        from reloaded in Checkr.Capture(
            () => scope.GetById<TEntity>(primaryKeyPropertyInfo.GetValue(info.Value)))
        from canRead in Combine.Checkrs(
            propertyChecks.Select(a =>
                Checkr.Expect($"Can Update {entityName}.{a.Name}",
                    () => a.Check(updatedEntity, reloaded),
                    report => [
                        $"Expected: {report.IntroduceThis(a.GetValue(updatedEntity))}",
                        $"Actual:   {report.IntroduceThis(a.GetValue(reloaded))}"])))
        from stored in info.Replace(reloaded)
        select Case.Closed;

    private CheckrOf<Case> DeleteCheckr(IPersistenceScope<TReader> scope, PoolElement<TEntity> info) =>
        from delete in Checkr.Act($"Delete {entityName}",
            () =>
            {
                scope.DeleteById<TEntity>(primaryKeyPropertyInfo.GetValue(info.Value));
                CommitAndStartNewSession(scope);
            })
        from reloaded in Checkr.Capture(
            () => scope.GetById<TEntity>(primaryKeyPropertyInfo.GetValue(info.Value)))
        from canDelete in Checkr.Expect($"Can Delete {entityName}", () => reloaded is null)
        from afterDelete in Combine.Checkrs(
            afterDeleteChecks.Select(check =>
                Checkr.Expect(
                    $"Deleting {entityName} {check.Description}",
                    () => check.Check(scope.Reader, info.Value))))
        from stored in info.Remove()
        select Case.Closed;

    private CheckrOf<Case> CreateSeveralCheckr(IPersistenceScope scope) =>
        from entities in Checkr.Input("Entities", Creator.Many(2))
        from create in Checkr.Act($"Create Several {entityName}", () =>
        {
            foreach (var entity in entities)
            {
                scope.Add(entity);
            }
            CommitAndStartNewSession(scope);
        })
        from identities in Checkr.Capture(() => entities
            .Select(primaryKeyPropertyInfo.GetValue)
            .ToList())
        from nonDefault in Checkr.Expect(
            $"Can Create Several {entityName}",
            () => identities.Count == 2 && identities.All(IsNonDefaultPrimaryKey),
            report => [
                $"Expected: 2 non-default {entityName}.{primaryKeyPropertyInfo.Name} values",
                $"Actual:   {report.IntroduceThis(identities)}"])
        from unique in Checkr.Expect(
            $"Can Create Unique {entityName}.{primaryKeyPropertyInfo.Name}",
            () => identities.Distinct().Count() == identities.Count,
            report => [
                $"Expected: {identities.Count} distinct {entityName}.{primaryKeyPropertyInfo.Name} values",
                $"Actual:   {report.IntroduceThis(identities)}"])
        select Case.Closed;

    public CheckrOf<Case> GetHasManyCheckr<T, TChild>(
        PoolElement<T> info,
        Action<T, TChild> apply,
        Func<T, TChild, bool> check,
        FuzzrOf<TChild> childFuzzr,
        IPersistenceScope scope)
    where T : class =>
        from entity in Checkr.Capture(() => scope.GetById<T>(primaryKeyPropertyInfo.GetValue(info.Value)))
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
            () => scope.GetById<T>(primaryKeyPropertyInfo.GetValue(info.Value)))
        from canUpdate in Trackr.PoolExpectEach<TChild>($"{entityName} Has Many",
            child => check(reloaded, child))
        from stored in info.Replace(reloaded)
        select Case.Closed;

    private static void CommitAndStartNewSession(IPersistenceScope scope)
    {
        scope.Commit();
        scope.StartNewSession();
    }

    private bool IsNonDefaultPrimaryKey(object? value)
    {
        var defaultValue = primaryKeyPropertyInfo.PropertyType.IsValueType
            ? Activator.CreateInstance(primaryKeyPropertyInfo.PropertyType)
            : null;
        return !Equals(value, defaultValue);
    }
}
