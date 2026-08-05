using Microsoft.EntityFrameworkCore;
using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickPersistr.Tests.WithGenericIdentity;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.BrokenStrongIdentityConversion;

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
    protected override void GetPersistr(Journalist journalist) =>
        Persistr
            .Named("Broken strongly typed identity conversion")
            .Scope(() => new BrokenIdentityScope())
            .Entities(new ThingamajigPersistence())
            .StoreCaseFiles(journalist)
            .Run(1489207536);

    protected override void Verify(Article article)
    {
        Assert.Equal("Can Read Thingamajig.Id", article.FailureDescription());
        Assert.StartsWith(
            "Expected: { Value: ",
            article.FailingExpectationMessages()[0]);
        Assert.DoesNotContain(
            Guid.Empty.ToString(),
            article.FailingExpectationMessages()[0]);
        Assert.Equal(
            "Actual:   { Value: 00000000-0000-0000-0000-000000000000 }",
            article.FailingExpectationMessages()[1]);
        Assert.Equal("", article.VerifyFailed());
        Assert.Equal(2, article.Total().Executions());
        Assert.Equal(2, article.Total().Actions());
        Assert.Equal(1, article.Total().Inputs());
        Assert.Equal(2, article.Total().PoolTraces());
        Assert.Equal(2, article.Total().PassedExpectations());
        Assert.Equal(2, article.ShrinkCount);
        Assert.Equal(1, article.Execution(1).Read().ExecutionId);
        Assert.Equal("Create Thingamajig", article.Execution(1).Action(1).Read().Label);
        Assert.Equal("Entity", article.Execution(1).Input(1).Read().Label);
        Assert.Equal("Entity", article.Execution(1).PoolTrace(1).Read().Label);
        Assert.Equal("Thingamajig-1", article.Execution(1).PoolTrace(1).Read().Value);
        Assert.Equal(2, article.Execution(2).Read().ExecutionId);
        Assert.Equal("Read Thingamajig", article.Execution(2).Action(1).Read().Label);
        Assert.Equal("Entity", article.Execution(2).PoolTrace(1).Read().Label);
        Assert.Equal("Thingamajig-1", article.Execution(2).PoolTrace(1).Read().Value);
        Assert.Equal("Can Create Thingamajig", article.PassedExpectation(1).Read().Label);
        Assert.Equal(1, article.PassedExpectation(1).Read().TimesPassed);
        Assert.Equal("Can Read Thingamajig.Description", article.PassedExpectation(2).Read().Label);
        Assert.Equal(1, article.PassedExpectation(2).Read().TimesPassed);
    }
}

public class ThingamajigPersistence : Persistence<BrokenIdentityDbContext, Thingamajig>
{
    public override IPersistenceSpecification<BrokenIdentityDbContext> Define() =>
        Entity
            .PrimaryKey(thingamajig => thingamajig.Id)
            .Property(thingamajig => thingamajig.Description)
            .Persist();
}

public class BrokenIdentityScope()
    : EfPersistenceScope<BrokenIdentityDbContext>(options => new BrokenIdentityDbContext(options));

public class BrokenIdentityDbContext(DbContextOptions<BrokenIdentityDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Thingamajig>(builder =>
        {
            builder.HasKey(thingamajig => thingamajig.Id);
            builder.Property(thingamajig => thingamajig.Id)
                // Bug: every domain identity is collapsed to the same stored value.
                .HasConversion(
                    _ => Guid.Empty,
                    stored => new Id<Thingamajig>(stored));
        });
    }
}
