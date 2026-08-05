using Microsoft.EntityFrameworkCore;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.OptimisticConcurrency;

public class OptimisticConcurrencyTests : PersistrTest<OptimisticConcurrencyTests>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => false;
    protected override bool Explain => false;

    [Fact]
    public override void Example() => Document();

    protected override void GetPersistr(Journalist journalist) =>
        Persistr
            .Named("Optimistic concurrency")
            .Scope(() => new PostScope())
            .Entities(new PostPersistence())
            .StoreCaseFiles(journalist)
            .Run();

    protected override void Verify(Article article)
    {
        Assert.Equal("", article.FailureDescription());
        Assert.Equal("", article.VerifyFailed());
        Assert.Equal(13, article.Total().PassedExpectations());
        Assert.Equal(
            "Rejects Stale Post Update: ChangeStatus",
            article.PassedExpectation(7).Read().Label);
        Assert.Equal(
            "Concurrent ChangeStatus Preserves Post.Id",
            article.PassedExpectation(8).Read().Label);
        Assert.Equal(
            "Concurrent ChangeStatus Persists Winner Post.Status",
            article.PassedExpectation(9).Read().Label);
        Assert.Equal(
            "Concurrent ChangeStatus Persists Winner Post.Version",
            article.PassedExpectation(10).Read().Label);
        Assert.All(
            Enumerable.Range(1, 13),
            index => Assert.Equal(1, article.PassedExpectation(index).Read().TimesPassed));
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
    : EfPersistenceScope<PostDbContext>(options => new PostDbContext(options));

public class PostDbContext(DbContextOptions<PostDbContext> options)
    : DbContext(options)
{
    public DbSet<Post> Posts => Set<Post>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Post>()
            .Property(post => post.Version)
            .IsConcurrencyToken();
}
