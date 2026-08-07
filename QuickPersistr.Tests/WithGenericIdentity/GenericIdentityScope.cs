namespace QuickPersistr.Tests.WithGenericIdentity;

public class GenericIdentityScope()
    : SqlitePersistenceScope<GenericIdentityDbContext>(a => new(a));
