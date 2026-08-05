using System.Linq.Expressions;

namespace QuickPersistr.UnderTheHood;

public class PersistencePrimaryKey<TReader, TEntity>
where TEntity : class
{
    public PersistenceProperties<TReader, TEntity, TId> PrimaryKey<TId>(
        Expression<Func<TEntity, TId>> primaryKeyExpression)
    {
        ArgumentNullException.ThrowIfNull(primaryKeyExpression);

        var property = primaryKeyExpression.AsPropertyInfo();
        return new(new IdentitySelector<TEntity, TId>(
            [property],
            primaryKeyExpression.Compile(),
            identity => IsNonDefault(identity, property.PropertyType),
            EqualityComparer<TId>.Default));
    }

    public PersistenceProperties<TReader, TEntity, object?[]> PrimaryKey(
        params Expression<Func<TEntity, object?>>[] identitySelectors)
    {
        ArgumentNullException.ThrowIfNull(identitySelectors);
        if (identitySelectors.Length < 2)
        {
            throw new ArgumentException(
                "A composite identity requires at least two selectors.",
                nameof(identitySelectors));
        }

        if (identitySelectors.Any(selector => selector is null))
        {
            throw new ArgumentException(
                "Composite identity selectors cannot contain null.",
                nameof(identitySelectors));
        }

        var properties = identitySelectors
            .Select(selector => selector.AsPropertyInfo())
            .ToList();
        var selectors = identitySelectors
            .Select(selector => selector.Compile())
            .ToList();

        return new(new IdentitySelector<TEntity, object?[]>(
            properties,
            entity => selectors.Select(selector => selector(entity)).ToArray(),
            identity => identity.Length == properties.Count &&
                identity.Select((value, index) => IsNonDefault(value, properties[index].PropertyType)).All(value => value),
            new CompositeIdentityComparer()));
    }

    private static bool IsNonDefault(object? value, Type type)
    {
        var defaultValue = type.IsValueType
            ? Activator.CreateInstance(type)
            : null;
        return !Equals(value, defaultValue);
    }

    private sealed class CompositeIdentityComparer : IEqualityComparer<object?[]>
    {
        public bool Equals(object?[]? left, object?[]? right)
        {
            if (ReferenceEquals(left, right))
                return true;

            return left is not null &&
                right is not null &&
                left.SequenceEqual(right);
        }

        public int GetHashCode(object?[] identity)
        {
            var hash = new HashCode();
            foreach (var component in identity)
            {
                hash.Add(component);
            }
            return hash.ToHashCode();
        }
    }

}
