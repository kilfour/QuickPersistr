using Microsoft.EntityFrameworkCore;

namespace QuickPersistr.Tests.CheckingFailure;

public class PersistrTests
{
    [Fact]
    public void FromClass()
    {
        Persistr
            .Named("CheckingFailure")
            .Scope(() => new FailScope())
            .Entities(new ThingamajigPersistence())
            .Run();
    }
}

public class ThingamajigPersistence : Persistence<FailDbContext, Thingamajig>
{
    public override IPersistenceSpecification<FailDbContext> Define() =>
        Entity
            .PrimaryKey(a => a.Id)
            .Property(a => a.Description)
            .Persist();
}

public class Thingamajig
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class FailScope()
    : EfPersistenceScope<FailDbContext>(a => new(a));

public class FailDbContext(DbContextOptions<FailDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Thingamajig>(builder =>
        {

        });
    }
};