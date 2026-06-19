using System.Reflection;
using QuickCheckr;
using QuickFuzzr;
using QuickPulse.Show;

namespace QuickPersistr.UnderTheHood;

public class PersistenceSpecification<TEntity>(
PropertyInfo primaryKeyExpression,
List<Func<TEntity, TEntity, bool>> propertyChecks) : IPersistenceSpecification
where TEntity : class, new()
{
    public CheckrOf<Case> GetCheckr(Func<IPersistenceScope> scopeFactory) =>
        from db in Trackr.Stashed(scopeFactory)
        from _ in Checkr.Sequence(
            CreateCheckr(db),
            Trackr.OneOfPool<TEntity>("Read", info => ReadCheckr(info, db)),
            Trackr.OneOfPool<TEntity>("Update", info => UpdateCheckr(db, info)),
            Trackr.OneOfPool<TEntity>("Delete", info => DeleteCheckr(db, info)))
        select Case.Closed;

    private readonly FuzzrOf<TEntity> Creator =
        from ignore in Configr.Ignore(a => a == primaryKeyExpression)
        from entity in Fuzzr.One<TEntity>()
        select entity;

    private FuzzrOf<TEntity> Modifier(TEntity course) =>
        from ignore in Configr.Ignore(a => a == primaryKeyExpression)
        from entity in Fuzzr.One(() => course)
        select entity;

    private CheckrOf<Case> CreateCheckr(IPersistenceScope db) =>
        from entity in Checkr.Input("Entity", Creator)
        from create in Checkr.Act("Create", () => { db.Add(entity); db.Commit(); })
        from canCreate in Checkr.Expect("Can Create", () => primaryKeyExpression.GetValue(entity) != default)
        from stored in Trackr.ToPool("Entity", () => entity)
        select Case.Closed;

    private CheckrOf<Case> ReadCheckr(PoolElement<TEntity> info, IPersistenceScope db) =>
        from entity in Checkr.Act("Read", () =>
            db.GetById<TEntity>(primaryKeyExpression.GetValue(info.Value)))
        from canRead in Checkr.Expect("Can Read",
            () => propertyChecks.All(a => a(info.Value, entity)))
        select Case.Closed;

    private CheckrOf<Case> UpdateCheckr(IPersistenceScope db, PoolElement<TEntity> info) =>
        from entity in Checkr.Capture(() => db.GetById<TEntity>(primaryKeyExpression.GetValue(info.Value)))
        from updatedEntity in Checkr.Input("Updated Entity", Modifier(entity))
        from updated in Checkr.Act("Update", db.Commit)
        from reloaded in Checkr.Capture(
            () => db.GetById<TEntity>(primaryKeyExpression.GetValue(info.Value)))
        from canUpdate in Checkr.Expect(
            "Can Update",
                () => propertyChecks.All(a => a(updatedEntity, reloaded)),
                () => Introduce.This((updatedEntity, reloaded)))
        from stored in info.Replace(reloaded)
        select Case.Closed;

    private CheckrOf<Case> DeleteCheckr(IPersistenceScope db, PoolElement<TEntity> info) =>
        from delete in Checkr.Act("Delete",
            () =>
            {
                db.DeleteById<TEntity>(primaryKeyExpression.GetValue(info.Value));
                db.Commit();
            })
        from reloaded in Checkr.Capture(
            () => db.GetById<TEntity>(primaryKeyExpression.GetValue(info.Value)))
        from canDelete in Checkr.Expect("Can Delete", () => reloaded is null)
        from stored in info.Remove()
        select Case.Closed;

}