using Microsoft.EntityFrameworkCore;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.CompositeIdentity;

public class PersistrTests : PersistrTest<PersistrTests>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => false;
    protected override bool Explain => false;

    [Fact]
    public override void Example() => Document();

    protected override void GetPersistr(Journalist journalist) =>
        Persistr
            .Named("Composite identity")
            .Scope(() => new CatalogScope())
            .Entities(new CatalogItemPersistence())
            .StoreCaseFiles(journalist)
            .Run();

    protected override void Verify(Article article)
    {
        Assert.Equal("", article.FailureDescription());
        Assert.Equal("", article.VerifyFailed());
        Assert.Equal(7, article.Total().PassedExpectations());
        Assert.Equal("Can Create CatalogItem", article.PassedExpectation(1).Read().Label);
        Assert.Equal(
            "Can Read CatalogItem.(CatalogId, ItemCode)",
            article.PassedExpectation(2).Read().Label);
        Assert.Equal(
            "Can Read CatalogItem.Description",
            article.PassedExpectation(3).Read().Label);
        Assert.Equal(
            "Can Update CatalogItem.Description",
            article.PassedExpectation(4).Read().Label);
        Assert.Equal("Can Delete CatalogItem", article.PassedExpectation(5).Read().Label);
        Assert.Equal(
            "Can Create Several CatalogItem",
            article.PassedExpectation(6).Read().Label);
        Assert.Equal(
            "Can Create Unique CatalogItem.(CatalogId, ItemCode)",
            article.PassedExpectation(7).Read().Label);
        Assert.All(
            Enumerable.Range(1, 7),
            index => Assert.Equal(1, article.PassedExpectation(index).Read().TimesPassed));
    }
}

public class CatalogItem
{
    public Guid CatalogId { get; set; } = Guid.NewGuid();
    public string ItemCode { get; set; } = Guid.NewGuid().ToString("N");
    public string Description { get; set; } = string.Empty;
}

public class CatalogItemPersistence : Persistence<CatalogDbContext, CatalogItem>
{
    public override IPersistenceSpecification<CatalogDbContext> Define() =>
        Entity
            .PrimaryKey(item => item.CatalogId, item => item.ItemCode)
            .Property(item => item.Description)
            .Persist();
}

public class CatalogScope()
    : EfPersistenceScope<CatalogDbContext>(options => new CatalogDbContext(options));

public class CatalogDbContext(DbContextOptions<CatalogDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<CatalogItem>()
            .HasKey(item => new { item.CatalogId, item.ItemCode });
}
