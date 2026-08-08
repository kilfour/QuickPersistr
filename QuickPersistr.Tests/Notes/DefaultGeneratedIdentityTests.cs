using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.Notes;

[DocFile]
public class DefaultGeneratedIdentityTests : PersistrTest<DefaultGeneratedIdentityTests>
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
            .Entities(new OnlyAModelPersistence())
            .StoreCaseFiles(journalist)
            .Run(751926438);

    protected override void Verify(Article article)
    {
        Assert.Equal("Can Create OnlyAModel", article.FailureDescription());
        Assert.Equal(
            "Expected: Non-default OnlyAModel.Id",
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
        Assert.Equal("Create OnlyAModel", article.Execution(1).Action(1).Read().Label);
        Assert.Equal("Entity", article.Execution(1).Input(1).Read().Label);
        Assert.Equal("Entity", article.Execution(1).PoolTrace(1).Read().Label);
        Assert.Equal("OnlyAModel-1", article.Execution(1).PoolTrace(1).Read().Value);
    }
}

public readonly record struct OnlyAModelId(int Value);

public class OnlyAModel
{
    public OnlyAModelId Id { get; set; }
}

public class OnlyAModelPersistence : Persistence<DefaultIdentityStore, OnlyAModel>
{
    public override IPersistenceSpecification<DefaultIdentityStore> Define() =>
        Entity
            .PrimaryKey(entity => entity.Id)
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
