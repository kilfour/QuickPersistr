using System.Reflection;
using QuickCheckr;
using QuickFuzzr;
using QuickPulse.Show;

namespace QuickPersistr.UnderTheHood;

public class PersistenceSpecification<TEntity>(
    PropertyInfo primaryKeyExpression,
    List<Func<TEntity, TEntity, bool>> propertyChecks)
: IPersistenceSpecification
where TEntity : class, new()
{
    private readonly string entityName = typeof(TEntity).Name;
    public IList<CheckrOf<Case>> ToCheckrs(IPersistenceScope scope) =>
        [CreateCheckr(scope),
            Trackr.OneOfPool<TEntity>("Read", info => ReadCheckr(info, scope)),
            Trackr.OneOfPool<TEntity>("Update", info => UpdateCheckr(scope, info)),
            Trackr.OneOfPool<TEntity>("Delete", info => DeleteCheckr(scope, info))];

    private readonly FuzzrOf<TEntity> Creator =
        from ignore in Configr.Ignore(a => a == primaryKeyExpression)
        from entity in Fuzzr.One<TEntity>()
        select entity;

    private FuzzrOf<TEntity> Modifier(TEntity course) =>
        from ignore in Configr.Ignore(a => a == primaryKeyExpression)
        from entity in Fuzzr.One(() => course)
        select entity;

    private CheckrOf<Case> CreateCheckr(IPersistenceScope scope) =>
        from entity in Checkr.Input("Entity", Creator)
        from create in Checkr.Act("Create", () => { scope.Add(entity); scope.Commit(); })
        from canCreate in Checkr.Expect($"{entityName} Can Create", () => primaryKeyExpression.GetValue(entity) != default)
        from stored in Trackr.ToPool("Entity", () => entity)
        select Case.Closed;

    private CheckrOf<Case> ReadCheckr(PoolElement<TEntity> info, IPersistenceScope scope) =>
        from entity in Checkr.Act("Read", () =>
            scope.GetById<TEntity>(primaryKeyExpression.GetValue(info.Value)))
        from canRead in Checkr.Expect($"{entityName} Can Read",
            () => propertyChecks.All(a => a(info.Value, entity)))
        select Case.Closed;

    private CheckrOf<Case> UpdateCheckr(IPersistenceScope scope, PoolElement<TEntity> info) =>
        from entity in Checkr.Capture(() => scope.GetById<TEntity>(primaryKeyExpression.GetValue(info.Value)))
        from updatedEntity in Checkr.Input("Updated Entity", Modifier(entity))
        from updated in Checkr.Act("Update", scope.Commit)
        from reloaded in Checkr.Capture(
            () => scope.GetById<TEntity>(primaryKeyExpression.GetValue(info.Value)))
        from canUpdate in Checkr.Expect(
            $"{entityName} Can Update",
                () => propertyChecks.All(a => a(updatedEntity, reloaded)),
                () => Introduce.This((updatedEntity, reloaded)))
        from stored in info.Replace(reloaded)
        select Case.Closed;

    private CheckrOf<Case> DeleteCheckr(IPersistenceScope scope, PoolElement<TEntity> info) =>
        from delete in Checkr.Act("Delete",
            () =>
            {
                scope.DeleteById<TEntity>(primaryKeyExpression.GetValue(info.Value));
                scope.Commit();
            })
        from reloaded in Checkr.Capture(
            () => scope.GetById<TEntity>(primaryKeyExpression.GetValue(info.Value)))
        from canDelete in Checkr.Expect($"{entityName} Can Delete", () => reloaded is null)
        from stored in info.Remove()
        select Case.Closed;
}