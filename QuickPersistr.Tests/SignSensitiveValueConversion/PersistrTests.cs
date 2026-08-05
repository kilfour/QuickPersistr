using Microsoft.EntityFrameworkCore;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickFuzzr;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.SignSensitiveValueConversion;

[DocFile]
public class PersistrTests : PersistrTest<PersistrTests>
{
    protected override bool Asserts => false;
    protected override bool PassedExpectationsContains => false;
    protected override bool Report => false;
    protected override bool Explain => false;

    [Fact]
    public override void Example() => Document();

    [CodeSnippet]
    [CodeRemove(".StoreCaseFiles(journalist)")]
    protected override void GetPersistr(Journalist journalist)
    {
        Persistr
            .Named("Sign-sensitive value conversion")
            .DomainConfiguration(
                Configr.Primitive(Fuzzr.Decimal(-100m, 100m, 2)))
            .Scope(() => new LedgerScope())
            .Entities(new LedgerEntryPersistence())
            .StoreCaseFiles(journalist)
            .Run(907638377);
    }

    protected override void Verify(Article article)
    {
        Assert.Equal("Can Read LedgerEntry.Amount", article.FailureDescription());
        Assert.Equal("Expected: -99.89", article.FailingExpectationMessages()[0]);
        Assert.Equal("Actual:   99.89", article.FailingExpectationMessages()[1]);
        Assert.Equal("", article.VerifyFailed());
        Assert.Equal(2, article.Total().Executions());
        Assert.Equal(2, article.Total().Actions());
        Assert.Equal(1, article.Total().Inputs());
        Assert.Equal(2, article.Total().PoolTraces());
        Assert.Equal(2, article.Total().PassedExpectations());
        Assert.Equal(3, article.ShrinkCount);
        Assert.Equal(1, article.Execution(1).Read().ExecutionId);
        Assert.Equal("Create LedgerEntry", article.Execution(1).Action(1).Read().Label);
        Assert.Equal("Entity", article.Execution(1).Input(1).Read().Label);
        Assert.Equal("{ Amount: -99.89 }", article.Execution(1).Input(1).Read().Value);
        Assert.Equal("{ Amount: -0.01 }", article.Execution(1).Input(1).Read().Redux.Value);
        Assert.Equal("Entity", article.Execution(1).PoolTrace(1).Read().Label);
        Assert.Equal("LedgerEntry-1", article.Execution(1).PoolTrace(1).Read().Value);
        Assert.Equal(2, article.Execution(2).Read().ExecutionId);
        Assert.Equal("Read LedgerEntry", article.Execution(2).Action(1).Read().Label);
        Assert.Equal("Entity", article.Execution(2).PoolTrace(1).Read().Label);
        Assert.Equal("LedgerEntry-1", article.Execution(2).PoolTrace(1).Read().Value);
        Assert.Equal("Can Create LedgerEntry", article.PassedExpectation(1).Read().Label);
        Assert.Equal(1, article.PassedExpectation(1).Read().TimesPassed);
        Assert.Equal("Can Read LedgerEntry.Id", article.PassedExpectation(2).Read().Label);
        Assert.Equal(1, article.PassedExpectation(2).Read().TimesPassed);
    }
}

public class LedgerEntry
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
}

public class LedgerEntryPersistence : Persistence<LedgerDbContext, LedgerEntry>
{
    public override IPersistenceSpecification<LedgerDbContext> Define() =>
        Entity
            .PrimaryKey(entry => entry.Id)
            .Property(entry => entry.Amount)
            .Persist();
}

public class LedgerScope()
    : EfPersistenceScope<LedgerDbContext>(options => new LedgerDbContext(options));

public class LedgerDbContext(DbContextOptions<LedgerDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LedgerEntry>()
            .Property(entry => entry.Amount)
            // Bug: debit amounts lose their sign while credits happen to round-trip.
            .HasConversion(
                amount => Math.Abs(amount),
                storedAmount => storedAmount);
    }
}
