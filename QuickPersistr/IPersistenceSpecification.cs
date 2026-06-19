using QuickCheckr;

namespace QuickPersistr;

public interface IPersistenceSpecification
{
    public CheckrOf<Case> GetCheckr(Func<IPersistenceScope> scopeFactory);
}
