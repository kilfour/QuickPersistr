using Microsoft.EntityFrameworkCore;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickFuzzr;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.HasOne.PrincipalReference;

public class PrincipalReferenceTests : PersistrTest<PrincipalReferenceTests>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => false;
    protected override bool Explain => false;

    [Fact]
    public override void Example() => Document();

    protected override void GetPersistr(Journalist journalist) =>
        Persistr
            .Named("Has one principal reference")
            .DomainConfiguration(Configr.Combine(
                Configr<Talk>.Construct(
                    Fuzzr.String(),
                    Fuzzr.One<Speaker>()),
                Configr<Talk>.Ignore(talk => talk.Speaker),
                Configr<Talk>.Ignore(talk => talk.SpeakerId)))
            .Scope(() => new TalkScope())
            .Entities(new TalkPersistence())
            .StoreCaseFiles(journalist)
            .Run(23745126);

    protected override void Verify(Article article)
    {
        Assert.Equal("", article.FailureDescription());
        Assert.Equal("", article.VerifyFailed());

        var labels = Enumerable.Range(1, article.Total().PassedExpectations())
            .Select(index => article.PassedExpectation(index).Read().Label)
            .ToList();

        Assert.Contains("Talk Can Replace Speaker", labels);
        Assert.Contains("Talk Releases Previous Speaker", labels);
        Assert.Contains("Can Delete Speaker", labels);
    }
}

public class Talk(string subject, Speaker speaker)
{
    private Talk() : this(string.Empty, new()) { }

    public int Id { get; set; }
    public string Subject { get; set; } = subject;
    public int SpeakerId { get; set; }
    public Speaker Speaker { get; set; } = speaker;
}

public class Speaker
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TalkPersistence : Persistence<TalkDbContext, Talk>
{
    public override IPersistenceSpecification<TalkDbContext> Define() =>
        Entity
            .PrimaryKey(talk => talk.Id)
            .Property(talk => talk.Subject)
            .HasOne(one => one
                .From(new SpeakerPersistence())
                .Set((talk, speaker) => talk.Speaker = speaker)
                .Reload((reader, id) => reader.Query(db =>
                    db.Set<Talk>()
                        .Include(talk => talk.Speaker)
                        .Single(talk => talk.Id == id)))
                .Contains((talk, speaker) => talk.Speaker.Id == speaker.Id))
            .Persist();
}

public class SpeakerPersistence : Persistence<TalkDbContext, Speaker>
{
    public override IPersistenceSpecification<TalkDbContext> Define() =>
        Entity
            .PrimaryKey(speaker => speaker.Id)
            .Property(speaker => speaker.Name)
            .Persist();
}

public class TalkScope()
    : SqlitePersistenceScope<TalkDbContext>(options => new TalkDbContext(options));

public class TalkDbContext(DbContextOptions<TalkDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Talk>()
            .HasOne(talk => talk.Speaker)
            .WithMany()
            .HasForeignKey(talk => talk.SpeakerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
