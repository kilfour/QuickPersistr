using Microsoft.EntityFrameworkCore;
using QuickCheckr;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickFuzzr;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.Notes;

[DocFile]
public class IncorrectEnumConversionTests : PersistrTest<IncorrectEnumConversionTests>
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
            .Named("Incorrect enum conversion")
            .DomainConfiguration(
                Configr<Payment>.Construct(
                    Fuzzr.OneOf(Enum.GetValues<PaymentStatus>())))
            .Scope(() => new PaymentsScope())
            .Entities(new PaymentPersistence())
            .StoreCaseFiles(journalist)
            .Run(100.Runs());
    }

    protected override void Verify(Article article)
    {
        Assert.Equal("Can Read Payment.Status", article.FailureDescription());
        Assert.Equal("Expected: Refunded", article.FailingExpectationMessages()[0]);
        Assert.Equal("Actual:   Paid", article.FailingExpectationMessages()[1]);
        Assert.Equal("", article.VerifyFailed());
        Assert.NotNull(article.Seed());
        Assert.Equal(2, article.Total().Executions());
        Assert.Equal(2, article.Total().Actions());
        Assert.Equal(1, article.Total().Inputs());
        Assert.Equal(2, article.Total().PoolTraces());
        Assert.Equal(2, article.Total().PassedExpectations());
        Assert.True(article.ShrinkCount is 0 or 1);
        Assert.Equal(1, article.Execution(1).Read().ExecutionId);
        Assert.Equal("Create Payment", article.Execution(1).Action(1).Read().Label);
        Assert.Equal("Entity", article.Execution(1).Input(1).Read().Label);
        Assert.Equal("{ Id: 1, Status: Refunded }", article.Execution(1).Input(1).Read().Value);
        Assert.Equal("Entity", article.Execution(1).PoolTrace(1).Read().Label);
        Assert.Equal("Payment-1", article.Execution(1).PoolTrace(1).Read().Value);
        Assert.Equal(2, article.Execution(2).Read().ExecutionId);
        Assert.Equal("Read Payment", article.Execution(2).Action(1).Read().Label);
        Assert.Equal("Entity", article.Execution(2).PoolTrace(1).Read().Label);
        Assert.Equal("Payment-1", article.Execution(2).PoolTrace(1).Read().Value);
        Assert.Equal("Can Create Payment", article.PassedExpectation(1).Read().Label);
        Assert.Equal(1, article.PassedExpectation(1).Read().TimesPassed);
        Assert.Equal("Can Read Payment.Id", article.PassedExpectation(2).Read().Label);
        Assert.Equal(1, article.PassedExpectation(2).Read().TimesPassed);
    }
}

public enum PaymentStatus
{
    Pending,
    Authorized,
    Paid,
    Refunded,
    Cancelled
}

public class Payment(PaymentStatus status)
{
    public int Id { get; private set; }
    public PaymentStatus Status { get; } = status;
}

public class PaymentPersistence : Persistence<PaymentsDbContext, Payment>
{
    public override IPersistenceSpecification<PaymentsDbContext> Define() =>
        Entity
            .PrimaryKey(payment => payment.Id)
            .Property(payment => payment.Status)
            .Persist();
}

public class PaymentsScope()
    : SqlitePersistenceScope<PaymentsDbContext>(options => new PaymentsDbContext(options));

public class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Payment>()
            .Property(payment => payment.Status)
            // Bug: Refunded is accidentally stored using the Paid representation.
            .HasConversion(
                status => status == PaymentStatus.Refunded
                    ? nameof(PaymentStatus.Paid)
                    : status.ToString(),
                storedStatus => Enum.Parse<PaymentStatus>(storedStatus));
    }
}
