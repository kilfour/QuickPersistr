using QuickCheckr;

namespace QuickPersistr;

public interface IPersistenceSpecification
{
    public IList<CheckrOf<Case>> ToCheckrs(IPersistenceScope scope);
}
