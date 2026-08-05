using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.Docs.A_GettingStarted.Sub;

[DocFile]
public class A_ASimpleEntity : PersistrTest<A_ASimpleEntity>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => false;
    protected override bool Explain => false;

    [Fact]
    public override void Example() => Document();

    [CodeSnippet]
    [CodeRemove(".StoreCaseFiles(TheJournalist)")]
    protected override void GetPersistr(Journalist journalist) =>
        Persistr.Named("A Simple Entity")
            .Scope(() => new EfPersistenceScope<Library>(a => new Library(a)))
            .Entities(new BookPersistence()).StoreCaseFiles(journalist)
            .Run(1383231788);

    protected override void Verify(Article article)
    {
        Assert.Equal("Can Update Book.Description", article.FailureDescription());
        Assert.Equal("Expected: \"dzrqiz\"", article.FailingExpectationMessages()[0]);
        Assert.Equal("Actual:   \"h\"", article.FailingExpectationMessages()[1]);
        Assert.Equal("", article.VerifyFailed());
        Assert.Equal(2, article.Total().Executions());
        Assert.Equal(2, article.Total().Actions());
        Assert.Equal(2, article.Total().PoolTraces());
        Assert.Equal(4, article.Total().PassedExpectations());
        Assert.Equal(9, article.ShrinkCount);
        Assert.Equal(1, article.Execution(1).Read().ExecutionId);
        Assert.Equal("Create Book", article.Execution(1).Action(1).Read().Label);
        Assert.Equal("Entity", article.Execution(1).PoolTrace(1).Read().Label);
        Assert.Equal("Book-1", article.Execution(1).PoolTrace(1).Read().Value);
        Assert.Equal(3, article.Execution(2).Read().ExecutionId);
        Assert.Equal("Update Book", article.Execution(2).Action(1).Read().Label);
        Assert.Equal("Entity", article.Execution(2).PoolTrace(1).Read().Label);
        Assert.Equal("Book-1", article.Execution(2).PoolTrace(1).Read().Value);
        Assert.Equal("Can Create Book", article.PassedExpectation(1).Read().Label);
        Assert.Equal(1, article.PassedExpectation(1).Read().TimesPassed);
        Assert.Equal("Can Read Book.Title", article.PassedExpectation(2).Read().Label);
        Assert.Equal(1, article.PassedExpectation(2).Read().TimesPassed);
        Assert.Equal("Can Read Book.Description", article.PassedExpectation(3).Read().Label);
        Assert.Equal(1, article.PassedExpectation(3).Read().TimesPassed);
        Assert.Equal("Can Update Book.Title", article.PassedExpectation(4).Read().Label);
        Assert.Equal(1, article.PassedExpectation(4).Read().TimesPassed);
    }
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