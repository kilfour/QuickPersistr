using Microsoft.EntityFrameworkCore;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickFuzzr;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.LossyValueConversion;

[DocFile]
public class LossyValueConversionTests : PersistrTest<LossyValueConversionTests>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => false;
    protected override bool Explain => false;

    [Fact]
    public override void Example() => Document();

    [CodeSnippet]
    [CodeRemove(".StoreCaseFiles(TheJournalist)")]
    protected override void GetPersistr(Journalist journalist)
    {
        var timestampWithMilliseconds =
            new DateTime(2026, 6, 18, 12, 34, 56, 789, DateTimeKind.Utc);

        Persistr
            .Named("Lossy value conversion")
            .DomainConfiguration(
                Configr.Primitive(Fuzzr.Constant(timestampWithMilliseconds)))
            .Scope(() => new AuditScope())
            .Entities(new AuditEntryPersistence())
            .StoreCaseFiles(journalist)
            .Run();
    }

    protected override void Verify(Article article)
    {
        Assert.Equal("Can Read AuditEntry.OccurredAt", article.FailureDescription());
        Assert.Equal("Expected: 18.June(2026).At(26.To(13).Seconds(56, 789))", article.FailingExpectationMessages()[0]);
        Assert.Equal("Actual:   18.June(2026).At(26.To(13).Seconds(56))", article.FailingExpectationMessages()[1]);
        Assert.Equal("", article.VerifyFailed());
        Assert.Equal(2, article.Total().Executions());
        Assert.Equal(2, article.Total().Actions());
        Assert.Equal(1, article.Total().Inputs());
        Assert.Equal(2, article.Total().PoolTraces());
        Assert.Equal(2, article.Total().PassedExpectations());
        Assert.Equal(1, article.ShrinkCount);
        Assert.Equal(1, article.Execution(1).Read().ExecutionId);
        Assert.Equal("Create AuditEntry", article.Execution(1).Action(1).Read().Label);
        Assert.Equal("Entity", article.Execution(1).Input(1).Read().Label);
        Assert.Equal("{ OccurredAt: 18.June(2026).At(26.To(13).Seconds(56, 789)) }", article.Execution(1).Input(1).Read().Value);
        Assert.Equal("Entity", article.Execution(1).PoolTrace(1).Read().Label);
        Assert.Equal("AuditEntry-1", article.Execution(1).PoolTrace(1).Read().Value);
        Assert.Equal(2, article.Execution(2).Read().ExecutionId);
        Assert.Equal("Read AuditEntry", article.Execution(2).Action(1).Read().Label);
        Assert.Equal("Entity", article.Execution(2).PoolTrace(1).Read().Label);
        Assert.Equal("AuditEntry-1", article.Execution(2).PoolTrace(1).Read().Value);
        Assert.Equal("Can Create AuditEntry", article.PassedExpectation(1).Read().Label);
        Assert.Equal(1, article.PassedExpectation(1).Read().TimesPassed);
        Assert.Equal("Can Read AuditEntry.Id", article.PassedExpectation(2).Read().Label);
        Assert.Equal(1, article.PassedExpectation(2).Read().TimesPassed);
    }

}

public class AuditEntry
{
    public int Id { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class AuditEntryPersistence : Persistence<AuditDbContext, AuditEntry>
{
    public override IPersistenceSpecification<AuditDbContext> Define() =>
        Entity
            .PrimaryKey(entry => entry.Id)
            .Property(entry => entry.OccurredAt)
            .Persist();
}

public class AuditScope()
    : EfPersistenceScope<AuditDbContext>(options => new AuditDbContext(options));

public class AuditDbContext(DbContextOptions<AuditDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditEntry>()
            .Property(entry => entry.OccurredAt)
            // Bug: storing whole seconds silently discards sub-second precision.
            .HasConversion(
                timestamp => timestamp.Ticks / TimeSpan.TicksPerSecond,
                wholeSeconds => new DateTime(
                    wholeSeconds * TimeSpan.TicksPerSecond,
                    DateTimeKind.Utc));
    }
}
