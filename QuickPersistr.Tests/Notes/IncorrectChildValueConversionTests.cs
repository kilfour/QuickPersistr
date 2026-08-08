using Microsoft.EntityFrameworkCore;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickFuzzr;

namespace QuickPersistr.Tests.Notes;

public class IncorrectChildValueConversionTests
    : PersistrTest<IncorrectChildValueConversionTests>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => false;
    protected override bool Explain => false;

    [Fact]
    public override void Example() => Document();

    protected override void GetPersistr(Journalist journalist) =>
        Persistr
            .Named("Incorrect child value conversion")
            .DomainConfiguration(Configr.Combine(
                Configr<Shelf>.Ignore(shelf => shelf.Volumes),
                Configr<Volume>.Construct(
                    Fuzzr.OneOf(new[] { Binding.Hardcover }))))
            .Scope(() => new LibraryScope())
            .Entities(new ShelfPersistence())
            .StoreCaseFiles(journalist)
            .Run();

    protected override void Verify(Article article)
    {
        Assert.Equal("Can Read Volume.Binding", article.FailureDescription());
        Assert.Equal(
            "Expected: Hardcover",
            article.FailingExpectationMessages()[0]);
        Assert.Equal(
            "Actual:   Paperback",
            article.FailingExpectationMessages()[1]);

        var labels = Enumerable.Range(1, article.Total().PassedExpectations())
            .Select(index => article.PassedExpectation(index).Read().Label)
            .ToList();

        Assert.Contains("Shelf Can Add Volume", labels);
        Assert.Contains("Can Create Volume", labels);
        Assert.Contains("Can Read Volume.Id", labels);
    }
}

public class Shelf
{
    public int Id { get; set; }
    public List<Volume> Volumes { get; } = [];
}

public class Volume(Binding binding)
{
    public int Id { get; set; }
    public int ShelfId { get; set; }
    public Binding Binding { get; private set; } = binding;
}

public enum Binding
{
    Paperback,
    Hardcover
}

public class ShelfPersistence : Persistence<LibraryDbContext, Shelf>
{
    public override IPersistenceSpecification<LibraryDbContext> Define() =>
        Entity
            .PrimaryKey(shelf => shelf.Id)
            .HasMany(many => many
                .From(new VolumePersistence())
                .Add((shelf, volume) => shelf.Volumes.Add(volume))
                .Reload((reader, id) => reader.Query(db =>
                    db.Set<Shelf>()
                        .Include(shelf => shelf.Volumes)
                        .Single(shelf => shelf.Id == id)))
                .Contains((shelf, volume) => shelf.Volumes.Any(
                    candidate => candidate.Id == volume.Id)))
            .Persist();
}

public class VolumePersistence : Persistence<LibraryDbContext, Volume>
{
    public override IPersistenceSpecification<LibraryDbContext> Define() =>
        Entity
            .PrimaryKey(volume => volume.Id)
            .Property(volume => volume.Binding)
            .Persist();
}

public class LibraryScope()
    : SqlitePersistenceScope<LibraryDbContext>(options => new LibraryDbContext(options));

public class LibraryDbContext(DbContextOptions<LibraryDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Shelf>()
            .HasMany(shelf => shelf.Volumes)
            .WithOne()
            .HasForeignKey(volume => volume.ShelfId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Volume>()
            .Property(volume => volume.Binding)
            // Bug: Hardcover is accidentally stored using the Paperback representation.
            .HasConversion(
                _ => nameof(Binding.Paperback),
                stored => Enum.Parse<Binding>(stored));
    }
}
