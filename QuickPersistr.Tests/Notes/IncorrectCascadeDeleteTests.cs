using Microsoft.EntityFrameworkCore;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickFuzzr;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.Notes;

[DocFile]
public class IncorrectCascadeDeleteTests : PersistrTest<IncorrectCascadeDeleteTests>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => false;
    protected override bool Explain => false;

    [Fact]
    public override void Example() => Document();

    [CodeSnippet]
    [CodeRemove(".StoreCaseFiles(journalist)")]
    protected override void GetPersistr(Journalist journalist) =>
        Persistr
            .Named("Incorrect cascade delete")
            .DomainConfiguration(
                Configr<Blog>.Construct(
                    Fuzzr.One<Post>().Many(1, 3).ToList()))
            .Scope(() => new IncorrectCascadeScope())
            .Entities(new BlogPersistence())
            .StoreCaseFiles(journalist)
            .Run(751926438);

    protected override void Verify(Article article)
    {
        Assert.Equal(
            "DbUpdateException: An error occurred while saving the entity changes. See the inner exception for details.",
            article.FailureDescription());
        Assert.Empty(article.FailingExpectationMessages());
        Assert.Equal("", article.VerifyFailed());
        Assert.Equal(2, article.Total().Executions());
        Assert.Equal(2, article.Total().Actions());
        Assert.Equal(0, article.Total().Inputs());
        Assert.Equal(2, article.Total().PoolTraces());
        Assert.Equal(4, article.Total().PassedExpectations());
        Assert.Equal(5, article.ShrinkCount);
        Assert.Equal(1, article.Execution(1).Read().ExecutionId);
        Assert.Equal("Create Blog", article.Execution(1).Action(1).Read().Label);
        Assert.Equal("Entity", article.Execution(1).PoolTrace(1).Read().Label);
        Assert.Equal("Blog-1", article.Execution(1).PoolTrace(1).Read().Value);
        Assert.Equal(4, article.Execution(2).Read().ExecutionId);
        Assert.Equal("Delete Blog", article.Execution(2).Action(1).Read().Label);
        Assert.Equal("Entity", article.Execution(2).PoolTrace(1).Read().Label);
        Assert.Equal("Blog-1", article.Execution(2).PoolTrace(1).Read().Value);
        Assert.Equal("Can Create Blog", article.PassedExpectation(1).Read().Label);
        Assert.Equal("Can Read Blog.Id", article.PassedExpectation(2).Read().Label);
        Assert.Equal("Can Read Blog.Name", article.PassedExpectation(3).Read().Label);
        Assert.Equal("Can Update Blog.Name", article.PassedExpectation(4).Read().Label);
        Assert.All(
            Enumerable.Range(1, 4),
            index => Assert.Equal(1, article.PassedExpectation(index).Read().TimesPassed));
    }
}

public class Blog(List<Post> posts)
{
    private Blog() : this([]) { }

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Post> Posts { get; } = posts;
}

public class Post
{
    public int Id { get; set; }
    public int BlogId { get; set; }
    public string Title { get; set; } = string.Empty;
}

public class BlogPersistence : Persistence<IncorrectCascadeDbContext, Blog>
{
    public override IPersistenceSpecification<IncorrectCascadeDbContext> Define() =>
        Entity
            .PrimaryKey(blog => blog.Id)
            .Property(blog => blog.Name)
            .Persist();
}

public class IncorrectCascadeScope()
    : SqlitePersistenceScope<IncorrectCascadeDbContext>(options => new IncorrectCascadeDbContext(options));

public class IncorrectCascadeDbContext(DbContextOptions<IncorrectCascadeDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blog>()
            .HasMany(blog => blog.Posts)
            .WithOne()
            .HasForeignKey(post => post.BlogId)
            // Bug: deleting a Blog leaves required Posts blocking the delete.
            .OnDelete(DeleteBehavior.Restrict);
    }
}
