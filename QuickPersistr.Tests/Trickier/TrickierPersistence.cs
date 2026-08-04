namespace QuickPersistr.Tests.Trickier;

public abstract class TrickierPersistence<TEntity>
    : Persistence<TrickierDbContext, TEntity>
    where TEntity : class;
