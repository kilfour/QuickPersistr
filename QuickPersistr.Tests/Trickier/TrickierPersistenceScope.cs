namespace QuickPersistr.Tests.Trickier;

public class TrickierPersistenceScope : SqlitePersistenceScope<TrickierDbContext>
{
    public TrickierPersistenceScope() : base(a => new TrickierDbContext(a)) { }
}
