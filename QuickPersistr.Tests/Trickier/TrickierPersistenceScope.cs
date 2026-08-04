namespace QuickPersistr.Tests.Trickier;

public class TrickierPersistenceScope : EfPersistenceScope<TrickierDbContext>
{
    public TrickierPersistenceScope() : base(a => new TrickierDbContext(a)) { }
}
