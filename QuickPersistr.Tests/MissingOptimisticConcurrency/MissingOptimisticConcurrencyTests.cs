using Microsoft.EntityFrameworkCore;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.MissingOptimisticConcurrency;

public class MissingOptimisticConcurrencyTests : PersistrTest<MissingOptimisticConcurrencyTests>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => false;
    protected override bool Explain => false;

    [Fact]
    public override void Example() => Document();

    protected override void GetPersistr(Journalist journalist) =>
        Persistr
            .Named("Missing optimistic concurrency")
            .Scope(() => new PostScope())
            .Entities(new PostPersistence())
            .StoreCaseFiles(journalist)
            .Run();

    protected override void Verify(Article article)
    {
        Assert.Equal(
            "Rejects Stale Post Update: ChangeStatus",
            article.FailureDescription());
        Assert.Equal(
            "Expected: DbUpdateConcurrencyException",
            article.FailingExpectationMessages()[0]);
        Assert.Equal(
            "Actual:   No Exception was thrown.",
            article.FailingExpectationMessages()[1]);
        Assert.Equal("", article.VerifyFailed());
        Assert.Equal(8, article.Total().PassedExpectations());
    }
}

public enum PostStatus
{
    Draft,
    Published,
    Archived
}

public class Post
{
    public int Id { get; private set; }
    public PostStatus Status { get; private set; }
    public int Version { get; private set; }

    public void ChangeStatus(PostStatus status)
    {
        Status = status;
        Version++;
    }
}

public class PostPersistence : Persistence<PostDbContext, Post>
{
    public override IPersistenceSpecification<PostDbContext> Define() =>
        Entity
            .PrimaryKey(post => post.Id)
            .Property(post => post.Status)
            .Property(post => post.Version)
            .OptimisticConcurrency<DbUpdateConcurrencyException>(
                post => post.ChangeStatus(PostStatus.Published),
                post => post.ChangeStatus(PostStatus.Archived))
            .Persist();
}

public class PostScope()
    : SqlitePersistenceScope<PostDbContext>(options => new PostDbContext(options));

public class PostDbContext(DbContextOptions<PostDbContext> options)
    : DbContext(options)
{
    public DbSet<Post> Posts => Set<Post>();
}
