using QuickCheckr;
using QuickFuzzr;

namespace QuickPersistr;

public interface IPersistenceSpecification
{
    int CheckrCount { get; }
    public CheckrOf<Case> GetHasManyCheckr<T, TChild>(
        PoolElement<T> info,
        Action<T, TChild> apply,
        Func<T, TChild, bool> check,
        FuzzrOf<TChild> childFuzzr,
        IPersistenceScope scope)
    where T : class, new();
    public IList<CheckrOf<Case>> ToCheckrs(IPersistenceScope scope);
}
