using System.Linq.Expressions;
using System.Reflection;

namespace QuickPersistr.UnderTheHood;

public class PersistenceProperties<TEntity, TId>(Expression<Func<TEntity, TId>> primaryKeyExpression)
where TEntity : class, new()
{
    private readonly List<Func<TEntity, TEntity, bool>> propertyChecks = [];
    public PersistenceProperties<TEntity, TId> Property<TProp>(Expression<Func<TEntity, TProp>> propertyExpression)
    {
        var propertyInfo = AsPropertyInfo(propertyExpression);
        propertyChecks.Add((a, b) => Equals(propertyInfo.GetValue(a), propertyInfo.GetValue(b)));
        return this;
    }
    public PersistenceProperties<TEntity, TId> HasMany<TChild>(Persistence<TChild> childSpecification, Action<TEntity, TChild> apply)
    where TChild : class, new()
    {
        return this;
    }
    public PersistenceSpecification<TEntity> Persist()
        => new(AsPropertyInfo(primaryKeyExpression), propertyChecks);

    private static PropertyInfo AsPropertyInfo<TProp>(Expression<Func<TEntity, TProp>> expression)
    {
        if (AsMemberInfo(expression) is PropertyInfo propertyInfo)
            return propertyInfo;
        throw new ArgumentException($"Expression '{expression}' does not refer to a field or property.");
    }

    private static MemberInfo AsMemberInfo<TTarget, TMember>(Expression<Func<TTarget, TMember>> expression)
    {
        if (expression.Body is MemberExpression memberExpr)
        {
            return memberExpr.Member;
        }

        if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
        {
            return unaryMember.Member;
        }

        throw new ArgumentException($"Expression '{expression}' does not refer to a field or property.");
    }
}
