using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using QuickCheckr.Diagnostics;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.Docs.A_GettingStarted.Sub;

[DocFile]
public class A_ASimpleEntity : PersistrTest<A_ASimpleEntity>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => true;
    protected override bool Explain => false;

    [Fact]
    public void Example()
        => Document(PersistIt, _ => { });

    [CodeSnippet]
    [CodeRemove(".StoreCaseFiles(TheJournalist)")]
    private void PersistIt() =>
        Persistr.Named("A Simple Entity")
            .Scope(() => new EfPersistenceScope<Library>(a => new Library(a)))
            .Entities(new BookPersistence()).StoreCaseFiles(TheJournalist)
            .Run();
    //.Autopsy(881300230, AutopsyProbe.Default);
}

public class Library(DbContextOptions<Library> options)
    : DbContext(options)
{
    public DbSet<Book> Courses => Set<Book>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Book>(
            entity => entity.Property(e => e.Description)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore));
    }
}

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class BookPersistence : Persistence<Library, Book>
{
    public override IPersistenceSpecification<Library> Define() =>
        Entity
            .PrimaryKey(a => a.Id)
            .Property(a => a.Title)
            .Property(a => a.Description)
            .Persist();
}