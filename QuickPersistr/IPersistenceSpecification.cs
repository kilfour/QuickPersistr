using QuickCheckr;
using QuickFuzzr;

namespace QuickPersistr;

public interface IPersistenceSpecification<TReader>
{
    int CheckrCount { get; }
    public FuzzrOf<T> GetCreator<T>() where T : class;
    public IList<CheckrOf<Case>> ToCheckrs(IPersistenceScope<TReader> scope);
}
