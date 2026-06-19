using System.Linq.Expressions;
using System.Reflection;
using QuickCheckr;
using QuickFuzzr;

namespace QuickPersistr.UnderTheHood;

public class PersistenceProperties<TReader, TEntity, TId>(PropertyInfo primaryKeyPropertyInfo)
where TEntity : class, new()
{
    private readonly List<Func<TEntity, TEntity, bool>> propertyChecks = [];
    public PersistenceProperties<TReader, TEntity, TId> Property<TProp>(Expression<Func<TEntity, TProp>> propertyExpression)
    {
        var propertyInfo = propertyExpression.AsPropertyInfo();
        propertyChecks.Add((a, b) => Equals(propertyInfo.GetValue(a), propertyInfo.GetValue(b)));
        return this;
    }

    private readonly List<Func<IPersistenceScope<TReader>, PoolElement<TEntity>, CheckrOf<Case>>> oneToManies = [];
    public PersistenceProperties<TReader, TEntity, TId> HasMany<TChild>(
        Persistence<TReader, TChild> childPersistence,
        Action<TEntity, TChild> apply,
        Func<TEntity, TChild, bool> check,
        Func<IPersistenceReader<TReader>, TId, TEntity> reload,
        Action<TEntity> clear,
        Func<TEntity, bool> checkCleared)
    where TChild : class, new()
    {
        PoolElement<TEntity> el = new(1, null);
        var childSpecification = childPersistence.Define();
        oneToManies.Add(
            (a, b) => GetHasManyCheckr(
                b, apply, check, reload, clear, checkCleared, childSpecification.GetCreator<TChild>(), a));
        return this;
    }

    public CheckrOf<Case> GetHasManyCheckr<T, TChild>(
        PoolElement<T> info,
        Action<T, TChild> apply,
        Func<T, TChild, bool> check,
        Func<IPersistenceReader<TReader>, TId, T> reload,
        Action<T> clear,
        Func<T, bool> checkCleared,
        FuzzrOf<TChild> childFuzzr,
        IPersistenceScope<TReader> scope)
    where T : class, new()
    {
        var entityName = typeof(T).Name;
        var childEntityName = typeof(TChild).Name;
        return
            from id in Checkr.Capture(() => (TId)primaryKeyPropertyInfo.GetValue(info.Value)!)
            from entity in Checkr.Capture(() => scope.GetById<T>(id))
            from children in Checkr.Input("Children", childFuzzr.Many(1, 3))
            from updated in Checkr.Act("Update", () =>
            {
                foreach (var child in children)
                {
                    apply(entity, child);
                }
                scope.Commit();
            })
            from reloaded in Checkr.Capture(
                () => reload(scope.Reader, id))
            from canUpdate in Checkr.Expect($"{entityName} Can Add {childEntityName}", () => children.All(a => check(reloaded, a)))
            from clearMany in Checkr.Act("Clear Many", () => { clear(reloaded); scope.Commit(); })
            from reloadedCleared in Checkr.Capture(
                () => reload(scope.Reader, id))
            from cleared in Checkr.Expect($"{entityName} Can Clear {childEntityName}", () => checkCleared(reloadedCleared))
            from stored in info.Replace(reloaded)
            select Case.Closed;
    }

    public PersistenceSpecification<TReader, TEntity> Persist()
        => new(primaryKeyPropertyInfo, propertyChecks, oneToManies);
}
