using System.Reflection;
using QuickCheckr;
using QuickCheckr.UnderTheHood;
using QuickFuzzr;
using QuickPulse.Show;

namespace QuickPersistr.UnderTheHood;

public class PersistenceSpecification<TReader, TEntity>(
    PropertyInfo primaryKeyPropertyInfo,
    List<PropertyCheck<TEntity>> propertyChecks,
    List<Func<IPersistenceScope<TReader>, PoolElement<TEntity>, CheckrOf<Case>>> oneToManies)
: IPersistenceSpecification<TReader>
where TEntity : class, new()
{
    private readonly string entityName = typeof(TEntity).Name;

    public int CheckrCount => 4 + oneToManies.Count;

    public FuzzrOf<T> GetCreator<T>()
    where T : class, new()
        => Creator.Select(a => (a as T)!);

    public IList<CheckrOf<Case>> ToCheckrs(IPersistenceScope<TReader> scope) =>
        [.. CruCheckrs(scope), .. OneToManyCheckrs(scope), .. DeleteCheckr(scope)];

    private IList<CheckrOf<Case>> CruCheckrs(IPersistenceScope scope) => [
        CreateCheckr(scope),
        Trackr.OneOfPool<TEntity>("Entity", info => ReadCheckr(info, scope)),
        Trackr.OneOfPool<TEntity>("Entity", info => UpdateCheckr(scope, info))];

    private IList<CheckrOf<Case>> OneToManyCheckrs(IPersistenceScope<TReader> scope) =>
        [.. oneToManies.Select(a =>
            Trackr.OneOfPool<TEntity>("Entity", info
                => a(scope,info)))];

    private IList<CheckrOf<Case>> DeleteCheckr(IPersistenceScope scope) => [
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
        from create in Checkr.Act($"Create {entityName}", () => { scope.Add(entity); scope.Commit(); })
        from canCreate in Checkr.Expect($"Can Create {entityName}", () => primaryKeyPropertyInfo.GetValue(entity) != default)
        from stored in Trackr.ToPool("Entity", () => entity)
        select Case.Closed;

    private CheckrOf<Case> ReadCheckr(PoolElement<TEntity> info, IPersistenceScope scope) =>
        from entity in Checkr.Act($"Read {entityName}", () =>
            scope.GetById<TEntity>(primaryKeyPropertyInfo.GetValue(info.Value)))
        from canRead in Combine.Checkrs(
            propertyChecks.Select(a =>
                Checkr.Expect($"Can Read {entityName}.{a.Name}", () => a.Check(info.Value, entity),
                () => [
                        $"Expected: {a.GetValue(info.Value)}",
                        $"Actual:   {a.GetValue(entity)}"])))
        select Case.Closed;

    private CheckrOf<Case> UpdateCheckr(IPersistenceScope scope, PoolElement<TEntity> info) =>
        from entity in Checkr.Capture(() => scope.GetById<TEntity>(primaryKeyPropertyInfo.GetValue(info.Value)))
        from updatedEntity in Checkr.Input("Updated Entity", Modifier(entity))
        from updated in Checkr.Act($"Update {entityName}", scope.Commit)
        from reloaded in Checkr.Capture(
            () => scope.GetById<TEntity>(primaryKeyPropertyInfo.GetValue(info.Value)))
        from canRead in Combine.Checkrs(
            propertyChecks.Select(a =>
                Checkr.Expect($"Can Update {entityName}.{a.Name}",
                    () => a.Check(updatedEntity, reloaded),
                    () => [
                        $"Expected: {a.GetValue(updatedEntity)}",
                        $"Actual:   {a.GetValue(reloaded)}"])))
        from stored in info.Replace(reloaded)
        select Case.Closed;

    private CheckrOf<Case> DeleteCheckr(IPersistenceScope scope, PoolElement<TEntity> info) =>
        from delete in Checkr.Act($"Delete {entityName}",
            () =>
            {
                scope.DeleteById<TEntity>(primaryKeyPropertyInfo.GetValue(info.Value));
                scope.Commit();
            })
        from reloaded in Checkr.Capture(
            () => scope.GetById<TEntity>(primaryKeyPropertyInfo.GetValue(info.Value)))
        from canDelete in Checkr.Expect($"Can Delete {entityName}", () => reloaded is null)
        from stored in info.Remove()
        select Case.Closed;

    public CheckrOf<Case> GetHasManyCheckr<T, TChild>(
        PoolElement<T> info,
        Action<T, TChild> apply,
        Func<T, TChild, bool> check,
        FuzzrOf<TChild> childFuzzr,
        IPersistenceScope scope)
    where T : class, new() =>
        from entity in Checkr.Capture(() => scope.GetById<T>(primaryKeyPropertyInfo.GetValue(info.Value)))
        from children in Checkr.Input("Children", childFuzzr.Many(1, 3))
        from updated in Checkr.Act("Add Many", () =>
        {
            foreach (var child in children)
            {
                apply(entity, child);
            }
            scope.Commit();
        })
        from reloaded in Checkr.Capture(
            () => scope.GetById<T>(primaryKeyPropertyInfo.GetValue(info.Value)))
        from canUpdate in Trackr.PoolExpectEach<TChild>($"{entityName} Has Many",
            child => check(reloaded, child))
        from stored in info.Replace(reloaded)
        select Case.Closed;
}