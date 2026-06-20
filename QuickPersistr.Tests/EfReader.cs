namespace QuickPersistr.Tests;

public class EfReader<TDbContext>(TDbContext context) : IPersistenceReader<TDbContext>
{
    public TEntity Query<TEntity>(Func<TDbContext, TEntity> query)
        => query(context);
}
