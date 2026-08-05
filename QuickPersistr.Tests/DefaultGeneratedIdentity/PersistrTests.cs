using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.DefaultGeneratedIdentity;

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
            .Named("Default generated identity")
            .Scope(() => new DefaultIdentityScope())
            .Entities(new ThingamajigPersistence())
            .StoreCaseFiles(journalist)
            .Run(751926438);

    protected override void Verify(Article article)
    {
        Assert.Equal("Can Create Thingamajig", article.FailureDescription());
        Assert.Equal(
            "Expected: Non-default Thingamajig.Id",
            article.FailingExpectationMessages()[0]);
        Assert.Equal(
            "Actual:   { Value: 0 }",
            article.FailingExpectationMessages()[1]);
        Assert.Equal("", article.VerifyFailed());
        Assert.Equal(1, article.Total().Executions());
        Assert.Equal(1, article.Total().Actions());
        Assert.Equal(1, article.Total().Inputs());
        Assert.Equal(1, article.Total().PoolTraces());
        Assert.Equal(0, article.Total().PassedExpectations());
        Assert.Equal(1, article.Execution(1).Read().ExecutionId);
        Assert.Equal("Create Thingamajig", article.Execution(1).Action(1).Read().Label);
        Assert.Equal("Entity", article.Execution(1).Input(1).Read().Label);
        Assert.Equal("Entity", article.Execution(1).PoolTrace(1).Read().Label);
        Assert.Equal("Thingamajig-1", article.Execution(1).PoolTrace(1).Read().Value);
    }
}

public readonly record struct ThingamajigId(int Value);

public class Thingamajig
{
    public ThingamajigId Id { get; set; }
}

public class ThingamajigPersistence : Persistence<DefaultIdentityStore, Thingamajig>
{
    public override IPersistenceSpecification<DefaultIdentityStore> Define() =>
        Entity
            .PrimaryKey(thingamajig => thingamajig.Id)
            .Persist();
}

public class DefaultIdentityStore { }

public class DefaultIdentityScope : IPersistenceScope<DefaultIdentityStore>
{
    public IPersistenceReader<DefaultIdentityStore> Reader =>
        new DefaultIdentityReader();

    public TEntity GetById<TEntity>(object? id)
    where TEntity : class => null!;

    public TEntity Add<TEntity>(TEntity entity) => entity;

    public void DeleteById<TEntity>(object? id)
    where TEntity : class
    { }

    public void Commit() { }

    public void StartNewSession() { }
}

public class DefaultIdentityReader : IPersistenceReader<DefaultIdentityStore>
{
    public TResult Query<TResult>(Func<DefaultIdentityStore, TResult> query) =>
        query(new DefaultIdentityStore());
}
