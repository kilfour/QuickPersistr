using QuickCheckr;
using QuickFuzzr;

namespace QuickPersistr;

public interface IPersistenceSpecification
{
    int CheckrCount { get; }
    public FuzzrOf<T> GetCreator<T>() where T : class, new();
    public IList<CheckrOf<Case>> ToCheckrs(IPersistenceScope scope);
}
