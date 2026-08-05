using QuickCheckr.Authoring.ThePress;
using QuickCheckr.Authoring.ThePress.Printing;
using QuickPulse.Explains;

namespace QuickPersistr.Tests.DuplicateGeneratedIdentity;

[DocFile]
public class DuplicateGeneratedIdentityTests : PersistrTest<DuplicateGeneratedIdentityTests>
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
            .Named("Duplicate generated identity")
            .Scope(() => new DuplicateIdentityScope())
            .Entities(new ThingamajigPersistence())
            .StoreCaseFiles(journalist)
            .Run(751926438);

    protected override void Verify(Article article)
    {
        Assert.Equal("Can Create Unique Thingamajig.Id", article.FailureDescription());
        Assert.Equal(
            "Expected: 2 distinct Thingamajig.Id values",
            article.FailingExpectationMessages()[0]);
        Assert.Equal(
            "Actual:   [ { Value: 42 }, { Value: 42 } ]",
            article.FailingExpectationMessages()[1]);
        Assert.Equal("", article.VerifyFailed());
        Assert.Equal(1, article.Total().Executions());
        Assert.Equal(1, article.Total().Actions());
        Assert.Equal(1, article.Total().Inputs());
        Assert.Equal(0, article.Total().PoolTraces());
        Assert.Equal(6, article.Total().PassedExpectations());
        Assert.Equal(5, article.Execution(1).Read().ExecutionId);
        Assert.Equal("Create Several Thingamajig", article.Execution(1).Action(1).Read().Label);
        Assert.Equal("Entities", article.Execution(1).Input(1).Read().Label);
        Assert.Equal("Can Create Thingamajig", article.PassedExpectation(1).Read().Label);
        Assert.Equal("Can Read Thingamajig.Id", article.PassedExpectation(2).Read().Label);
        Assert.Equal("Can Read Thingamajig.Description", article.PassedExpectation(3).Read().Label);
        Assert.Equal("Can Update Thingamajig.Description", article.PassedExpectation(4).Read().Label);
        Assert.Equal("Can Delete Thingamajig", article.PassedExpectation(5).Read().Label);
        Assert.Equal("Can Create Several Thingamajig", article.PassedExpectation(6).Read().Label);
        Assert.All(
            Enumerable.Range(1, 6),
            index => Assert.Equal(1, article.PassedExpectation(index).Read().TimesPassed));
    }
}

public readonly record struct ThingamajigId(int Value);

public class Thingamajig
{
    public ThingamajigId Id { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ThingamajigPersistence : Persistence<DuplicateIdentityStore, Thingamajig>
{
    public override IPersistenceSpecification<DuplicateIdentityStore> Define() =>
        Entity
            .PrimaryKey(thingamajig => thingamajig.Id)
            .Property(thingamajig => thingamajig.Description)
            .Persist();
}

public class DuplicateIdentityStore
{
    public List<Thingamajig> Entities { get; } = [];
}

public class DuplicateIdentityScope : IPersistenceScope<DuplicateIdentityStore>
{
    private static readonly ThingamajigId DuplicateId = new(42);
    private readonly DuplicateIdentityStore store = new();

    public IPersistenceReader<DuplicateIdentityStore> Reader =>
        new DuplicateIdentityReader(store);

    public TEntity GetById<TEntity>(object? id)
    where TEntity : class =>
        (store.Entities.FirstOrDefault(entity => Equals(entity.Id, id)) as TEntity)!;

    public TEntity Add<TEntity>(TEntity entity)
    {
        var thingamajig = (entity as Thingamajig)!;
        thingamajig.Id = DuplicateId;
        store.Entities.Add(thingamajig);
        return entity;
    }

    public void DeleteById<TEntity>(object? id)
    where TEntity : class =>
        store.Entities.RemoveAll(entity => Equals(entity.Id, id));

    public void Commit() { }

    public void StartNewSession() { }
}

public class DuplicateIdentityReader(DuplicateIdentityStore store)
    : IPersistenceReader<DuplicateIdentityStore>
{
    public TResult Query<TResult>(Func<DuplicateIdentityStore, TResult> query) =>
        query(store);
}
