using QuickFuzzr;

namespace QuickPersistr;

public abstract class EntityPersister<T>
{
    public abstract FuzzrOf<T> Valid { get; }
}
