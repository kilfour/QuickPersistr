using QuickCheckr;
using QuickFuzzr;

namespace QuickPersistr;

public interface IPersistenceSpecification<TReader>
{
    int CheckrCount { get; }
    public FuzzrOf<T> GetCreator<T>() where T : class;
    public CheckrOf<Case> GetNestedCheckr<T>(
        IPersistenceScope<TReader> scope,
        IReadOnlyList<T> created,
        T active,
        Action<T> add,
        string keyPrefix)
        where T : class;
    public CheckrOf<Case> GetNestedDeleteCheckr<T>(
        IPersistenceScope<TReader> scope,
        T active,
        Action<T> add,
        string keyPrefix)
        where T : class;
    public IList<CheckrOf<Case>> ToCheckrs(IPersistenceScope<TReader> scope);
}
