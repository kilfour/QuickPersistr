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
