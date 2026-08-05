namespace QuickPersistr.Tests.WithGenericIdentity;

public class GenericIdentityScope()
    : EfPersistenceScope<GenericIdentityDbContext>(a => new(a));
