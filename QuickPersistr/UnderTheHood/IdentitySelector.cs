using System.Reflection;

namespace QuickPersistr.UnderTheHood;

public sealed class IdentitySelector<TEntity, TId>(
    IReadOnlyList<PropertyInfo> properties,
    Func<TEntity, TId> select,
    Func<TId, bool> isNonDefault,
    IEqualityComparer<TId> comparer)
where TEntity : class
{
    public IReadOnlyList<PropertyInfo> Properties { get; } = [.. properties];
    public IEqualityComparer<TId> Comparer { get; } = comparer;

    public TId Select(TEntity entity) => select(entity);

    public bool IsNonDefault(TId identity) => isNonDefault(identity);

    public string QualifiedName(string entityName) =>
        Properties.Count == 1
            ? $"{entityName}.{Properties[0].Name}"
            : $"{entityName}.({string.Join(", ", Properties.Select(property => property.Name))})";

    public T GetById<T>(IPersistenceScope scope, TId identity)
    where T : class
    {
        if (Properties.Count > 1)
        {
            return scope.GetById<T>((object?[])(object)identity!);
        }

        return scope.GetById<T>(identity);
    }

    public void DeleteById<T>(IPersistenceScope scope, TId identity)
    where T : class
    {
        if (Properties.Count > 1)
        {
            scope.DeleteById<T>((object?[])(object)identity!);
            return;
        }

        scope.DeleteById<T>(identity);
    }
}
