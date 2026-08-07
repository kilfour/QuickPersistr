using Microsoft.EntityFrameworkCore;

namespace QuickPersistr.EntityFrameworkCore;

public sealed class EfPersistenceReader<TDbContext>(TDbContext context)
    : IPersistenceReader<TDbContext>
where TDbContext : DbContext
{
    public TResult Query<TResult>(Func<TDbContext, TResult> query) => query(context);
}
