using Microsoft.EntityFrameworkCore;

namespace QuickPersistr.Tests.FreshSession;

public class EfPersistenceScopeTests
{
    [Fact]
    public void StartsANewSessionWithoutLosingPersistedState()
    {
        using var scope = new EfPersistenceScope<FreshSessionDbContext>(
            options => new FreshSessionDbContext(options));
        var entity = scope.Add(new FreshSessionEntity { Name = "Persisted" });
        scope.Commit();
        var firstContext = scope.Reader.Query(context => context);

        entity.Name = "Only changed in the tracked instance";
        scope.StartNewSession();

        var secondContext = scope.Reader.Query(context => context);
        var reloaded = scope.GetById<FreshSessionEntity>(entity.Id);

        Assert.NotSame(firstContext, secondContext);
        Assert.Equal("Persisted", reloaded.Name);
    }
}

public class FreshSessionEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class FreshSessionDbContext(DbContextOptions<FreshSessionDbContext> options)
    : DbContext(options)
{
    public DbSet<FreshSessionEntity> Entities => Set<FreshSessionEntity>();
}
