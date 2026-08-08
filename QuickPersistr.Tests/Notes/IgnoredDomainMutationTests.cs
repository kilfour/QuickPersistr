using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;

namespace QuickPersistr.Tests.Notes;

public class IgnoredDomainMutationTests : PersistrTest<IgnoredDomainMutationTests>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => false;
    protected override bool Explain => false;

    [Fact]
    public override void Example() => Document();

    protected override void GetPersistr(Journalist journalist) =>
        Persistr
            .Named("Ignored domain mutation")
            .Scope(() => new PublicationScope())
            .Entities(new PublicationPersistence())
            .StoreCaseFiles(journalist)
            .Run();

    protected override void Verify(Article article)
    {
        Assert.Equal(
            "ChangeStatus Persists Publication.Status",
            article.FailureDescription());
        Assert.Equal("Expected: Published", article.FailingExpectationMessages()[0]);
        Assert.Equal("Actual:   Draft", article.FailingExpectationMessages()[1]);
        Assert.Equal("", article.VerifyFailed());
        Assert.Equal(5, article.Total().PassedExpectations());
        Assert.Equal(
            "ChangeStatus Preserves Publication.Id",
            article.PassedExpectation(5).Read().Label);
    }


    public enum PublicationStatus
    {
        Draft,
        Published
    }

    public class Publication
    {
        private PublicationStatus status = PublicationStatus.Draft;

        public Guid Id { get; set; } = Guid.NewGuid();
        public PublicationStatus Status => status;

        public void ChangeStatus(PublicationStatus newStatus) =>
            status = newStatus;
    }

    public class PublicationPersistence : Persistence<PublicationDbContext, Publication>
    {
        public override IPersistenceSpecification<PublicationDbContext> Define() =>
            Entity
                .PrimaryKey(publication => publication.Id)
                .Property(publication => publication.Status)
                .Update(publication => publication.ChangeStatus(PublicationStatus.Published))
                .Persist();
    }

    public class PublicationScope()
        : SqlitePersistenceScope<PublicationDbContext>(options => new PublicationDbContext(options));

    public class PublicationDbContext(DbContextOptions<PublicationDbContext> options)
        : DbContext(options)
    {
        public DbSet<Publication> Publications => Set<Publication>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var status = modelBuilder.Entity<Publication>()
                .Property(publication => publication.Status)
                .HasField("status");
            status.Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        }
    }

}
