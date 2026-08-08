using Microsoft.EntityFrameworkCore;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickFuzzr;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.FullChildContract;

public class FullChildContractTests : PersistrTest<FullChildContractTests>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => false;
    protected override bool Explain => false;

    [Fact]
    public override void Example() => Document();

    protected override void GetPersistr(Journalist journalist) =>
        Persistr
            .Named("Full child contract")
            .DomainConfiguration(Configr.Combine(
                Configr<Catalog>.Ignore(catalog => catalog.Items),
                Configr<Item>.Construct(
                    Fuzzr.String(1, 12),
                    Fuzzr.OneOf(new[] { 1 })),
                Configr<Item>.Ignore(item => item.Notes),
                Configr<Note>.Construct(Fuzzr.String(1, 12))))
            .Scope(() => new CatalogScope())
            .Entities(new CatalogPersistence())
            .StoreCaseFiles(journalist)
            .Run(174932);

    protected override void Verify(Article article)
    {
        Assert.Equal("", article.FailureDescription());
        Assert.Equal("", article.VerifyFailed());

        var labels = Enumerable.Range(1, article.Total().PassedExpectations())
            .Select(index => article.PassedExpectation(index).Read().Label)
            .ToList();

        Assert.Contains("Can Create Several Item", labels);
        Assert.Contains("Can Create Unique Item.Id", labels);
        Assert.Contains("Rename Persists Item.Name", labels);
        Assert.Contains("Rejects Stale Item Update: Publish / Archive", labels);
        Assert.Contains("Rejects Creating Item: invalid item", labels);
        Assert.Contains("Rejected Create Leaves Item Absent", labels);
        Assert.Contains("Rejects Updating Item: invalid change", labels);
        Assert.Contains("Rejected Update Preserves Item.Name", labels);
        Assert.Contains("Rejects Deleting Item: protected item", labels);
        Assert.Contains("Rejected Delete Preserves Item.Name", labels);
        Assert.Contains("Item Can Add Note", labels);
        Assert.Contains("Can Create Note", labels);
        Assert.Contains("Can Delete Note", labels);
        Assert.Contains("Can Delete Item", labels);
        Assert.Contains("Deleting Item leaves it absent", labels);
    }
}

public class Catalog
{
    public int Id { get; set; }
    public List<Item> Items { get; } = [];
}

public class Item(string name, int validity)
{
    public int Id { get; set; }
    public int CatalogId { get; set; }
    public string Name { get; private set; } = name;
    public int Validity { get; private set; } = validity;
    public int Version { get; private set; }
    public List<Note> Notes { get; } = [];

    public void Rename()
    {
        Name += "!";
        Version++;
    }

    public void Publish()
    {
        Name = "published";
        Version++;
    }

    public void Archive()
    {
        Name = "archived";
        Version++;
    }

    public void Invalidate() => Validity = 0;

    public void RejectDelete() => throw new ProtectedItemException();
}

public class Note(string text)
{
    public int Id { get; set; }
    public int ItemId { get; set; }
    public string Text { get; private set; } = text;
}

public sealed class ProtectedItemException : Exception;

public class CatalogPersistence : Persistence<CatalogDbContext, Catalog>
{
    public override IPersistenceSpecification<CatalogDbContext> Define() =>
        Entity
            .PrimaryKey(catalog => catalog.Id)
            .HasMany(many => many
                .From(new ItemPersistence())
                .Add((catalog, item) => catalog.Items.Add(item))
                .Reload((reader, id) => reader.Query(db =>
                    db.Set<Catalog>()
                        .Include(catalog => catalog.Items)
                        .Single(catalog => catalog.Id == id)))
                .Contains((catalog, item) => catalog.Items.Any(
                    candidate => candidate.Id == item.Id)))
            .Persist();
}

public class ItemPersistence : Persistence<CatalogDbContext, Item>
{
    public override IPersistenceSpecification<CatalogDbContext> Define() =>
        Entity
            .PrimaryKey(item => item.Id)
            .Property(item => item.Name)
            .Property(item => item.Version)
            .Update("Rename", item => item.Rename())
            .OptimisticConcurrency<DbUpdateConcurrencyException>(
                item => item.Publish(),
                item => item.Archive())
            .HasMany(many => many
                .From(new NotePersistence())
                .Add((item, note) => item.Notes.Add(note))
                .Remove((item, note) => item.Notes.RemoveAll(
                    candidate => candidate.Id == note.Id))
                .Clear(item => item.Notes.Clear())
                .Reload((reader, id) => reader.Query(db =>
                    db.Set<Item>()
                        .Include(item => item.Notes)
                        .Single(item => item.Id == id)))
                .Contains((item, note) => item.Notes.Any(
                    candidate => candidate.Id == note.Id))
                .Empty(item => item.Notes.Count == 0))
            .CreateRejected<DbUpdateException>("invalid item", item => item.Invalidate())
            .UpdateRejected<DbUpdateException>("invalid change", item => item.Invalidate())
            .DeleteRejected<ProtectedItemException>("protected item", item => item.RejectDelete())
            .AfterDelete(
                "leaves it absent",
                (reader, item) => !reader.Query(db =>
                    db.Set<Item>().Any(candidate => candidate.Id == item.Id)))
            .Persist();
}

public class NotePersistence : Persistence<CatalogDbContext, Note>
{
    public override IPersistenceSpecification<CatalogDbContext> Define() =>
        Entity
            .PrimaryKey(note => note.Id)
            .Property(note => note.Text)
            .Persist();
}

public class CatalogScope()
    : SqlitePersistenceScope<CatalogDbContext>(options => new CatalogDbContext(options));

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Catalog>()
            .HasMany(catalog => catalog.Items)
            .WithOne()
            .HasForeignKey(item => item.CatalogId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Item>()
            .HasMany(item => item.Notes)
            .WithOne()
            .HasForeignKey(note => note.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Item>()
            .Property(item => item.Version)
            .IsConcurrencyToken();

        modelBuilder.Entity<Item>()
            .ToTable(table => table.HasCheckConstraint(
                "CK_Item_Validity",
                "Validity > 0"));
    }
}
