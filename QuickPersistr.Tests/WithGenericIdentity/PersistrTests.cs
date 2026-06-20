using Microsoft.EntityFrameworkCore;


namespace QuickPersistr.Tests.WithGenericIdentity;

public class GenericIdentityDbContext(DbContextOptions<GenericIdentityDbContext> options)
    : DbContext(options)
{
    public DbSet<Thingamajig> Thingamajigs => Set<Thingamajig>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Thingamajig>(builder =>
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).HasConversion(id => id.Value, value => new Id<Thingamajig>(value));
        });
    }

};

public class GenericIdentityScope()
    : EfPersistenceScope<GenericIdentityDbContext>(a => new(a));


public class PersistrTests
{
    [Fact]
    public void FromClass()
    {
        Persistr
            .Named("BackToSchool")
            .Scope(() => new GenericIdentityScope())
            .Entities(new ThingamajigPersistence())
            .Run();
    }

    public class ThingamajigPersistence : Persistence<GenericIdentityDbContext, Thingamajig>
    {
        public override IPersistenceSpecification<GenericIdentityDbContext> Define() =>
            Entity
                .PrimaryKey(a => a.Id)
                .Property(a => a.Description)
                .Persist();
    }
}
