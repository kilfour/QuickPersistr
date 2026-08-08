using Microsoft.EntityFrameworkCore;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;

namespace QuickPersistr.Tests.Notes;

public class RejectedOperationTests : PersistrTest<RejectedOperationTests>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => false;
    protected override bool Explain => false;

    [Fact]
    public override void Example() => Document();

    protected override void GetPersistr(Journalist journalist) =>
        Persistr
            .Named("Rejected persistence operations")
            .Scope(() => new DocumentScope())
            .Entities(new DocumentPersistence())
            .StoreCaseFiles(journalist)
            .Run();

    protected override void Verify(Article article)
    {
        Assert.Equal("", article.FailureDescription());
        Assert.Equal("", article.VerifyFailed());
        Assert.Equal(15, article.Total().PassedExpectations());
        Assert.Equal(
            "Rejects Creating Document: invalid document",
            article.PassedExpectation(5).Read().Label);
        Assert.Equal(
            "Rejected Create Leaves Document Absent",
            article.PassedExpectation(6).Read().Label);
        Assert.Equal(
            "Rejects Updating Document: invalid change",
            article.PassedExpectation(7).Read().Label);
        Assert.Equal(
            "Rejected Update Preserves Document.Id",
            article.PassedExpectation(8).Read().Label);
        Assert.Equal(
            "Rejected Update Preserves Document.Title",
            article.PassedExpectation(9).Read().Label);
        Assert.Equal(
            "Rejects Deleting Document: protected document",
            article.PassedExpectation(10).Read().Label);
        Assert.Equal(
            "Rejected Delete Preserves Document.Id",
            article.PassedExpectation(11).Read().Label);
        Assert.Equal(
            "Rejected Delete Preserves Document.Title",
            article.PassedExpectation(12).Read().Label);
        Assert.All(
            Enumerable.Range(1, 15),
            index => Assert.Equal(1, article.PassedExpectation(index).Read().TimesPassed));
    }
}

public sealed class Document
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;

    public void Reject(string attemptedTitle)
    {
        Title = attemptedTitle;
        throw new PersistenceRejectedException();
    }
}

public sealed class PersistenceRejectedException : Exception;

public class DocumentPersistence : Persistence<DocumentDbContext, Document>
{
    public override IPersistenceSpecification<DocumentDbContext> Define() =>
        Entity
            .PrimaryKey(document => document.Id)
            .Property(document => document.Title)
            .CreateRejected<PersistenceRejectedException>(
                "invalid document",
                document => document.Reject("leaked create"))
            .UpdateRejected<PersistenceRejectedException>(
                "invalid change",
                document => document.Reject("leaked update"))
            .DeleteRejected<PersistenceRejectedException>(
                "protected document",
                document => document.Reject("leaked delete"))
            .Persist();
}

public class DocumentScope()
    : SqlitePersistenceScope<DocumentDbContext>(options => new DocumentDbContext(options));

public class DocumentDbContext(DbContextOptions<DocumentDbContext> options)
    : DbContext(options)
{
    public DbSet<Document> Documents => Set<Document>();
}
