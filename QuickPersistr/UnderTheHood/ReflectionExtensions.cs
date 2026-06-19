using System.Linq.Expressions;
using System.Reflection;

namespace QuickPersistr.UnderTheHood;

public static class ReflectionExtensions
{
    public static PropertyInfo AsPropertyInfo<TEntity, TProp>(this Expression<Func<TEntity, TProp>> expression)
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